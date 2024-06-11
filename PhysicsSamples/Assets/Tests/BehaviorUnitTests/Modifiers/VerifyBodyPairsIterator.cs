using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Tests
{
    public struct VerifyBodyPairsIteratorData : IComponentData
    {
    }

    [Serializable]
    public class VerifyBodyPairsIterator : MonoBehaviour
    {
        class VerifyBodyPairsIteratorBaker : Baker<VerifyBodyPairsIterator>
        {
            public override void Bake(VerifyBodyPairsIterator authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<VerifyBodyPairsIteratorData>(entity);
            }
        }
    }
    [UpdateInGroup(typeof(PhysicsSimulationGroup))]
    [UpdateAfter(typeof(PhysicsCreateBodyPairsGroup))]
    [UpdateBefore(typeof(PhysicsCreateContactsGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial struct VerifyBodyPairsIteratorSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyBodyPairsIteratorData) }
            }));
        }

        struct VerifyBodyPairsIteratorJob : IBodyPairsJob
        {
            [ReadOnly]
            public NativeArray<RigidBody> Bodies;

            [ReadOnly]
            public ComponentLookup<VerifyBodyPairsIteratorData> VerificationData;

            public void Execute(ref ModifiableBodyPair pair)
            {
                Assert.AreNotEqual(pair.BodyIndexA, pair.BodyIndexB);
                Assert.AreEqual(pair.EntityA, Bodies[pair.BodyIndexA].Entity);
                Assert.AreEqual(pair.EntityB, Bodies[pair.BodyIndexB].Entity);
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            var worldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            state.Dependency = new VerifyBodyPairsIteratorJob
            {
                Bodies = worldSingleton.PhysicsWorld.Bodies,
                VerificationData = SystemAPI.GetComponentLookup<VerifyBodyPairsIteratorData>(true)
            }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), ref worldSingleton.PhysicsWorld, state.Dependency);
        }
    }
}
