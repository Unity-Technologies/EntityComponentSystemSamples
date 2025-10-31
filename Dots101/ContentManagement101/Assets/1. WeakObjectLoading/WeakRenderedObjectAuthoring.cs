using Unity.Entities;
using Unity.Entities.Content;
using Unity.Entities.Serialization;
using UnityEngine;

namespace ContentManagement.Sample
{
#if UNITY_EDITOR
    //  This authoring component adds two components with WeakObjectReferences: one component for a mesh and one for a list of materials.
    //  If the UseUntypedId flag is set, it instead adds two components with UntypedWeakReferenceIds.
    //  
    //  A WeakObjectReference is basically a typed wrapper around an UntypedWeakReferenceId that is slightly more convenient to use.
    //  Generally, using WeakObjectReference is preferred unless you need a reference whose asset type isn't fixed at compile time,
    //  in which case you'll need an UntypedWeakReferenceId.
    //  Furthermore, the LocalContent component is assigned to the resulting entity,
    //  enabling the LoadingLocalCatalogSystem to load the files directly from disk.
    public class WeakRenderedObjectAuthoring : MonoBehaviour
    {
        public bool UseUntypedId;

        public Mesh Mesh;
        public Material[] Materials;  // an array because a single mesh can have multiple materials 

        class Baker : Baker<WeakRenderedObjectAuthoring>
        {
            public override void Bake(WeakRenderedObjectAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                var mesh = authoring.Mesh;
                var materials = authoring.Materials;

                // This allows the [LoadingLocalCatalogSystem] to runs and load the content,
                // then the system [WeakObjectLoadingSystem] can run and connect the references.
                AddComponent<LocalContent>(entity);

                if (authoring.UseUntypedId)
                {
                    AddComponent(entity, new WeakMeshUntyped
                    {
                        Value = UntypedWeakReferenceId.CreateFromObjectInstance(mesh)
                    });

                    var matsBuffer = AddBuffer<WeakMaterialUntyped>(entity);
                    foreach (var mat in materials)
                    {
                        matsBuffer.Add(new WeakMaterialUntyped
                        {
                            Value = UntypedWeakReferenceId.CreateFromObjectInstance(mat)
                        });
                    }
                }
                else
                {
                    AddComponent(entity, new WeakMesh
                    {
                        Value = new WeakObjectReference<Mesh>(mesh),
                    });

                    var matsBuffer = AddBuffer<WeakMaterial>(entity);
                    foreach (var mat in materials)
                    {
                        matsBuffer.Add(new WeakMaterial
                        {
                            Value = new WeakObjectReference<Material>(mat),
                        });
                    }
                }
            }
        }
    }
#endif

    public struct WeakMeshUntyped : IComponentData
    {
        public UntypedWeakReferenceId Value;
    }

    public struct WeakMaterialUntyped : IBufferElementData
    {
        public UntypedWeakReferenceId Value;
    }
    
    public struct WeakMesh : IComponentData
    {
        public WeakObjectReference<Mesh> Value;
    }
    
    public struct WeakMaterial : IBufferElementData
    {
        public WeakObjectReference<Material> Value;
    }
}