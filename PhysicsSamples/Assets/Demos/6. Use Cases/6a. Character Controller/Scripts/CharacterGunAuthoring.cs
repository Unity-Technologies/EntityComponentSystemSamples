using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;

public struct CharacterGun : IComponentData
{
    public Entity Bullet;
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

    class CharacterGunBaker : Baker<CharacterGunAuthoring>
    {
        public override void Bake(CharacterGunAuthoring authoring)
        {
            AddComponent(new CharacterGun()
            {
                Bullet = GetEntity(authoring.Bullet),
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
[BurstCompile]
[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(CharacterControllerSystem))]
public partial struct CharacterGunOneToManyInputSystem : ISystem
{
    [BurstCompile]
    private partial struct IJobEntity_CharacterGunOneToManyInputSystem : IJobEntity
    {
        public float DeltaTime;
        public CharacterGunInput Input;
        public EntityCommandBuffer.ParallelWriter CommandBufferParallel;

#if !ENABLE_TRANSFORM_V1
        private void Execute([ChunkIndexInQuery] int chunkIndexInQuery, Entity entity, ref LocalTransform gunLocalTransform, ref CharacterGun gun, in LocalToWorld gunTransform)
#else
        private void Execute([ChunkIndexInQuery] int chunkIndexInQuery, Entity entity, ref Rotation gunRotation, ref CharacterGun gun, in LocalToWorld gunTransform)
#endif
        {
            // Handle input
            {
                float a = -Input.Looking.y;
#if !ENABLE_TRANSFORM_V1
                gunLocalTransform.Rotation = math.mul(gunLocalTransform.Rotation, quaternion.Euler(math.radians(a), 0, 0));
#else
                gunRotation.Value = math.mul(gunRotation.Value, quaternion.Euler(math.radians(a), 0, 0));
#endif
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

#if !ENABLE_TRANSFORM_V1
                    LocalTransform localTransform = LocalTransform.FromPositionRotationScale(
                        gunTransform.Position + gunTransform.Forward,
                        gunLocalTransform.Rotation,
                        gunLocalTransform.Scale);
#else
                    Translation position = new Translation { Value = gunTransform.Position + gunTransform.Forward };
                    Rotation rotation = new Rotation { Value = gunRotation.Value };
#endif
                    PhysicsVelocity velocity = new PhysicsVelocity
                    {
                        Linear = gunTransform.Forward * gun.Strength,
                        Angular = float3.zero
                    };

#if !ENABLE_TRANSFORM_V1
                    CommandBufferParallel.SetComponent(chunkIndexInQuery, e, localTransform);
#else
                    CommandBufferParallel.SetComponent(chunkIndexInQuery, e, position);
                    CommandBufferParallel.SetComponent(chunkIndexInQuery, e, rotation);
#endif
                    CommandBufferParallel.SetComponent(chunkIndexInQuery, e, velocity);
                }
                gun.Duration = 0;
            }
            gun.WasFiring = 1;
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var commandBufferParallel = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
        var input = SystemAPI.GetSingleton<CharacterGunInput>();
        float dt = SystemAPI.Time.DeltaTime;

        state.Dependency = new IJobEntity_CharacterGunOneToManyInputSystem
        {
            DeltaTime = dt,
            Input = input,
            CommandBufferParallel = commandBufferParallel
        }.ScheduleParallel(state.Dependency);
    }
}
#endregion
