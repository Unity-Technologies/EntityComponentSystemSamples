using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace Miscellaneous.ProceduralMesh
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct ProceduralMeshBakingSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Execute.ProceduralMesh>();
        }

        // This OnUpdate accesses managed objects and so cannot be Burst-compiled.
        public void OnUpdate(ref SystemState state)
        {
            var mesh = new Mesh();
            mesh.vertices = new[] { Vector3.up, Vector3.left, Vector3.right };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 1 };

            var desc = new RenderMeshDescription(ShadowCastingMode.Off);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var meshArray = new RenderMeshArray(new[] { material }, new[] { mesh });
            var materialMeshInfo = MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0);

            var query = SystemAPI.QueryBuilder().WithAll<BakingProcedural>().Build();
            var entities = query.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                RenderMeshUtility.AddComponents(entity, state.EntityManager, desc, meshArray, materialMeshInfo);
            }
        }
    }
}
