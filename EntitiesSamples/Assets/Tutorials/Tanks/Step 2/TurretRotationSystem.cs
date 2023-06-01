using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Tutorials.Tanks.Step2
{
    // Unmanaged systems based on ISystem can be Burst compiled, but this is not yet the default,
    // so we have to explicitly opt into Burst compilation with the [BurstCompile] attribute.
    // It has to be added on BOTH the struct AND the OnCreate/OnDestroy/OnUpdate functions to be
    // effective.
    public partial struct TurretRotationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Unless we disable "automatic bootstrapping", all systems are automatically instantiated and added to the default world.
            // This RequireForUpdate call makes this system update only if at least one entity has the Step2.TurretRotation component.
            // So when we play a scene, this system will only update if this component is added to an entity in a sub scene.
            // This pattern effectively allows each scene to choose whether a system should update.
            // (In this tutorial, we only want the systems from the current step and earlier to update for a given scene,
            // e.g. when running the Step 5 scene, we only want systems from steps 1-5 to update.)
            state.RequireForUpdate<Execute.TurretRotation>();
        }

        // This system doesn't need an OnDestroy method, so it uses the default empty one defined in ISystem.

        // See note above regarding the [BurstCompile] attribute.
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Rotate 180 degrees around Y every second.
            var spin =  quaternion.RotateY(SystemAPI.Time.DeltaTime * math.PI);

            // The SystemAPI.Query method can be called only as the 'in' clause of a foreach loop.
            // For these loops, source generation creates a query, and the loop iterates over each entity of the query.
            // In the example below, the query matches all entities with LocalTransform and Turret components.

            // We want to modify the LocalTransform component values, so we put RefRW<LocalTransform> in the Query call.
            // Because we want to include Turret in the query but don't need to read or write
            // the Turret component values, we call WithAll<Turret>().
            // Without this WithAll() call, the query would match *all* entities having transform components,
            // and this foreach would rotate more than just the turrets.
            foreach (var transform in
                     SystemAPI.Query<RefRW<LocalTransform>>()
                         .WithAll<Turret>())
            {
                // ValueRW returns a ref to the actual component value.
                // Add a rotation around the parent's Y axis.
                transform.ValueRW.Rotation = math.mul(spin, transform.ValueRO.Rotation);
            }
        }
    }
}
