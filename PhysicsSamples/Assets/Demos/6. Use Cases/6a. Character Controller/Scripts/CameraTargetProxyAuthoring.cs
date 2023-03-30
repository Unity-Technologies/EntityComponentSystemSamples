using Unity.Entities;
using UnityEngine;

public struct CameraTargetProxy : IComponentData
{
    public Entity Target;
    public Entity LookTo;
    public Entity LookFrom;
}

public class CameraTargetProxyAuthoring : MonoBehaviour
{
    public GameObject Target;
    public GameObject LookTo;
    public GameObject LookFrom;

    class CameraTargetProxyAuthoringBaker : Baker<CameraTargetProxyAuthoring>
    {
        public override void Bake(CameraTargetProxyAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new CameraTargetProxy()
            {
                Target = GetEntity(authoring.Target, TransformUsageFlags.Dynamic),
                LookTo = GetEntity(authoring.LookTo, TransformUsageFlags.Dynamic),
                LookFrom = GetEntity(authoring.LookFrom, TransformUsageFlags.Dynamic)
            });
        }
    }
}
