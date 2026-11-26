using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.Netcode.NetcodeTime;
using Unity.Netcode.Tracing;
using Unity.Transforms;
using UnityEngine;

namespace Samples.TracingSandbox
{
    public class CubeTracingAuthoring : MonoBehaviour
    {
        private class CubeTracingAuthoringBaker : Baker<CubeTracingAuthoring>
        {
            public override void Bake(CubeTracingAuthoring authoring)
            {
                var e = GetEntity(authoring, TransformUsageFlags.Dynamic);
                AddComponent<MyCubeTracing>(e);
            }
        }
    }

    public struct MyCubeTracing : IComponentData
    {
        private int value;
    }

    [BurstCompile]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation)]
    public partial struct MoveCubeSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        [WithAll(typeof(MyCubeTracing), typeof(Simulate))]
        [BurstCompile]
        public partial struct CubeTracingAuthoringJob : IJobEntity
        {
            public bool shouldMispredict;
            public float circleFactor;
            public NetworkTime netTime;
            [BurstCompile]
            public void Execute(ref LocalTransform transformRef)
            {
                transformRef.Position += new float3(circleFactor * math.sin(netTime.ServerTick.TickIndexForValidTick * 0.1f), circleFactor * math.cos(netTime.ServerTick.TickIndexForValidTick * 0.1f), 0f);
                if (shouldMispredict)
                {
                    transformRef.Position += new float3(0, 0, 5f);
                }
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var netTime = SystemAPI.GetSingleton<NetworkTime>();
            bool shouldMispredict = state.WorldUnmanaged.IsClient() && netTime.ServerTick.TickIndexForValidTick % 60 == 0;
            var circleFactor = 1f;
            var j = new CubeTracingAuthoringJob(){ shouldMispredict = shouldMispredict, circleFactor = circleFactor, netTime = netTime};
            state.Dependency = j.ScheduleParallel(state.Dependency);
        }
    }

    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class MissingSystem : SystemBase
    {
        protected override void OnCreate() { }

        protected override void OnUpdate()
        {

        }
    }

    [GhostComponentVariation(typeof(LocalTransform), "No Quantization Transform - 3D")]
    public struct NoQuantizationTransform : IComponentData
    {
        [GhostField(Quantization=0, Smoothing=SmoothingAction.InterpolateAndExtrapolate)]
        public float3 Position;

        [GhostField(Quantization=0, Smoothing=SmoothingAction.InterpolateAndExtrapolate)]
        public float Scale;

        [GhostField(Quantization=0, Smoothing=SmoothingAction.InterpolateAndExtrapolate)]
        public quaternion Rotation;
    }
}

