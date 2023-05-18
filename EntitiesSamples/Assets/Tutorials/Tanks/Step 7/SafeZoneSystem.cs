using Tutorials.Tanks.Execute;
using Tutorials.Tanks.Step2;
using Tutorials.Tanks.Step4;
using Tutorials.Tanks.Step6;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Tutorials.Tanks.Step7
{
    [UpdateBefore(typeof(TurretShootingSystem))]
    [BurstCompile]
    public partial struct SafeZoneSystem : ISystem
    {
        ComponentLookup<Shooting> m_TurretActiveFromEntity;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SafeZone>();
            state.RequireForUpdate<Config>();

            m_TurretActiveFromEntity = state.GetComponentLookup<Shooting>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float radius = SystemAPI.GetSingleton<Config>().SafeZoneRadius;
            const float debugRenderStepInDegrees = 20;

            // Debug rendering (the white circle).
            for (float angle = 0; angle < 360; angle += debugRenderStepInDegrees)
            {
                var a = float3.zero;
                var b = float3.zero;
                math.sincos(math.radians(angle), out a.x, out a.z);
                math.sincos(math.radians(angle + debugRenderStepInDegrees), out b.x, out b.z);
                Debug.DrawLine(a * radius, b * radius);
            }

            m_TurretActiveFromEntity.Update(ref state);
            var safeZoneJob = new SafeZoneJob
            {
                ShootingLookup = m_TurretActiveFromEntity,
                SquaredRadius = radius * radius
            };
            safeZoneJob.ScheduleParallel();
        }
    }

    [WithAll(typeof(Turret))]
    [BurstCompile]
    public partial struct SafeZoneJob : IJobEntity
    {
        // Without this attribute on the ShootingLookup, the safety system will complain about a
        // potential race condition if we try to schedule the job to run in parallel because it would
        // be unsafe for different threads to access the Shooting component of the same entity.
        // In this job, we know that different threads will not access any of the same entities,
        // so it is safe to disable the parallel safety check with the
        // [NativeDisableParallelForRestriction] attribute.
        [NativeDisableParallelForRestriction] public ComponentLookup<Shooting> ShootingLookup;

        public float SquaredRadius;

        // Because we want the global position of a child entity, we read LocalToWorld instead of LocalTransform.
        void Execute(Entity entity, in LocalToWorld transformMatrix)
        {
            var enable = math.lengthsq(transformMatrix.Position) > SquaredRadius;
            ShootingLookup.SetComponentEnabled(entity, enable);
        }
    }
}
