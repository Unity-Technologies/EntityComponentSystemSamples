using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class BulletAgeComponentAuthoring : MonoBehaviour
{
    [RegisterBinding(typeof(BulletAgeComponent), "age")]
    public float age;
    [RegisterBinding(typeof(BulletAgeComponent), "maxAge")]
    public float maxAge;

    class Baker : Baker<BulletAgeComponentAuthoring>
    {
        public override void Bake(BulletAgeComponentAuthoring authoring)
        {
            BulletAgeComponent component = default(BulletAgeComponent);
            component.age = authoring.age;
            component.maxAge = authoring.maxAge;
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}
