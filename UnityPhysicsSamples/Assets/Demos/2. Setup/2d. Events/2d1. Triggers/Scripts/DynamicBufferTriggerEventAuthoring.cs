using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;
using Unity.Collections;
using Unity.Burst;
using System;
using Unity.Assertions;
using Unity.Mathematics;

namespace Unity.Physics.Stateful
{
    // Describes the overlap state.
    // OverlapState in StatefulTriggerEvent is set to:
    //    1) EventOverlapState.Enter, when 2 bodies are overlapping in the current frame,
    //    but they did not overlap in the previous frame
    //    2) EventOverlapState.Stay, when 2 bodies are overlapping in the current frame,
    //    and they did overlap in the previous frame
    //    3) EventOverlapState.Exit, when 2 bodies are NOT overlapping in the current frame,
    //    but they did overlap in the previous frame
    public enum EventOverlapState : byte
    {
        Enter,
        Stay,
        Exit
    }

    // Trigger Event that is stored inside a DynamicBuffer
    public struct StatefulTriggerEvent : IBufferElementData, IComparable<StatefulTriggerEvent>
    {
        internal EntityPair Entities;
        internal BodyIndexPair BodyIndices;
        internal ColliderKeyPair ColliderKeys;

        public EventOverlapState State;
        public Entity EntityA => Entities.EntityA;
        public Entity EntityB => Entities.EntityB;
        public int BodyIndexA => BodyIndices.BodyIndexA;
        public int BodyIndexB => BodyIndices.BodyIndexB;
        public ColliderKey ColliderKeyA => ColliderKeys.ColliderKeyA;
        public ColliderKey ColliderKeyB => ColliderKeys.ColliderKeyB;

        public StatefulTriggerEvent(Entity entityA, Entity entityB, int bodyIndexA, int bodyIndexB,
            ColliderKey colliderKeyA, ColliderKey colliderKeyB)
        {
            Entities = new EntityPair
            {
                EntityA = entityA,
                EntityB = entityB
            };
            BodyIndices = new BodyIndexPair
            {
                BodyIndexA = bodyIndexA,
                BodyIndexB = bodyIndexB
            };
            ColliderKeys = new ColliderKeyPair
            {
                ColliderKeyA = colliderKeyA,
                ColliderKeyB = colliderKeyB
            };
            State = default;
        }

        // Returns other entity in EntityPair, if provided with one
        public Entity GetOtherEntity(Entity entity)
        {
            Assert.IsTrue((entity == EntityA) || (entity == EntityB));
            int2 indexAndVersion = math.select(new int2(EntityB.Index, EntityB.Version),
                new int2(EntityA.Index, EntityA.Version), entity == EntityB);
            return new Entity
            {
                Index = indexAndVersion[0],
                Version = indexAndVersion[1]
            };
        }

        public int CompareTo(StatefulTriggerEvent other)
        {
            var cmpResult = EntityA.CompareTo(other.EntityA);
            if (cmpResult != 0)
            {
                return cmpResult;
            }

            cmpResult = EntityB.CompareTo(other.EntityB);
            if (cmpResult != 0)
            {
                return cmpResult;
            }

            if (ColliderKeyA.Value != other.ColliderKeyA.Value)
            {
                return ColliderKeyA.Value < other.ColliderKeyA.Value ? -1 : 1;
            }

            if (ColliderKeyB.Value != other.ColliderKeyB.Value)
            {
                return ColliderKeyB.Value < other.ColliderKeyB.Value ? -1 : 1;
            }

            return 0;
        }
    }

    // If this component is added to an entity, trigger events won't be added to dynamic buffer
    // of that entity by TriggerEventConversionSystem. This component is by default added to
    // CharacterController entity, so that CharacterControllerSystem can add trigger events to
    // CharacterController on its own, without TriggerEventConversionSystem interference.
    public struct ExcludeFromTriggerEventConversion : IComponentData { }

