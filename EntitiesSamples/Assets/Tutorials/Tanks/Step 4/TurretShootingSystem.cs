using Tutorials.Tanks.Execute;
using Tutorials.Tanks.Step2;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Tutorials.Tanks.Step4
{
    [UpdateAfter(typeof(TurretRotationSystem))]
    [BurstCompile]
    public partial struct TurretShootingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TurretShooting>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // To make structural changes, the job needs an EntityCommandBuffer.
            // An EntityCommandBuffer created from an EntityCommandBufferSystem will
            // be played back and disposed by that system.
            var ecbSystemSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSystemSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            // Instantiate the job.
            var turretShootJob = new TurretShoot
            {
                LocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                ECB = ecb
            };

            // Schedule the job to run as a single-threaded job.
            turretShootJob.Schedule();
        }
    }

    // IJobEntity relies on source generation to implicitly define a query from the signature of the Execute function.
    [WithAll(typeof(Shooting))] // Used in Step 8.
    [BurstCompile]
    public partial struct TurretShoot : IJobEntity
    {
        [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
        public EntityCommandBuffer ECB;

        // We need global transforms, so we access the LocalToWorld matrix of the turret and of the spawn position.

        // Note that the parameters use "in", which makes them readonly.
        // You should mark data in jobs as readonly wherever possible because the safety checks
        // allow jobs that access the same readonly data to run concurrently. If the parameters here
        // were "ref" instead, the safety checks would not allow this job to run concurrently
        // with any other jobs that also read the same component types.
        public void Execute(in TurretAspect turret, in LocalToWorld localToWorld)
        {
            var instance = ECB.Instantiate(turret.CannonBallPrefab);
            var spawnLocalToWorld = LocalToWorldLookup[turret.CannonBallSpawn];

            ECB.SetComponent(instance, new LocalTransform
            {
                Position = spawnLocalToWorld.Position,
                Rotation = quaternion.identity,
                Scale = LocalTransformLookup[turret.CannonBallPrefab].Scale
            });
            ECB.SetComponent(instance, new CannonBall
            {
                Velocity = localToWorld.Up * 20.0f
            });

            // The line below propagates the color from the turret to the cannon ball.
            ECB.SetComponent(instance, new URPMaterialPropertyBaseColor { Value = turret.Color });
        }
    }
}
