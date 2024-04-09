using UnityEngine;
using UnityObject = UnityEngine.Object;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using Hash128 = Unity.Entities.Hash128;

namespace Baking.BlobAssetBakingSystem
{
#if UNITY_EDITOR
    public class MeshBBAuthoring : MonoBehaviour
    {
        public float MeshScale = 1;

        class Baker : Baker<MeshBBAuthoring>
        {
            public override void Bake(MeshBBAuthoring authoring)
            {
                var meshVertices = new NativeList<MeshVertex>(Allocator.Temp);
                var vertices = new List<Vector3>(4096);

                // Compute the blob asset hash based on Authoring properties
                var mesh = GetComponent<MeshFilter>().sharedMesh;
                DependsOn(mesh);

                var hasMesh = mesh != null;
                var assetPath = AssetDatabase.GetAssetPath(mesh);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh.GetInstanceID(), out string guid, out long localId);
                Hash128 hash = default;

                // Here we get a hash from the mesh to use it later to de-duplicate the blob assets
                // mesh.GetHashCode() will not update if the mesh is changed outside of the editor
                // So we get the hash from the asset path through the asset database.
                // However, trying to get a hash from the Default Resources (cube, capsule eg.) will always give a hash of 0s
                // In addition, it won't change (except for a Unity version update) so we can use GetHashCode
                if (IsBuiltin(new GUID(guid)))
                {
                    hash = new Hash128((uint) localId, (uint) authoring.MeshScale.GetHashCode(), 0, 0);
                }
                else if (UnityEditor.EditorUtility.IsPersistent(mesh))
                {
                    hash = AssetDatabase.GetAssetDependencyHash(assetPath);
                }
                else
                {
                    Debug.LogError("This sample does not support procedural meshes stored in the scene");
                }

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
                var entity = GetEntity(TransformUsageFlags.None);
                var buffer = AddBuffer<MeshVertex>(entity);
                buffer.AddRange(meshVertices.AsArray());

                // Add the hash and scale to for the BlobAsset creation
                AddComponent(entity, new RawMesh()
                {
                    MeshScale = authoring.MeshScale,
                    Hash = hash
                });

                // Add the Component that will hold the BlobAssetReference later 
                AddComponent(entity, new MeshBB());
            }

            public static GUID UnityEditorResources = new GUID("0000000000000000d000000000000000");
            public static GUID UnityBuiltinResources = new GUID("0000000000000000e000000000000000");
            public static GUID UnityBuiltinExtraResources = new GUID("0000000000000000f000000000000000");

            public static bool IsBuiltin(in GUID g) =>
                g == UnityEditorResources ||
                g == UnityBuiltinResources ||
                g == UnityBuiltinExtraResources;
        }
    }
#endif

    public struct MeshBBBlobAsset
    {
        public float3 MinBoundingBox;
        public float3 MaxBoundingBox;
    }

    public struct MeshBB : IComponentData
    {
        public BlobAssetReference<MeshBBBlobAsset> BlobData;
    }

    [BakingType]
    public struct RawMesh : IComponentData
    {
        public float MeshScale;
        public Hash128 Hash;
    }

    [BakingType]
    public struct MeshVertex : IBufferElementData
    {
        public float3 Value;
    }
}
