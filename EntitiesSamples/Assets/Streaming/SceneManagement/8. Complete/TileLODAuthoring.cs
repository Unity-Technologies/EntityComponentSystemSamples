using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Streaming.SceneManagement.CompleteSample
{
    // LOD ranges for each tile.
    public class TileLODAuthoring : MonoBehaviour
    {
        public List<float> LODRadius;

        class Baker : Baker<TileLODAuthoring>
        {
            public override void Bake(TileLODAuthoring authoring)
            {
                List<float> sorted = new List<float>(authoring.LODRadius);
                sorted.Sort();

                // index n corresponds to section n+1
                // (section 0 will be always loaded)
                for (int index = 0; index < sorted.Count; ++index)
                {
                    var entity = CreateAdditionalEntity(TransformUsageFlags.None, true);
                    AddComponent(entity, new TileLODBaking
                    {
                        LowerRadius = index > 0 ? sorted[index - 1] : 0f,
                        HigherRadius = sorted[index],
                        Section = index + 1
                    });
                }
            }
        }
    }

    // Used in only in baking.
    [BakingType]
    public struct TileLODBaking : IComponentData
    {
        public float LowerRadius; // Distance to load the section
        public float HigherRadius; // Distance to unload the section
        public int Section; // Section index that this component refers to
    }
}
