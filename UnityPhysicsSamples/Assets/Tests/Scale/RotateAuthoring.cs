using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

struct Rotate : IComponentData
{
    public float3 AngularVelocity;
}

class RotateAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    [SerializeField]
    float3 m_AngularVelocity = new float3(10f);

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) =>
        dstManager.AddComponentData(entity, new Rotate { AngularVelocity = math.radians(m_AngularVelocity) });
}

class RotateSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var deltaTime = Time.DeltaTime;
        return Entities.ForEach((ref Rotate rotate, ref Rotation rotation) =>
        {
            rotation.Value = math.mul(rotation.Value, quaternion.EulerZXY(rotate.AngularVelocity * deltaTime));
        }).Schedule(inputDeps);
    }
}
