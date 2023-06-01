using Unity.Entities;

public struct CubeTagComponent : IComponentData { }

[UnityEngine.DisallowMultipleComponent]
public class CubeTagComponentAuthoring : UnityEngine.MonoBehaviour
{
    class CubeTagComponentBaker : Baker<CubeTagComponentAuthoring>
    {
        public override void Bake(CubeTagComponentAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<CubeTagComponent>(entity);
        }
    }
}
