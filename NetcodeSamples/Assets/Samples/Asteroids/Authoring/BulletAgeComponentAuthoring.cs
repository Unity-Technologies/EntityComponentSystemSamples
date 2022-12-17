#if UNITY_EDITOR
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[DisallowMultipleComponent]
public class BulletAgeComponentAuthoring : MonoBehaviour
{
    [RegisterBinding(typeof(BulletAgeComponent), "age")]
    public float age;
    [RegisterBinding(typeof(BulletAgeComponent), "maxAge")]
    public float maxAge;

    class BulletAgeComponentBaker : Baker<BulletAgeComponentAuthoring>
    {
        public override void Bake(BulletAgeComponentAuthoring authoring)
        {
            BulletAgeComponent component = default(BulletAgeComponent);
            component.age = authoring.age;
            component.maxAge = authoring.maxAge;
            AddComponent(component);
        }
    }
}
#endif
