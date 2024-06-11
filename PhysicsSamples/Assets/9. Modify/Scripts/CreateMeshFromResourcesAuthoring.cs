using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Unity.Physics
{
    // Takes the Materials and Meshes specified in the GameObject and writes that data into a RenderMeshArray component
    public class CreateMeshFromResourcesAuthoring : MonoBehaviour
    {
        public UnityEngine.Material MaterialA;
        public UnityEngine.Material MaterialB;
        public UnityEngine.Material MaterialC;
        public UnityEngine.Mesh MeshA;
        public UnityEngine.Mesh MeshB;

        class CreateMeshFromResourcesBaker : Baker<CreateMeshFromResourcesAuthoring>
        {
            public override void Bake(CreateMeshFromResourcesAuthoring authoring)
            {
                var materialA = authoring.MaterialA;
                var materialB = authoring.MaterialB;
                var materialC = authoring.MaterialC;
                var meshA = authoring.MeshA;
                var meshB = authoring.MeshB;

                var createComponent = new RenderMeshArray(
                    new[] { materialA, materialB, materialC },
                    new[] { meshA, meshB, meshB });

                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddSharedComponentManaged(entity, createComponent);
                AddComponent(entity, new ResourcesLoadedTag());
            }
        }
    }

    // Use as a tag to more easily identify the entity that has the RenderMeshArray data for the loaded resources
    public struct ResourcesLoadedTag : IComponentData {}
}
