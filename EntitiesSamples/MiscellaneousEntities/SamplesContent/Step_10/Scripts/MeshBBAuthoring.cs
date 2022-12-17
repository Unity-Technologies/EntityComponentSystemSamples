using UnityEngine;
using UnityObject = UnityEngine.Object;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Hash128 = Unity.Entities.Hash128;

namespace Advanced.BlobAssets
{
    public class MeshBBAuthoring : MonoBehaviour
    {
        public float MeshScale = 1;
        public Mesh Mesh;

        class Baker : Baker<MeshBBAuthoring>
        {
            public override void Bake(MeshBBAuthoring authoring)
            {
                var meshVertices = new NativeList<MeshVertex>(Allocator.Temp);
                var vertices = new List<Vector3>(4096);

                // Compute the blob asset hash based on Authoring properties
                var mesh = authoring.Mesh;
                var hasMesh = mesh != null;
                var meshHashCode = hasMesh ? mesh.GetHashCode() : 0;
                var hash = new Hash128((uint) meshHashCode, (uint) authoring.MeshScale.GetHashCode(), 0, 0);

                // Copy the mesh vertices into the dynamic buffer array
                if (hasMesh)
                {
                    mesh.GetVertices(vertices);
                    for (int i = 0; i < vertices.Count; i++)
                    {
                        var p = vertices[i];
                        meshVertices.Add(new MeshVertex() {Value = p});
                    }
                }

                // Add the dynamic buffer with the vertices to create the BlobAsset in the BakingSystem
                var buffer = AddBuffer<MeshVertex>();
                buffer.AddRange(meshVertices.AsArray());

                // Add the hash and scale to for the BlobAsset creation
                AddComponent(new RawMeshComponent()
                {
                    MeshScale = authoring.MeshScale,
                    Hash = hash
                });

                // Add the Component that will hold the BlobAssetReference later
                AddComponent(new MeshBBComponent());
            }
        }
    }
}
