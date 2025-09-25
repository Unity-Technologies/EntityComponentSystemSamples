using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct EnableImportance : IComponentData
    {
        public bool Enabled;
        public bool UseBatchedImportanceFunction;
        public GhostDistanceData TilingConfiguration;
    }

    public class EnableImportanceAuthoring : MonoBehaviour
    {
        public bool Enabled = true;
        public bool UseBatchedImportanceFunction = true;
        class Baker : Baker<EnableImportanceAuthoring>
        {
            public GhostDistanceData TilingConfiguration = new GhostDistanceData
            {
                TileCenter = 0,
                TileSize = new int3(5, 100, 5),
                TileBorderWidth = 1,
            };

            public override void Bake(EnableImportanceAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new EnableImportance
                {
                    Enabled = authoring.Enabled,
                    UseBatchedImportanceFunction = authoring.UseBatchedImportanceFunction,
                    TilingConfiguration = TilingConfiguration,
                });
            }
        }
    }
}