    // This system converts stream of TriggerEvents to StatefulTriggerEvents that are stored in a Dynamic Buffer.
    // In order for TriggerEvents to be transformed to StatefulTriggerEvents and stored in a Dynamic Buffer, it is required to:
    //    1) Tick IsTrigger on PhysicsShapeAuthoring on the entity that should raise trigger events
    //    2) Add a DynamicBufferTriggerEventAuthoring component to that entity
    //    3) If this is desired on a Character Controller, tick RaiseTriggerEvents on CharacterControllerAuthoring (skip 1) and 2)),
    //    note that Character Controller will not become a trigger, it will raise events when overlapping with one
    [UpdateAfter(typeof(StepPhysicsWorld))]
    [UpdateBefore(typeof(EndFramePhysicsSystem))]
    public class TriggerEventConversionSystem : SystemBase
    {
        public JobHandle OutDependency => Dependency;

        private StepPhysicsWorld m_StepPhysicsWorld = default;
        private BuildPhysicsWorld m_BuildPhysicsWorld = default;
        private EndFramePhysicsSystem m_EndFramePhysicsSystem = default;
        private EntityQuery m_Query = default;

        private NativeList<StatefulTriggerEvent> m_PreviousFrameTriggerEvents;
        private NativeList<StatefulTriggerEvent> m_CurrentFrameTriggerEvents;

