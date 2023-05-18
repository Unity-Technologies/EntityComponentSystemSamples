using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

public struct MovableCubeComponent : IComponentData
{
}

[DisallowMultipleComponent]
public class MovableCubeComponentAuthoring : MonoBehaviour
{
    class MovableCubeComponentBaker : Baker<MovableCubeComponentAuthoring>
    {
        public override void Bake(MovableCubeComponentAuthoring authoring)
        {
            MovableCubeComponent component = default(MovableCubeComponent);
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}
