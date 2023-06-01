using Unity.Entities;
using UnityEngine;

namespace Miscellaneous.ProceduralMesh
{
    public class RuntimeProceduralAuthoring : MonoBehaviour
    {
        class Baker : Baker<RuntimeProceduralAuthoring>
        {
            public override void Bake(RuntimeProceduralAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<RuntimeProcedural>(entity);
            }
        }
    }

    public struct RuntimeProcedural : IComponentData
    {
    }
}
