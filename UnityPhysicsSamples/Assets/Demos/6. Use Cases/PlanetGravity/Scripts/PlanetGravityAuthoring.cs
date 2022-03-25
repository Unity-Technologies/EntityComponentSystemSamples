using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public struct PlanetGravity : IComponentData
{
    public float3 GravitationalCenter;
    public float GravitationalMass;
    public float GravitationalConstant;
    public float EventHorizonDistance;
    public float RotationMultiplier;
}

public class PlanetGravityAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float GravitationalMass;
    public float GravitationalConstant;
    public float EventHorizonDistance;
    public float RotationMultiplier;

    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var component = new PlanetGravity
        {
            GravitationalCenter = transform.position,
            GravitationalMass = GravitationalMass,
            GravitationalConstant = GravitationalConstant,
            EventHorizonDistance = EventHorizonDistance,
            RotationMultiplier = RotationMultiplier
        };
        dstManager.AddComponentData(entity, component);

        if (dstManager.HasComponent<PhysicsMass>(entity))
        {
            var bodyMass = dstManager.GetComponentData<PhysicsMass>(entity);
            var random = new Random();
            random.InitState(10);
            bodyMass.InverseMass = random.NextFloat(bodyMass.InverseMass, bodyMass.InverseMass * 4f);

            dstManager.SetComponentData(entity, bodyMass);
        }
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(BuildPhysicsWorld))]
public partial class PlanetGravitySystem : SystemBase
{
    static readonly quaternion k_GravityOrientation = quaternion.RotateY(math.PI / 2f);

    protected override void OnUpdate()
    {
        var dt = Time.DeltaTime;

        Entities
            .WithName("ApplyGravityFromPlanet")
            .WithBurst()
            .ForEach((ref PhysicsMass bodyMass, ref PhysicsVelocity bodyVelocity, in Translation pos, in PlanetGravity gravity) =>
            {
                float mass = math.rcp(bodyMass.InverseMass);

                float3 dir = (gravity.GravitationalCenter - pos.Value);
                float dist = math.length(dir);
                float invDist = 1.0f / dist;
                dir = math.normalize(dir);
                float3 xtraGravity = (gravity.GravitationalConstant * (gravity.GravitationalMass * mass) * dir) * invDist * invDist;
                bodyVelocity.Linear += xtraGravity * dt;
                if (dist < gravity.EventHorizonDistance)
                {
                    xtraGravity = (gravity.RotationMultiplier * gravity.GravitationalConstant * gravity.GravitationalMass * dir) * invDist;
                    bodyVelocity.Linear += math.rotate(k_GravityOrientation, xtraGravity) * gravity.RotationMultiplier * dt;
                }
            }).Schedule();
    }
}
