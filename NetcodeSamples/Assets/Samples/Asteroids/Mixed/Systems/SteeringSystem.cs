using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Collections;
using Unity.Burst;

namespace Asteroids.Mixed
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [BurstCompile]
    public partial struct SteeringSystem : ISystem
    {
        private Entity m_BulletPrefab;
        private BufferLookup<ShipCommandData> m_ShipCommandDataFromEntity;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ShipCommandDataFromEntity = state.GetBufferLookup<ShipCommandData>(true);
            state.RequireForUpdate<LevelComponent>();
            state.RequireForUpdate<AsteroidsSpawner>();
        }
        [WithAll(typeof(ShipTagComponentData), typeof(ShipCommandData), typeof(Simulate))]
        [BurstCompile]
        partial struct SteeringJob : IJobEntity
        {
            public LevelComponent level;
            public EntityCommandBuffer.ParallelWriter commandBuffer;
            public Entity bulletPrefab;
            public float deltaTime;
            public NetworkTick currentTick;
            public byte isFirstFullTick;
            [ReadOnly] public BufferLookup<ShipCommandData> inputFromEntity;
            public void Execute(Entity entity, [EntityIndexInQuery] int entityIndexInQuery,

                ref LocalTransform transform, ref Velocity velocity,

                ref ShipStateComponentData state, in GhostOwner ghostOwner)
            {
                var input = inputFromEntity[entity];
                ShipCommandData inputData;
                if (!input.GetDataAtTick(currentTick, out inputData))
                    inputData.shoot = 0;

                state.State = inputData.thrust;

                if (inputData.left == 1)
                {

                    transform.Rotation = math.mul(transform.Rotation,

                        quaternion.RotateZ(math.radians(level.shipRotationRate * deltaTime)));
                }

                if (inputData.right == 1)
                {

                    transform.Rotation = math.mul(transform.Rotation,

                        quaternion.RotateZ(math.radians(-level.shipRotationRate * deltaTime)));
                }

                if (inputData.thrust == 1)
                {
                    float3 fwd = new float3(0, level.shipForwardForce * deltaTime, 0);

                    velocity.Value += math.mul(transform.Rotation, fwd).xy;

                }


                transform.Position.xy += velocity.Value * deltaTime;


                var canShoot = !state.WeaponCooldown.IsValid || currentTick.IsNewerThan(state.WeaponCooldown);
                if (inputData.shoot != 0 && canShoot)
                {
                    if (bulletPrefab != Entity.Null && isFirstFullTick == 1)
                    {
                        var e = commandBuffer.Instantiate(entityIndexInQuery, bulletPrefab);


                        var bulletTx = transform;
                        bulletTx.Scale = 10; //TODO: this should come from the bullet prefab
                        commandBuffer.SetComponent(entityIndexInQuery, e, bulletTx);

                        var vel = new Velocity
                            {Value = math.mul(transform.Rotation, new float3(0, level.bulletVelocity, 0)).xy};


                        commandBuffer.SetComponent(entityIndexInQuery, e,
                            new GhostOwner {NetworkId = ghostOwner.NetworkId});
                        commandBuffer.SetComponent(entityIndexInQuery, e, vel);
                    }

                    state.WeaponCooldown = currentTick;
                    state.WeaponCooldown.Add(level.bulletRofCooldownTicks);
                }
                else if (canShoot)
                {
                    state.WeaponCooldown = NetworkTick.Invalid;
                }
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            m_ShipCommandDataFromEntity.Update(ref state);
            var steeringJob = new SteeringJob
            {
                level = SystemAPI.GetSingleton<LevelComponent>(),
                commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                bulletPrefab = SystemAPI.GetSingleton<AsteroidsSpawner>().Bullet,
                deltaTime = SystemAPI.Time.DeltaTime,
                currentTick = networkTime.ServerTick,
                isFirstFullTick = (byte) (networkTime.IsFirstTimeFullyPredictingTick ? 1 : 0),
                inputFromEntity = m_ShipCommandDataFromEntity
            };
            state.Dependency = steeringJob.ScheduleParallel(state.Dependency);
        }
    }
}

