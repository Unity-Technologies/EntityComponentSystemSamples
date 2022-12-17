using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class BulletTagComponentAuthoring : MonoBehaviour
{
    class BulletTagComponentBaker : Baker<BulletTagComponentAuthoring>
    {
        public override void Bake(BulletTagComponentAuthoring authoring)
        {
            BulletTagComponent component = default(BulletTagComponent);
            AddComponent(component);
        }
    }
}
