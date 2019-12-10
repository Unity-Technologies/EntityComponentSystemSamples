using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
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

public class PlanetGravityBehaviour : MonoBehaviour, IConvertGameObjectToEntity
{
    public float GravitationalMass;
    public float GravitationalConstant;
    public float EventHorizonDistance;
    public float RotationMultiplier;

    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var component = new PlanetGravity()
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
            Random random = new Random();
            random.InitState(10);
            bodyMass.InverseMass = random.NextFloat(bodyMass.InverseMass, bodyMass.InverseMass * 4f);

            dstManager.SetComponentData(entity, bodyMass);
        }
    }
}

#region System
[UpdateBefore(typeof(BuildPhysicsWorld))]
public class PlanetGravitySystem : JobComponentSystem
{
    [BurstCompile]
    struct PlanetGravityJob : IJobForEach<Translation, PhysicsMass, PhysicsVelocity, PlanetGravity>
    {
        public float dt;

        public void Execute([ReadOnly]ref Translation pos,
                            ref PhysicsMass bodyMass,
                            ref PhysicsVelocity bodyVelocity,
                            [ReadOnly]ref PlanetGravity gravity)
        {
            float mass = math.rcp(bodyMass.InverseMass);
            //motion.LinearVelocity *= 0.99f;

            float3 dir = (gravity.GravitationalCenter - pos.Value);
            float dist = math.length(dir);
            float invDist = 1.0f / dist;
            dir = math.normalize(dir);
            float3 xtraGravity = (gravity.GravitationalConstant * (gravity.GravitationalMass * mass) * dir) * invDist * invDist;
            bodyVelocity.Linear += xtraGravity * dt;
            if (dist < gravity.EventHorizonDistance)
            {
                xtraGravity = (gravity.RotationMultiplier * gravity.GravitationalConstant * gravity.GravitationalMass * dir) * invDist;
                quaternion quat = quaternion.RotateY(90.0f);
                bodyVelocity.Linear += math.rotate(quat, xtraGravity) * gravity.RotationMultiplier * dt;
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var planetGravityAccelerationJob = new PlanetGravityJob { dt = UnityEngine.Time.fixedDeltaTime };
        return planetGravityAccelerationJob.Schedule(this, inputDeps);
    }
}
#endregion
