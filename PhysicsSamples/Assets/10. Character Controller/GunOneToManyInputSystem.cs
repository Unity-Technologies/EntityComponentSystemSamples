using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace CharacterController
{
    // Update before physics gets going so that we don't have hazard warnings.
    // This assumes that all gun are being controlled from the same single input system
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(CharacterControllerSystem))]
    public partial struct GunOneToManyInputSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<CharacterGunInput>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            new GunInputJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Input = SystemAPI.GetSingleton<CharacterGunInput>(),
                ECB = ecb
            }.ScheduleParallel();
        }

        [BurstCompile]
        private partial struct GunInputJob : IJobEntity
        {
            public float DeltaTime;
            public CharacterGunInput Input;
            public EntityCommandBuffer.ParallelWriter ECB;

            private void Execute([ChunkIndexInQuery] int chunkIndexInQuery, ref LocalTransform gunLocalTransform,
                ref CharacterGun gun, in LocalToWorld gunTransform)
            {
                // Handle input
                {
                    float a = -Input.Looking.y;

                    gunLocalTransform.Rotation =
                        math.mul(gunLocalTransform.Rotation, quaternion.Euler(math.radians(a), 0, 0));

                    gun.IsFiring = Input.Firing > 0f ? 1 : 0;
                }

                if (gun.IsFiring == 0)
                {
                    gun.Duration = 0;
                    gun.WasFiring = 0;
                    return;
                }

                gun.Duration += DeltaTime;
                if ((gun.Duration > gun.Rate) || (gun.WasFiring == 0))
                {
                    if (gun.Bullet != null)
                    {
                        var e = ECB.Instantiate(chunkIndexInQuery, gun.Bullet);


                        LocalTransform localTransform = LocalTransform.FromPositionRotationScale(
                            gunTransform.Position + gunTransform.Forward,
                            gunLocalTransform.Rotation,
                            gunLocalTransform.Scale);

                        PhysicsVelocity velocity = new PhysicsVelocity
                        {
                            Linear = gunTransform.Forward * gun.Strength,
                            Angular = float3.zero
                        };


                        ECB.SetComponent(chunkIndexInQuery, e, localTransform);

                        ECB.SetComponent(chunkIndexInQuery, e, velocity);
                    }

                    gun.Duration = 0;
                }

                gun.WasFiring = 1;
            }
        }
    }
}
