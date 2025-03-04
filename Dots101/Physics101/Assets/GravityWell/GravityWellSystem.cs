using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace GravityWell
{
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    public partial struct GravityWellSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();
            var dt = SystemAPI.Time.DeltaTime;

            // move the gravity wells
            foreach (var (wellTransform, well) in
                     SystemAPI.Query<RefRW<LocalTransform>, RefRW<GravityWell>>())
            {
                // orbit the origin in a circle
                well.ValueRW.OrbitPos += config.WellOrbitSpeed * dt;
                math.sincos(well.ValueRW.OrbitPos, out var s, out var c);
                wellTransform.ValueRW.Position = new float3(c, 0, s) * config.WellOrbitRadius;
            }

            var wellQuery = SystemAPI.QueryBuilder().WithAll<GravityWell, LocalTransform>().Build();
            var wellTransforms = wellQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            foreach (var (velocity, collider,
                         mass, ballTransform) in
                     SystemAPI.Query<RefRW<PhysicsVelocity>, RefRO<PhysicsCollider>,
                         RefRO<PhysicsMass>, RefRO<LocalTransform>>())
            {
                for (int i = 0; i < wellTransforms.Length; i++)
                {
                    var wellTransform = wellTransforms[i];

                    velocity.ValueRW.ApplyExplosionForce(
                        mass.ValueRO,       
                        collider.ValueRO,   
                        ballTransform.ValueRO.Position,  // position of the body
                        ballTransform.ValueRO.Rotation,    // the rotation of the body
                        -config.WellStrength, // negative strength makes this an implosion 
                        wellTransform.Position,   // position of the explosion
                        // an explosion radius of 0 means the reach is infinite
                        // and strength does not diminish with distance
                        0, 
                        dt,
                        math.up());
                }
            }
        }
    }
}