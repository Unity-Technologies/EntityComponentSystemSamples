using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class AsteroidTagComponentDataAuthoring : MonoBehaviour
{
    class Baker : Baker<AsteroidTagComponentDataAuthoring>
    {
        public override void Bake(AsteroidTagComponentDataAuthoring authoring)
        {
            AsteroidTagComponentData component = default(AsteroidTagComponentData);
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}
