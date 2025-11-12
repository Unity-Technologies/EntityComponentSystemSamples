using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace ContentManagement.Sample
{
    // This system looks for the entities that have the components added by WeakRenderedObjectAuthoring
    // and turns them into renderable entities by:
    // 1. asynchronously loading their weakly referenced meshes and materials
    // 2. once the assets have loaded, adding the required rendering components
    public partial struct WeakObjectLoadingSystem : ISystem
    {
        private EntityQuery weakQuery;
        private EntityQuery weakUntypedQuery;

        public void OnCreate(ref SystemState state)
        {
            // Renderable entities have the RenderBounds component (among other rendering components), so
            // these queries match only entities that haven't yet been made renderable.
            weakQuery = SystemAPI.QueryBuilder().WithAll<WeakMesh, WeakMaterial>().WithNone<RenderBounds>().Build();
            weakUntypedQuery = SystemAPI.QueryBuilder().WithAll<WeakMeshUntyped, WeakMaterialUntyped>()
                .WithNone<RenderBounds>().Build();

            // The system should update only if entities exist which still need to be made renderable.
            var query = SystemAPI.QueryBuilder().WithAny<WeakMesh, WeakMeshUntyped>().WithNone<RenderBounds>().Build();
            state.RequireForUpdate(query);
        }

        public void OnUpdate(ref SystemState state)
        {
#region WeakObjectReference            
            var weakEntities = weakQuery.ToEntityArray(Allocator.Temp);
            var weakMeshes = weakQuery.ToComponentDataArray<WeakMesh>(Allocator.Temp);

            // note that we can't use SystemAPI.Query for this loop because 
            // are making structural changes to the entities
            for (int i = 0; i < weakEntities.Length; i++)
            {
                var loaded = true;

                var entity = weakEntities[i];
                var mesh = weakMeshes[i];
                var materials = state.EntityManager.GetBuffer<WeakMaterial>(entity);

                // mesh load status
                var meshStatus = mesh.Value.LoadingStatus;
                if (meshStatus == ObjectLoadingStatus.None)
                {
                    Debug.Log("Initiate mesh LOAD");
                    mesh.Value.LoadAsync(); // trigger load
                }

                if (meshStatus != ObjectLoadingStatus.Completed)
                {
                    loaded = false;
                }

                // material load status
                for (int j = 0; j < materials.Length; j++)
                {
                    var mat = materials[j];
                    var materialStatus = mat.Value.LoadingStatus;
                    if (materialStatus == ObjectLoadingStatus.None)
                    {
                        Debug.Log("Initiate material LOAD");
                        mat.Value.LoadAsync(); // trigger load
                    }

                    if (materialStatus != ObjectLoadingStatus.Completed)
                    {
                        loaded = false;
                    }
                }

                if (loaded)
                {
                    Debug.Log("Creating rendered entity");
                    
                    // each rendered object has a single mesh, but the mesh may
                    // have submeshes, each with their own material
                    var meshArray = new Mesh[] { mesh.Value.Result };
                    var materialArray = new Material[materials.Length];
                    var indices = new MaterialMeshIndex[materials.Length];

                    for (int j = 0; j < materials.Length; j++)
                    {
                        materialArray[j] = materials[j].Value.Result;
                        indices[j] = new MaterialMeshIndex
                        {
                            MeshIndex = 0,
                            MaterialIndex = j,
                            SubMeshIndex = j,
                        };
                    }

                    // Add the rendering components to the entity
                    // (including RenderBounds, so the entity will hereafter 
                    // be excluded from this system's queries) 
                    RenderMeshUtility.AddComponents(entity, state.EntityManager,
                        new RenderMeshDescription(ShadowCastingMode.On),
                        new RenderMeshArray(materialArray, meshArray, indices),
                        MaterialMeshInfo.FromMaterialMeshIndexRange(0, materialArray.Length)
                    );
                }
            }
#endregion

#region UntypedWeakReferenceId
            // very similar to above but for UntypedWeakReferenceId...

            var weakUntypedEntities = weakUntypedQuery.ToEntityArray(Allocator.Temp);
            var untypedWeakMeshes = weakUntypedQuery.ToComponentDataArray<WeakMeshUntyped>(Allocator.Temp);

            for (int i = 0; i < weakUntypedEntities.Length; i++)
            {
                var loaded = true;

                var entity = weakUntypedEntities[i];
                var mesh = untypedWeakMeshes[i];
                var materials = state.EntityManager.GetBuffer<WeakMaterialUntyped>(entity);

                // mesh load status
                var meshStatus = RuntimeContentManager.GetObjectLoadingStatus(mesh.Value);
                if (meshStatus == ObjectLoadingStatus.None)
                {
                    RuntimeContentManager.LoadObjectAsync(mesh.Value);  // trigger load
                }
                if (meshStatus != ObjectLoadingStatus.Completed)
                {
                    loaded = false;
                }

                // material load status
                for (int j = 0; j < materials.Length; j++)
                {
                    var mat = materials[j];
                    var materialStatus = RuntimeContentManager.GetObjectLoadingStatus(mat.Value);
                    if (materialStatus == ObjectLoadingStatus.None)
                    {
                        RuntimeContentManager.LoadObjectAsync(mat.Value);  // trigger load
                    }

                    if (materialStatus != ObjectLoadingStatus.Completed)
                    {
                        loaded = false;
                    }
                }

                if (loaded)
                {
                    // each rendered object has a single mesh, but the mesh may
                    // have submeshes, each with their own material
                    var meshArray = new Mesh[] { RuntimeContentManager.GetObjectValue<Mesh>(mesh.Value) };
                    var materialArray = new Material[materials.Length];
                    var indices = new MaterialMeshIndex[materials.Length];

                    for (int j = 0; j < materials.Length; j++)
                    {
                        materialArray[j] = RuntimeContentManager.GetObjectValue<Material>(materials[j].Value);
                        indices[j] = new MaterialMeshIndex
                        {
                            MeshIndex = 0,
                            MaterialIndex = j,
                            SubMeshIndex = j,
                        };
                    }

                    // Add the rendering components to the entity
                    // (including RenderBounds, so the entity will hereafter be excluded from this system's queries) 
                    RenderMeshUtility.AddComponents(entity, state.EntityManager,
                        new RenderMeshDescription(ShadowCastingMode.On),
                        new RenderMeshArray(materialArray, meshArray, indices),
                        MaterialMeshInfo.FromMaterialMeshIndexRange(0, materialArray.Length)
                    );
                }
            }
#endregion
        }
    }
}