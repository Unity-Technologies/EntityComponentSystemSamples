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

class RotateSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;
        Entities.ForEach((ref Rotation rotation, in Rotate rotate) =>
        {
            rotation.Value = math.mul(rotation.Value, quaternion.EulerZXY(rotate.AngularVelocity * deltaTime));
        }).Schedule();
    }
}
