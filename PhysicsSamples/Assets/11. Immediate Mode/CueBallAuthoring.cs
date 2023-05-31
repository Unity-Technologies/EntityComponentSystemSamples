using Unity.Entities;
using UnityEngine;

namespace ImmediateMode
{
    public class CueBallAuthoring : MonoBehaviour
    {
        class Baker : Baker<CueBallAuthoring>
        {
            public override void Bake(CueBallAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<CueBall>(entity);
            }
        }
    }

    public struct CueBall : IComponentData
    {
    }
}
