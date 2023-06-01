using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

namespace Miscellaneous.ProceduralMesh
{
    public partial struct ProceduralMeshRuntimeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RuntimeProcedural>();
            state.RequireForUpdate<Execute.ProceduralMesh>();
        }

        // This OnUpdate accesses managed objects and so cannot be Burst-compiled.
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            var mesh = new Mesh();
            mesh.vertices = new[] { Vector3.up, Vector3.left, Vector3.right };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 1 };

            var desc = new RenderMeshDescription(ShadowCastingMode.Off);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var meshArray = new RenderMeshArray(new[] { material }, new[] { mesh });
            var materialMeshInfo = MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0);

            var query = SystemAPI.QueryBuilder().WithAll<RuntimeProcedural>().Build();
            var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                RenderMeshUtility.AddComponents(entity, state.EntityManager, desc, meshArray, materialMeshInfo);
            }
        }
    }
}
