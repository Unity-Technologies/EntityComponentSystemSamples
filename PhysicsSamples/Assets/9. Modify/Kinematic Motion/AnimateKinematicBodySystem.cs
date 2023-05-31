using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Modify
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    partial struct AnimateKinematicBodySystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            float elapsedTime = (float)SystemAPI.Time.ElapsedTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // teleport all new bodies into place on the first frame
            // otherwise they may collide with things between their initial location and the first frame of animation
            foreach (var(transform, curve, entity) in
                     SystemAPI.Query<RefRW<LocalTransform>, AnimateKinematicBodyCurve>()
                         .WithEntityAccess()
                         .WithNone<Initialized>())
            {
                // sample curves and apply results directly to Translation and Rotation
                Sample(curve, elapsedTime, ref transform.ValueRW.Position, ref transform.ValueRW.Rotation);
                ecb.AddComponent<Initialized>(entity);
            }

            // moving kinematic bodies via their Translation and Rotation components will teleport them to the target location
            foreach (var(transform, mass, curve) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRO<PhysicsMass>, AnimateKinematicBodyCurve>()
                         .WithAll<TeleportKinematicBody, Initialized>())
            {
                // sample curves and apply results directly to Translation and Rotation
                Sample(curve, elapsedTime, ref transform.ValueRW.Position, ref transform.ValueRW.Rotation);
            }

            var tickSpeed = 1f / SystemAPI.Time.DeltaTime;

            // moving kinematic bodies via their PhysicsVelocity component will generate contact events with any bodies they pass through
            // use PhysicsVelocity.CalculateVelocityToTarget() to compute the velocity required to move to a desired target position
            // NOTE: if you want to avoid incorrect contact events, you can teleport kinematic bodies only on frames when there is discontinuity in their motion
            // if you do so, make sure you also set PhysicsGraphicalSmoothing.ApplySmoothing = 0 on that frame if using it, to prevent incorrect interpolation
            foreach (var(velocity, transform, mass, curve) in
                     SystemAPI.Query<RefRW<PhysicsVelocity>, RefRO<LocalTransform>, RefRO<PhysicsMass>,
                                     AnimateKinematicBodyCurve>()
                         .WithAll<Initialized>()
                         .WithNone<TeleportKinematicBody>())
            {
                // sample curves to determine target position and orientation
                var targetTransform = new RigidTransform(transform.ValueRO.Rotation, transform.ValueRO.Position);
                Sample(curve, elapsedTime, ref targetTransform.pos, ref targetTransform.rot);

                // modify PhysicsVelocity to move to the target location
                velocity.ValueRW = PhysicsVelocity.CalculateVelocityToTarget(mass.ValueRO, transform.ValueRO.Position,
                    transform.ValueRO.Rotation, targetTransform, tickSpeed);
            }

            ecb.Playback(state.EntityManager);
        }

        // cleanup component used to identify new animated bodies on their first frame
        struct Initialized : ICleanupComponentData
        {
        }

        // sample the animation curves to generate a new position and orientation
        // curves translate along the z-axis and set an orientation rotated about the y-axis
        static void Sample(in AnimateKinematicBodyCurve curve, in float t, ref float3 position,
            ref quaternion orientation)
        {
            position.z = curve.TranslationCurve.Evaluate(t);
            orientation = quaternion.AxisAngle(math.up(), math.radians(curve.OrientationCurve.Evaluate(t)));
        }
    }
}
