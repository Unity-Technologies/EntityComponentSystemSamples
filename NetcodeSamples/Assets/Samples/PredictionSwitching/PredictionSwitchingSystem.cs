using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Collections;
using Unity.Rendering;

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct PredictionSwitchingSystem : ISystem
{
    ComponentLookup<GhostOwner> m_GhostOwnerFromEntity;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PredictionSwitchingSettings>();
        state.RequireForUpdate<CommandTarget>();
        state.RequireForUpdate<GhostPredictionSwitchingQueues>();
        m_GhostOwnerFromEntity = state.GetComponentLookup<GhostOwner>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var playerEnt = SystemAPI.GetSingleton<CommandTarget>().targetEntity;
        if (playerEnt == Entity.Null)
            return;

        var playerPos = state.EntityManager.GetComponentData<LocalTransform>(playerEnt).Position;

        var ghostPredictionSwitchingQueues = SystemAPI.GetSingletonRW<GhostPredictionSwitchingQueues>().ValueRW;
        var parallelEcb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        m_GhostOwnerFromEntity.Update(ref state);
        var ghostOwnerFromEntity = m_GhostOwnerFromEntity;

        var predictionSwitchingSettings = SystemAPI.GetSingleton<PredictionSwitchingSettings>();

        new SwitchToPredictedGhostViaRange
        {
            playerPos = playerPos,
            parallelEcb = parallelEcb,
            predictedQueue = ghostPredictionSwitchingQueues.ConvertToPredictedQueue,
            enterRadiusSq = predictionSwitchingSettings.PredictionSwitchingRadius * predictionSwitchingSettings.PredictionSwitchingRadius,
            ghostOwnerFromEntity = ghostOwnerFromEntity,
            transitionDurationSeconds = predictionSwitchingSettings.TransitionDurationSeconds,
            ballColorChangingEnabled = predictionSwitchingSettings.BallColorChangingEnabled,
        }.ScheduleParallel();

        var radiusPlusMargin = (predictionSwitchingSettings.PredictionSwitchingRadius + predictionSwitchingSettings.PredictionSwitchingMargin);
        new SwitchToInterpolatedGhostViaRange
        {
            playerPos = playerPos,
            parallelEcb = parallelEcb,
            interpolatedQueue = ghostPredictionSwitchingQueues.ConvertToInterpolatedQueue,
            exitRadiusSq = radiusPlusMargin * radiusPlusMargin,
            ghostOwnerFromEntity = ghostOwnerFromEntity,
            transitionDurationSeconds = predictionSwitchingSettings.TransitionDurationSeconds,
        }.ScheduleParallel();
    }

    [BurstCompile]
    [WithNone(typeof(PredictedGhost), typeof(SwitchPredictionSmoothing))]
    partial struct SwitchToPredictedGhostViaRange : IJobEntity
    {
        public float3 playerPos;
        public float enterRadiusSq;

        public NativeQueue<ConvertPredictionEntry>.ParallelWriter predictedQueue;
        public EntityCommandBuffer.ParallelWriter parallelEcb;

        [ReadOnly]
        public ComponentLookup<GhostOwner> ghostOwnerFromEntity;

        public float transitionDurationSeconds;
        public byte ballColorChangingEnabled;

        void Execute(Entity ent, [EntityIndexInQuery] int entityIndexInQuery, in LocalTransform transform, in GhostInstance ghostInstance)
        {
            if (ghostInstance.ghostType < 0) return;

            if (math.distancesq(playerPos, transform.Position) < enterRadiusSq)
            {
                predictedQueue.Enqueue(new ConvertPredictionEntry
                {
                    TargetEntity = ent,
                    TransitionDurationSeconds = transitionDurationSeconds,
                });

                if (ballColorChangingEnabled == 1 && !ghostOwnerFromEntity.HasComponent(ent))
                    parallelEcb.AddComponent(entityIndexInQuery, ent, new URPMaterialPropertyBaseColor {Value = new float4(0, 1, 0, 1)});
            }
        }
    }

    [BurstCompile]
    [WithNone(typeof(SwitchPredictionSmoothing))]
    [WithAll(typeof(PredictedGhost))]
    partial struct SwitchToInterpolatedGhostViaRange : IJobEntity
    {
        public float3 playerPos;
        public float exitRadiusSq;

        public NativeQueue<ConvertPredictionEntry>.ParallelWriter interpolatedQueue;
        public EntityCommandBuffer.ParallelWriter parallelEcb;

        [ReadOnly]
        public ComponentLookup<GhostOwner> ghostOwnerFromEntity;
        public float transitionDurationSeconds;

        void Execute(Entity ent, [EntityIndexInQuery] int entityIndexInQuery, in LocalTransform transform, in GhostInstance ghostInstance)
        {
            if (ghostInstance.ghostType < 0) return;

            if (math.distancesq(playerPos, transform.Position) > exitRadiusSq)
            {
                interpolatedQueue.Enqueue(new ConvertPredictionEntry
                {
                    TargetEntity = ent,
                    TransitionDurationSeconds = transitionDurationSeconds,
                });
                if (!ghostOwnerFromEntity.HasComponent(ent))
                    parallelEcb.RemoveComponent<URPMaterialPropertyBaseColor>(entityIndexInQuery, ent);
            }
        }
    }
}
