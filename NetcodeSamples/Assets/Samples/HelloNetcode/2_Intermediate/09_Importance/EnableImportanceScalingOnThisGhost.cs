using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public class EnableImportanceScalingOnThisGhost : MonoBehaviour
    {
        private class BarrelAuthoringBaker : Baker<EnableImportanceScalingOnThisGhost>
        {
            public override void Bake(EnableImportanceScalingOnThisGhost authoring)
            {
                // Note: This relies on setting `GhostDistancePartitioningSystem.AutomaticallyAddGhostDistancePartitionShared` to false.
                // Note2: This should ideally be a runtime thing, as we don't need this component on the client.
                AddSharedComponent(GetEntity(TransformUsageFlags.Dynamic), default(GhostDistancePartitionShared));
            }
        }
    }
}

