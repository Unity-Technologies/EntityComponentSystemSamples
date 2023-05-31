using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Collider = Unity.Physics.Collider;

namespace Common.Scripts
{
    public static class SceneCreationUtilities
    {
        static readonly Type k_DrawComponent = Type.GetType(
            Assembly.CreateQualifiedName("Unity.Physics.Hybrid", "Unity.Physics.Authoring.AppendMeshColliders"))
            .GetNestedType("GetMeshes", BindingFlags.Public);

        static readonly MethodInfo k_DrawComponent_BuildDebugDisplayMesh = k_DrawComponent
            .GetMethod("BuildDebugDisplayMesh", BindingFlags.Static | BindingFlags.Public, null,
            new[] { typeof(BlobAssetReference<Collider>), typeof(float) }, null);

        static readonly Type k_DisplayResult = k_DrawComponent.GetNestedType("DisplayResult");

        static readonly FieldInfo k_DisplayResultsMesh = k_DisplayResult.GetField("Mesh");
        static readonly PropertyInfo k_DisplayResultsTransform = k_DisplayResult.GetProperty("Transform");

        public static Mesh CreateMeshFromCollider(BlobAssetReference<Collider> collider)
        {
            var mesh = new Mesh { hideFlags = HideFlags.DontSave };
            var instances = new List<CombineInstance>(8);
            var numVertices = 0;
            foreach (var displayResult in (IEnumerable)k_DrawComponent_BuildDebugDisplayMesh.Invoke(null,
                new object[] { collider, 1.0f }))
            {
                var instance = new CombineInstance
                {
                    mesh = k_DisplayResultsMesh.GetValue(displayResult) as Mesh,
                    transform = (float4x4)k_DisplayResultsTransform.GetValue(displayResult)
                };
                instances.Add(instance);
                numVertices += mesh.vertexCount;
            }

            mesh.indexFormat = numVertices > UInt16.MaxValue
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.CombineMeshes(instances.ToArray());
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
