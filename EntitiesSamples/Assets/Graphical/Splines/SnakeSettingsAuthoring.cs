using Unity.Entities;
using UnityEngine;

namespace Graphical.Splines
{
    public class SnakeSettingsAuthoring : MonoBehaviour
    {
        public GameObject Prefab;
        public int Length;
        public int Count;
        public float Speed;
        public float Spacing;

        class Baker : Baker<SnakeSettingsAuthoring>
        {
            public override void Bake(SnakeSettingsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SnakeSettings
                {
                    Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
                    NumPartsPerSnake = authoring.Length,
                    NumSnakes = authoring.Count,
                    Speed = authoring.Speed,
                    Spacing = authoring.Spacing
                });
            }
        }
    }
}
