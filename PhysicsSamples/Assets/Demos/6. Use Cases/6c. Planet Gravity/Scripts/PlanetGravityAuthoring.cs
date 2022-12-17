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
        AddComponent(component);
    }
}

[BurstCompile]
[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
partial struct InverseMassSystem : ISystem
{
    [BurstCompile]
    [WithAll(typeof(PlanetGravity))]
    public partial struct JobEntityInverseMass : IJobEntity
    {
        private void Execute(ref PhysicsMass mass)
        {
            var random = new Random();
            random.InitState(10);
            mass.InverseMass = random.NextFloat(mass.InverseMass, mass.InverseMass * 4f);
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
        state.Dependency = new JobEntityInverseMass().Schedule(state.Dependency);
    }
}

[RequireMatchingQueriesForUpdate]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
[BurstCompile]
public partial struct PlanetGravitySystem : ISystem
{
    static readonly quaternion k_GravityOrientation = quaternion.RotateY(math.PI / 2f);

    [BurstCompile]
    public partial struct ApplyGravityFromPlanet : IJobEntity
    {
        public float DeltaTime;

        [BurstCompile]
#if !ENABLE_TRANSFORM_V1
        public void Execute(ref PhysicsMass bodyMass, ref PhysicsVelocity bodyVelocity, in LocalTransform localTransform, in PlanetGravity gravity)
#else
        public void Execute(ref PhysicsMass bodyMass, ref PhysicsVelocity bodyVelocity, in Translation pos, in PlanetGravity gravity)
#endif
        {
            float mass = math.rcp(bodyMass.InverseMass);

#if !ENABLE_TRANSFORM_V1
            float3 dir = (gravity.GravitationalCenter - localTransform.Position);
#else
            float3 dir = (gravity.GravitationalCenter - pos.Value);
#endif
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
        // TODO(DOTS-6141): This expression can't currently be inlined into the IJobEntity initializer
        state.Dependency = new ApplyGravityFromPlanet
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
        }.Schedule(state.Dependency);
    }
}
