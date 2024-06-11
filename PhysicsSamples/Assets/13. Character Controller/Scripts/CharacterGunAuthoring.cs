using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;

public struct CharacterGun : IComponentData
{
    public Entity Bullet;
    public float BulletScale;
    public float Strength;
    public float Rate;
    public float Duration;

    public int WasFiring;
    public int IsFiring;
}

public struct CharacterGunInput : IComponentData
{
    public float2 Looking;
    public float Firing;
}

public class CharacterGunAuthoring : MonoBehaviour
{
    public GameObject Bullet;

    public float Strength;
    public float Rate;
    public float Scale = 0.1f;

    class CharacterGunBaker : Baker<CharacterGunAuthoring>
    {
        public override void Bake(CharacterGunAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new CharacterGun()
            {
                Bullet = GetEntity(authoring.Bullet, TransformUsageFlags.Dynamic),
                BulletScale = authoring.Scale,
                Strength = authoring.Strength,
                Rate = authoring.Rate,
                WasFiring = 0,
                IsFiring = 0
            });
        }
    }
}

#region System

// Update before physics gets going so that we don't have hazard warnings.
// This assumes that all gun are being controlled from the same single input system
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(CharacterControllerSystem))]
public partial struct CharacterGunOneToManyInputSystem : ISystem
{
    [BurstCompile]
    private partial struct CharacterGunOneToManyInputSystemJob : IJobEntity
    {
        public float DeltaTime;
        public CharacterGunInput Input;
        public EntityCommandBuffer.ParallelWriter CommandBufferParallel;

        private void Execute([ChunkIndexInQuery] int chunkIndexInQuery, ref LocalTransform gunLocalTransform, ref CharacterGun gun, in LocalToWorld gunTransform)
        {
            // Handle input
            {
                float a = -Input.Looking.y;

                gunLocalTransform.Rotation = math.mul(gunLocalTransform.Rotation, quaternion.Euler(math.radians(a), 0, 0));

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
                    var e = CommandBufferParallel.Instantiate(chunkIndexInQuery, gun.Bullet);

                    LocalTransform localTransform = LocalTransform.FromPositionRotationScale(
                        gunTransform.Position + gunTransform.Forward,
                        gunLocalTransform.Rotation,
                        gunLocalTransform.Scale * gun.BulletScale);

                    PhysicsVelocity velocity = new PhysicsVelocity
                    {
                        Linear = gunTransform.Forward * gun.Strength,
                        Angular = float3.zero
                    };


                    CommandBufferParallel.SetComponent(chunkIndexInQuery, e, localTransform);

                    CommandBufferParallel.SetComponent(chunkIndexInQuery, e, velocity);
                }
                gun.Duration = 0;
            }
            gun.WasFiring = 1;
        }
    }

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CharacterGunInput>();
        state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new CharacterGunOneToManyInputSystemJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            Input = SystemAPI.GetSingleton<CharacterGunInput>(),
            CommandBufferParallel = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
        }.ScheduleParallel(state.Dependency);
    }
}
#endregion
