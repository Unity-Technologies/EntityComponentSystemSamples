using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics
{
    public struct VerifyPhasedDispatchPairsData : IComponentData {}

    [Serializable]
    public class VerifyPhasedDispatchPairs : MonoBehaviour, IConvertGameObjectToEntity
    {
        void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new VerifyPhasedDispatchPairsData());
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(StepPhysicsWorld))]
    public class VerifyPhasedDispatchPairsSystem : SystemBase
    {
        EntityQuery m_VerificationGroup;
        StepPhysicsWorld m_StepPhysicsWorld;

        protected override void OnCreate()
        {
            m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            m_VerificationGroup = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyPhasedDispatchPairsData) }
            });
        }

        struct VerifyPhasedDispatchPairsJob : IBodyPairsJob
        {
            [ReadOnly]
            public ComponentDataFromEntity<VerifyPhasedDispatchPairsData> VerificationData;

            [DeallocateOnJobCompletion]
            public NativeArray<int> LastStaticPairPerDynamicBody;

            public bool IsUnityPhysics;

            public void Execute(ref ModifiableBodyPair pair)
            {
                if (IsUnityPhysics)
                {
                    int bodyIndexA = pair.BodyIndexA;
                    int bodyIndexB = pair.BodyIndexB;

                    bool bodyBIsDynamic = bodyIndexB < LastStaticPairPerDynamicBody.Length;
                    if (bodyBIsDynamic)
                    {
                        Assert.IsTrue(LastStaticPairPerDynamicBody[bodyIndexA] <= bodyIndexB);
                    }
                    else
                    {
                        LastStaticPairPerDynamicBody[bodyIndexA] = bodyIndexB;
                    }
                }
            }
        }

        protected override void OnUpdate()
        {
            SimulationCallbacks.Callback verifyPhasedDispatchPairsJobCallback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
            {
                return new VerifyPhasedDispatchPairsJob
                {
                    VerificationData = GetComponentDataFromEntity<VerifyPhasedDispatchPairsData>(true),
                    LastStaticPairPerDynamicBody = new NativeArray<int>(world.NumDynamicBodies, Allocator.TempJob),
                    IsUnityPhysics = simulation.Type == SimulationType.UnityPhysics
                }.Schedule(simulation, ref world, inDeps);
            };

            m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateDispatchPairs, verifyPhasedDispatchPairsJobCallback, Dependency);
        }
    }
}
