using Unity.Entities;
using UnityEngine;

namespace Tutorials.Tanks.Step3
{
    public class TankAuthoring : MonoBehaviour
    {
        class Baker : Baker<TankAuthoring>
        {
            public override void Bake(TankAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<Tank>(entity);
            }
        }
    }

    // A tag component to identify the tank entities.
    public struct Tank : IComponentData
    {
    }
}
