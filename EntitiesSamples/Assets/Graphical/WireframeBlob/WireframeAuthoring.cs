using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Graphical.PrefabInitializer
{
    [RequireComponent(typeof(MeshFilter))]
    public class WireframeAuthoring : MonoBehaviour
    {
        class Baker : Baker<WireframeAuthoring>
        {
            public override void Bake(WireframeAuthoring authoring)
            {
                var sharedMesh = GetComponent<MeshFilter>().sharedMesh;
                DependsOn(sharedMesh);
                var meshVertices = sharedMesh.vertices;
                var meshIndices = sharedMesh.GetIndices(0); // assume one single submesh and triangles topology

                using var blobBuilder = new BlobBuilder(Allocator.Temp);
                ref var blobData = ref blobBuilder.ConstructRoot<LocalSpaceBlob>();

                var blobVerticesBuilder = blobBuilder.Allocate(ref blobData.Vertices, meshVertices.Length);
                for (int i = 0; i < meshVertices.Length; i++)
                {
                    blobVerticesBuilder[i] = meshVertices[i];
                }

                var lineHash = new NativeHashSet<int2>(meshIndices.Length, Allocator.Temp);
                for (int i = 0; i < meshIndices.Length; i += 3)
                {
                    var a = meshIndices[i + 0];
                    var b = meshIndices[i + 1];
                    var c = meshIndices[i + 2];
                    lineHash.Add(a < b ? new int2(a, b) : new int2(b, a));
                    lineHash.Add(b < c ? new int2(b, c) : new int2(c, b));
                    lineHash.Add(c < a ? new int2(c, a) : new int2(a, c));
                }

                var blobSegmentsBuilder = blobBuilder.Allocate(ref blobData.Segments, lineHash.Count);
                int segmentIndex = 0;
                foreach (var line in lineHash)
                {
                    blobSegmentsBuilder[segmentIndex] = line;
                    segmentIndex++;
                }

                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new WireframeLocalSpace
                {
                    Blob = blobBuilder.CreateBlobAssetReference<LocalSpaceBlob>(Allocator.Persistent)
                });
            }
        }
    }

    public struct WireframeLocalSpace : IComponentData
    {
        public BlobAssetReference<LocalSpaceBlob> Blob;
    }

    public struct LocalSpaceBlob
    {
        public BlobArray<float3> Vertices;
        public BlobArray<int2> Segments;
    }

    public struct WireframeWorldSpace : IBufferElementData
    {
        public float3 Position;
    }
}