        protected override void OnCreate()
        {
            m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            m_EndFramePhysicsSystem = World.GetOrCreateSystem<EndFramePhysicsSystem>();
            m_Query = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(StatefulTriggerEvent)
                },
                None = new ComponentType[]
                {
                    typeof(ExcludeFromTriggerEventConversion)
                }
            });

            m_PreviousFrameTriggerEvents = new NativeList<StatefulTriggerEvent>(Allocator.Persistent);
            m_CurrentFrameTriggerEvents = new NativeList<StatefulTriggerEvent>(Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            m_PreviousFrameTriggerEvents.Dispose();
            m_CurrentFrameTriggerEvents.Dispose();
        }

        protected void SwapTriggerEventStates()
        {
            var tmp = m_PreviousFrameTriggerEvents;
            m_PreviousFrameTriggerEvents = m_CurrentFrameTriggerEvents;
            m_CurrentFrameTriggerEvents = tmp;
            m_CurrentFrameTriggerEvents.Clear();
        }

        protected static void AddTriggerEventsToDynamicBuffers(NativeList<StatefulTriggerEvent> triggerEventList,
            ref BufferFromEntity<StatefulTriggerEvent> bufferFromEntity, NativeHashMap<Entity, byte> entitiesWithTriggerBuffers)
        {
            for (int i = 0; i < triggerEventList.Length; i++)
            {
                var triggerEvent = triggerEventList[i];
                if (entitiesWithTriggerBuffers.ContainsKey(triggerEvent.EntityA))
                {
                    bufferFromEntity[triggerEvent.EntityA].Add(triggerEvent);
                }
                if (entitiesWithTriggerBuffers.ContainsKey(triggerEvent.EntityB))
                {
                    bufferFromEntity[triggerEvent.EntityB].Add(triggerEvent);
                }
            }
        }

        public static void UpdateTriggerEventState(NativeList<StatefulTriggerEvent> previousFrameTriggerEvents, NativeList<StatefulTriggerEvent> currentFrameTriggerEvents,
            NativeList<StatefulTriggerEvent> resultList)
        {
            int i = 0;
            int j = 0;

            while (i < currentFrameTriggerEvents.Length && j < previousFrameTriggerEvents.Length)
            {
                var currentFrameTriggerEvent = currentFrameTriggerEvents[i];
                var previousFrameTriggerEvent = previousFrameTriggerEvents[j];

                int cmpResult = currentFrameTriggerEvent.CompareTo(previousFrameTriggerEvent);

                // Appears in previous, and current frame, mark it as Stay
                if (cmpResult == 0)
                {
                    currentFrameTriggerEvent.State = EventOverlapState.Stay;
                    resultList.Add(currentFrameTriggerEvent);
                    i++;
                    j++;
                }
                else if (cmpResult < 0)
                {
                    // Appears in current, but not in previous, mark it as Enter
                    currentFrameTriggerEvent.State = EventOverlapState.Enter;
                    resultList.Add(currentFrameTriggerEvent);
                    i++;
                }
                else
                {
                    // Appears in previous, but not in current, mark it as Exit
                    previousFrameTriggerEvent.State = EventOverlapState.Exit;
                    resultList.Add(previousFrameTriggerEvent);
                    j++;
                }
            }

            if (i == currentFrameTriggerEvents.Length)
            {
                while (j < previousFrameTriggerEvents.Length)
                {
                    var triggerEvent = previousFrameTriggerEvents[j++];
                    triggerEvent.State = EventOverlapState.Exit;
                    resultList.Add(triggerEvent);
                }
            }
            else if (j == previousFrameTriggerEvents.Length)
            {
                while (i < currentFrameTriggerEvents.Length)
                {
                    var triggerEvent = currentFrameTriggerEvents[i++];
                    triggerEvent.State = EventOverlapState.Enter;
                    resultList.Add(triggerEvent);
                }
            }
        }

        protected override void OnUpdate()
        {
            if (m_Query.CalculateEntityCount() == 0)
            {
                return;
            }

            Dependency = JobHandle.CombineDependencies(m_StepPhysicsWorld.FinalSimulationJobHandle, Dependency);

            Entities
                .WithName("ClearTriggerEventDynamicBuffersJobParallel")
                .WithBurst()
                .WithNone<ExcludeFromTriggerEventConversion>()
                .ForEach((ref DynamicBuffer<StatefulTriggerEvent> buffer) =>
                {
                    buffer.Clear();
                }).ScheduleParallel();

            SwapTriggerEventStates();

            var currentFrameTriggerEvents = m_CurrentFrameTriggerEvents;
            var previousFrameTriggerEvents = m_PreviousFrameTriggerEvents;

            var triggerEventBufferFromEntity = GetBufferFromEntity<StatefulTriggerEvent>();
            var physicsWorld = m_BuildPhysicsWorld.PhysicsWorld;

            var collectTriggerEventsJob = new CollectTriggerEventsJob
            {
                TriggerEvents = currentFrameTriggerEvents
            };

            var collectJobHandle = collectTriggerEventsJob.Schedule(m_StepPhysicsWorld.Simulation, ref physicsWorld, Dependency);

            // Using HashMap since HashSet doesn't exist
            // Setting value type to byte to minimize memory waste
            NativeHashMap<Entity, byte> entitiesWithBuffersMap = new NativeHashMap<Entity, byte>(0, Allocator.TempJob);

            var collectTriggerBuffersHandle = Entities
                .WithName("CollectTriggerBufferJob")
                .WithBurst()
                .WithNone<ExcludeFromTriggerEventConversion>()
                .ForEach((Entity e, ref DynamicBuffer<StatefulTriggerEvent> buffer) =>
                {
                    entitiesWithBuffersMap.Add(e, 0);
                }).Schedule(Dependency);

            Dependency = JobHandle.CombineDependencies(collectJobHandle, collectTriggerBuffersHandle);

            Job
                .WithName("ConvertTriggerEventStreamToDynamicBufferJob")
                .WithBurst()
                .WithCode(() =>
                {
                    currentFrameTriggerEvents.Sort();

                    var triggerEventsWithStates = new NativeList<StatefulTriggerEvent>(currentFrameTriggerEvents.Length, Allocator.Temp);

                    UpdateTriggerEventState(previousFrameTriggerEvents, currentFrameTriggerEvents, triggerEventsWithStates);
                    AddTriggerEventsToDynamicBuffers(triggerEventsWithStates, ref triggerEventBufferFromEntity, entitiesWithBuffersMap);
                }).Schedule();

            m_EndFramePhysicsSystem.AddInputDependency(Dependency);
            entitiesWithBuffersMap.Dispose(Dependency);
        }

        [BurstCompile]
        public struct CollectTriggerEventsJob : ITriggerEventsJob
        {
            public NativeList<StatefulTriggerEvent> TriggerEvents;

            public void Execute(TriggerEvent triggerEvent)
            {
                TriggerEvents.Add(new StatefulTriggerEvent(
                    triggerEvent.EntityA, triggerEvent.EntityB, triggerEvent.BodyIndexA, triggerEvent.BodyIndexB,
                    triggerEvent.ColliderKeyA, triggerEvent.ColliderKeyB));
            }
        }
    }

    public class DynamicBufferTriggerEventAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddBuffer<StatefulTriggerEvent>(entity);
        }
    }
}
