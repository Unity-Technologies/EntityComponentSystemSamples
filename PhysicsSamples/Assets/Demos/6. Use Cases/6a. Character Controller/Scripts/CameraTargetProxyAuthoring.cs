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
            AddComponent(new CameraTargetProxy()
            {
                Target = GetEntity(authoring.Target),
                LookTo = GetEntity(authoring.LookTo),
                LookFrom = GetEntity(authoring.LookFrom)
            });
        }
    }
}
