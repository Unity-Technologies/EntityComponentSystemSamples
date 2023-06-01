using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace PlanetGravity
{
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial struct GravitySystem : ISystem
    {
        static readonly quaternion gravityOrientation = quaternion.RotateY(math.PI / 2f);

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new GravityJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
            }.Schedule();
        }

        [BurstCompile]
        public partial struct GravityJob : IJobEntity
        {
            public float DeltaTime;

            public void Execute(ref PhysicsMass bodyMass, ref PhysicsVelocity bodyVelocity,
                in LocalTransform localTransform, in Asteroid asteroid)
            {
                float mass = math.rcp(bodyMass.InverseMass);
                float3 dir = (asteroid.GravitationalCenter - localTransform.Position);
                float dist = math.length(dir);
                float invDist = 1.0f / dist;
                dir = math.normalize(dir);
                float3 addedGravity = (asteroid.GravitationalConstant * (asteroid.GravitationalMass * mass) * dir)
                    * invDist * invDist;
                bodyVelocity.Linear += addedGravity * DeltaTime;
                if (dist < asteroid.EventHorizonDistance)
                {
                    addedGravity = (asteroid.RotationMultiplier * asteroid.GravitationalConstant *
                        asteroid.GravitationalMass *
                        dir) * invDist;
                    bodyVelocity.Linear += math.rotate(gravityOrientation, addedGravity)
                        * asteroid.RotationMultiplier * DeltaTime;
                }
            }
        }
    }
}
