using Unity.Burst;
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

public class PlanetGravityAuthoring : MonoBehaviour
{
    public float GravitationalMass;
    public float GravitationalConstant;
    public float EventHorizonDistance;
    public float RotationMultiplier;
}

class PlanetGravityAuthoringBaker : Baker<PlanetGravityAuthoring>
{
    public override void Bake(PlanetGravityAuthoring authoring)
    {
        var transform = GetComponent<Transform>();
        var component = new PlanetGravity
        {
            GravitationalCenter = transform.position,
            GravitationalMass = authoring.GravitationalMass,
            GravitationalConstant = authoring.GravitationalConstant,
            EventHorizonDistance = authoring.EventHorizonDistance,
            RotationMultiplier = authoring.RotationMultiplier
        };
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, component);
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
partial struct InverseMassSystem : ISystem
{
    [BurstCompile]
    [WithAll(typeof(PlanetGravity))]
    public partial struct EntityInverseMassJob : IJobEntity
    {
        private void Execute(ref PhysicsMass mass)
        {
            var random = new Random();
            random.InitState(10);
            mass.InverseMass = random.NextFloat(mass.InverseMass, mass.InverseMass * 4f);
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new EntityInverseMassJob().Schedule(state.Dependency);
    }
}

[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial struct PlanetGravitySystem : ISystem
{
    static readonly quaternion k_GravityOrientation = quaternion.RotateY(math.PI / 2f);

    [BurstCompile]
    public partial struct ApplyGravityFromPlanetJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref PhysicsMass bodyMass, ref PhysicsVelocity bodyVelocity, in LocalTransform localTransform, in PlanetGravity gravity)
        {
            float mass = math.rcp(bodyMass.InverseMass);


            float3 dir = (gravity.GravitationalCenter - localTransform.Position);

            float dist = math.length(dir);
            float invDist = 1.0f / dist;
            dir = math.normalize(dir);
            float3 xtraGravity = (gravity.GravitationalConstant * (gravity.GravitationalMass * mass) * dir) * invDist * invDist;
            bodyVelocity.Linear += xtraGravity * DeltaTime;
            if (dist < gravity.EventHorizonDistance)
            {
                xtraGravity = (gravity.RotationMultiplier * gravity.GravitationalConstant * gravity.GravitationalMass * dir) * invDist;
                bodyVelocity.Linear += math.rotate(k_GravityOrientation, xtraGravity) * gravity.RotationMultiplier * DeltaTime;
            }
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        state.Dependency = new ApplyGravityFromPlanetJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
        }.Schedule(state.Dependency);
    }
}
