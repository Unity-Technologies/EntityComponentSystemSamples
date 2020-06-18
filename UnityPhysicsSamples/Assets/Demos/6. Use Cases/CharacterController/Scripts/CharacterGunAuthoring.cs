using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;

public struct CharacterGun: IComponentData
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

public class CharacterGunAuthoring : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject Bullet;

    public float Strength;
    public float Rate;

    // Referenced prefabs have to be declared so that the conversion system knows about them ahead of time
    public void DeclareReferencedPrefabs(List<GameObject> gameObjects)
    {
        gameObjects.Add(Bullet);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(
            entity,
            new CharacterGun
            {
                Bullet = conversionSystem.GetPrimaryEntity(Bullet),
                Strength = Strength,
                Rate = Rate,
                WasFiring = 0,
                IsFiring = 0
            });
    }
}

#region System
// Update before physics gets going so that we don't have hazard warnings.
// This assumes that all gun are being controlled from the same single input system
[UpdateAfter(typeof(CharacterControllerSystem))]
public class CharacterGunOneToManyInputSystem : SystemBase
{
    EntityCommandBufferSystem m_EntityCommandBufferSystem;

    protected override void OnCreate()
    {
        m_EntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var commandBuffer = m_EntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        var input = GetSingleton<CharacterGunInput>();
        float dt = UnityEngine.Time.fixedDeltaTime;

        Entities
            .WithName("CharacterControllerGunToManyInputJob")
            .WithBurst()
            .ForEach((Entity entity, int entityInQueryIndex, ref Rotation gunRotation, ref CharacterGun gun, in LocalToWorld gunTransform) =>
            {
                // Handle input
                {
                    float a = -input.Looking.y;
                    gunRotation.Value = math.mul(gunRotation.Value, quaternion.Euler(math.radians(a), 0, 0));
                    gun.IsFiring = input.Firing > 0f ? 1 : 0;
                }

                if (gun.IsFiring == 0)
                {
                    gun.Duration = 0;
                    gun.WasFiring = 0;
                    return;
                }

                gun.Duration += dt;
                if ((gun.Duration > gun.Rate) || (gun.WasFiring == 0))
                {
                    if (gun.Bullet != null)
                    {
                        var e = commandBuffer.Instantiate(entityInQueryIndex, gun.Bullet);

                        Translation position = new Translation { Value = gunTransform.Position + gunTransform.Forward };
                        Rotation rotation = new Rotation { Value = gunRotation.Value };
                        PhysicsVelocity velocity = new PhysicsVelocity
                        {
                            Linear = gunTransform.Forward * gun.Strength,
                            Angular = float3.zero
                        };

                        commandBuffer.SetComponent(entityInQueryIndex, e, position);
                        commandBuffer.SetComponent(entityInQueryIndex, e, rotation);
                        commandBuffer.SetComponent(entityInQueryIndex, e, velocity);
                    }
                    gun.Duration = 0;
                }
                gun.WasFiring = 1;
            }).ScheduleParallel();
        m_EntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}
#endregion
