using Unity.Entities;
using UnityEngine;

namespace Miscellaneous.ProceduralMesh
{

    public class BakingProceduralAuthoring : MonoBehaviour
    {
        class Baker : Baker<BakingProceduralAuthoring>
        {
            public override void Bake(BakingProceduralAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<BakingProcedural>(entity);
            }
        }
    }

    [TemporaryBakingType]
    public struct BakingProcedural : IComponentData
    {

    }
}
