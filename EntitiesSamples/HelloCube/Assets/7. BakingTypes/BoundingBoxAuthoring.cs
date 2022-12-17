using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace HelloCube.BakingTypes
{
    public class BoundingBoxAuthoring : MonoBehaviour
    {
        class Baker : Baker<BoundingBoxAuthoring>
        {
            public override void Bake(BoundingBoxAuthoring authoring)
            {
                // Get a dependency on the mesh and the transform
                // This ensures that if either of these change, the Baker is rerun
                var mesh = GetComponent<MeshFilter>().sharedMesh;
                var pos = GetComponent<Transform>().position;

                DependsOn(mesh);
                var parentBox = GetComponentInParent<CompoundBBAuthoring>();
                var parentEntity = GetEntity(parentBox);

                var hasMesh = mesh != null;
                float xp = float.MinValue, yp = float.MinValue, zp = float.MinValue;
                float xn = float.MaxValue, yn = float.MaxValue, zn = float.MaxValue;

                // Calculate the bounding box
                if (hasMesh)
                {
                    var vertices = new List<Vector3>(4096);
                    mesh.GetVertices(vertices);
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        var p = vertices[i];
                        xp = math.max(p.x, xp);
                        yp = math.max(p.y, yp);
                        zp = math.max(p.z, zp);
                        xn = math.min(p.x, xn);
                        yn = math.min(p.y, yn);
                        zn = math.min(p.z, zn);
                    }
                }
                else
                {
                    xp = yp = zp = xn = yn = zn = 0;
                }

                var minBoundingBox = new float3(xn, yn, zn) + new float3(pos.x, pos.y, pos.z);
                var maxBoundingBox = new float3(xp, yp, zp) + new float3(pos.x, pos.y, pos.z);

                AddComponent(new BoundingBoxComponent()
                {
                    Parent = parentEntity,
                    MinBBVertex = minBoundingBox,
                    MaxBBVertex = maxBoundingBox
                });
                AddComponent<ChangesComponent>();
            }
        }
    }
    
    /// BakingType components are present in the Baking process, but not in the destination world.
    /// It can be used to get data from a Baker to a Baking System.
    [BakingType]
    public struct BoundingBoxComponent : IComponentData
    {
        public Entity Parent;
        public float3 MinBBVertex;
        public float3 MaxBBVertex;
    }

    /// TemporaryBakingType components are removed after a single Baking pass.
    /// Because of this only the first frame after a change is made and the Baker is run, this component is available.
    /// This component is used to only run a Baking System once for each change (iso every time)
    [TemporaryBakingType]
    public struct ChangesComponent : IComponentData{}

}
