using Unity.Entities;
using UnityEngine;

public class AnimatorAuthoring : MonoBehaviour
{
    public GameObject AnimatorPrefab;

    public class AnimatorBaker : Baker<AnimatorAuthoring>
    {
        public override void Bake(AnimatorAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Renderable);
            AddComponent(e, new AnimatorRefComponent { AnimatorAsGO = authoring.AnimatorPrefab });
        }
    }
}

public struct AnimatorRefComponent : IComponentData
{
    public UnityObjectRef<GameObject> AnimatorAsGO;
}
