using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class LevelBorderAuthoring : MonoBehaviour
{
    [RegisterBinding(typeof(LevelBorder), "Side")]
    public int Side;

    class Baker : Baker<LevelBorderAuthoring>
    {
        public override void Bake(LevelBorderAuthoring authoring)
        {
            LevelBorder component = default(LevelBorder);
            component.Side = authoring.Side;
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}
