using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics
{
    public struct VerifyPhasedDispatchPairsData : IComponentData {}

    [Serializable]
    public class VerifyPhasedDispatchPairs : MonoBehaviour
    {
        class VerifyPhasedDispatchPairsBaker : Baker<VerifyPhasedDispatchPairs>
        {
            public override void Bake(VerifyPhasedDispatchPairs authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<VerifyPhasedDispatchPairsData>(entity);
            }
        }
    }

    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(PhysicsSimulationGroup))]
    [UpdateAfter(typeof(PhysicsCreateBodyPairsGroup))]
    [UpdateBefore(typeof(PhysicsCreateContactsGroup))]
    public partial struct VerifyPhasedDispatchPairsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[] { typeof(VerifyPhasedDispatchPairsData) }
            }));
        }

        struct VerifyPhasedDispatchPairsJob : IBodyPairsJob
        {
            [ReadOnly]
            public ComponentLookup<VerifyPhasedDispatchPairsData> VerificationData;

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

        public void OnUpdate(ref SystemState state)
        {
            var simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
            var worldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

            state.Dependency = new VerifyPhasedDispatchPairsJob
            {
                VerificationData = SystemAPI.GetComponentLookup<VerifyPhasedDispatchPairsData>(true),
                LastStaticPairPerDynamicBody = new NativeArray<int>(worldSingleton.PhysicsWorld.NumDynamicBodies, Allocator.TempJob),
                IsUnityPhysics = simulationSingleton.Type == SimulationType.UnityPhysics
            }.Schedule(simulationSingleton, ref worldSingleton.PhysicsWorld, state.Dependency);
        }
    }
}
