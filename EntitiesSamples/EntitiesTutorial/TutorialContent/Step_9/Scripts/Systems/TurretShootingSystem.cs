using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;

[BurstCompile]
// This system reads WorldTransform, which is updated in the TransformSystemGroup. Without this UpdateAfterAttribute,
// the WorldTransform value read by this system would be the one computed in the prior frame instead of the current frame.
[UpdateAfter(typeof(TransformSystemGroup))]
partial struct TurretShootingSystem : ISystem
{
    // A ComponentLookup provides random access to a component (looking up an entity).
    // We'll use it to extract the world space position and orientation of the spawn point (cannon nozzle).
    ComponentLookup<WorldTransform> m_WorldTransformLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // ComponentLookup structures have to be initialized once.
        // The parameter specifies if the lookups will be read only or if they should allow writes.
        m_WorldTransformLookup = state.GetComponentLookup<WorldTransform>(true);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // ComponentLookup structures have to be updated every frame.
        m_WorldTransformLookup.Update(ref state);

        // Creating an EntityCommandBuffer to defer the structural changes required by instantiation.
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // Creating an instance of the job.
        // Passing it the ComponentLookup required to get the world transform of the spawn point.
        // And the entity command buffer the job can write to.
        var turretShootJob = new TurretShoot
        {
            WorldTransformLookup = m_WorldTransformLookup,
            ECB = ecb
        };

        // Schedule execution in a single thread, and do not block main thread.
        turretShootJob.Schedule();
    }
}

[BurstCompile]
// Requiring the Shooting tag component effectively prevents this job from running
// for the tanks which are in the safe zone.
[WithAll(typeof(Shooting))]
partial struct TurretShoot : IJobEntity
{
    [ReadOnly] public ComponentLookup<WorldTransform> WorldTransformLookup;
    public EntityCommandBuffer ECB;

    // Note that the TurretAspects parameter is "in", which declares it as read only.
    // Making it "ref" (read-write) would not make a difference in this case, but you
    // will encounter situations where potential race conditions trigger the safety system.
    // So in general, using "in" everywhere possible is a good principle.
    void Execute(in TurretAspect turret)
    {
        var instance = ECB.Instantiate(turret.CannonBallPrefab);
        var spawnLocalToWorld = WorldTransformLookup[turret.CannonBallSpawn];
        var cannonBallTransform = LocalTransform.FromPosition(spawnLocalToWorld.Position);

        // We are about to overwrite the transform of the new instance. If we didn't explicitly
        // copy the scale it would get reset to 1 and we'd have oversized cannon balls.
        cannonBallTransform.Scale = WorldTransformLookup[turret.CannonBallPrefab].Scale;
        ECB.SetComponent(instance, cannonBallTransform);
        ECB.SetComponent(instance, new CannonBall
        {
            Speed = spawnLocalToWorld.Forward() * 20.0f
        });

        // The line below propagates the color from the turret to the cannon ball.
        ECB.SetComponent(instance, new URPMaterialPropertyBaseColor { Value = turret.Color });
    }
}