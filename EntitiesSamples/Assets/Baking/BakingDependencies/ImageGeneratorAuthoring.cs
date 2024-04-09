using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Baking.BakingDependencies
{
#if !UNITY_DISABLE_MANAGED_COMPONENTS
    public class ImageGeneratorAuthoring : MonoBehaviour
    {
        // The image is expected to have compression set to none and read/write enabled.
        public Texture2D Image;
        public ImageGeneratorInfo Info;

        class Baker : Baker<ImageGeneratorAuthoring>
        {
            public override void Bake(ImageGeneratorAuthoring authoring)
            {
                // The following 4 things should cause this baker to rerun:

                // 1. Modifying the image.
                DependsOn(authoring.Image);

                // 2. Modifying the ImageGeneratorInfo scriptable object.
                DependsOn(authoring.Info);

                // 3. Modifying the Mesh referenced by ImageGeneratorInfo.
                DependsOn(authoring.Info.Mesh);

                // 4. Modifying the Material referenced by ImageGeneratorInfo.
                DependsOn(authoring.Info.Material);

                // Check that the authoring component is properly setup, early out if not.
                // Notice that those checks are made *after* declaring the dependencies, this is intentional.
                // Because a null reference can be two things:
                // - An actual null reference (nothing is set)
                // - A reference to something missing (e.g. a texture which has been deleted from the assets folder)
                // In the second case, registering a dependency on a reference to a missing asset will cause
                // the baker to rerun when the missing asset is restored. This is the intended behavior.
                if (authoring.Info == null) return;
                if (authoring.Info.Mesh == null) return;
                if (authoring.Image == null) return;

                // It is important to access other authoring components by using the GetComponent methods, because
                // doing so registers dependencies. If we would have used authoring.transform here instead, a
                // dependency would not have been registered, and moving the authoring GameObject while live baking
                // would not rerun the baker as it should.
                var transform = GetComponent<Transform>();

                var spacing = authoring.Info.Spacing;
                var sizeX = authoring.Image.width;
                var sizeY = authoring.Image.height;
                var centerOffset = transform.TransformPoint(new float3(sizeX - 1, sizeY - 1, 0) / -2.0f);
                var axisX = transform.right;
                var axisY = transform.up;

                // The additional entities created by this baker will have to be further processed
                // by a baking system. In order to keep track of those entities and communicate between
                // the baker and the baking system, the entities are stored in a dynamic array attached to
                // the current entity.
                var mainEntity = GetEntity(TransformUsageFlags.None);
                var entities = AddBuffer<ImageGeneratorEntity>(mainEntity);

                var typeSet = new ComponentTypeSet(typeof(LocalTransform),
                    typeof(LocalToWorld),
                    typeof(URPMaterialPropertyBaseColor));

                for (int y = 0; y < sizeY; y++)
                {
                    for (int x = 0; x < sizeX; x++)
                    {
                        var pixel = authoring.Image.GetPixel(x, y);

                        // Skip fully transparent pixels.
                        if (pixel.a == 0) continue;

                        // ManualOverride prevents the transform baking system from adding transform components
                        // to those entities, which would conflict with the ones we are explicitly adding here.
                        var entity = CreateAdditionalEntity(TransformUsageFlags.ManualOverride);

                        // Adding all the components at once and subsequently setting their values is more efficient
                        // than adding them one by one. We avoid moving the entity across multiple archetypes this way.

                        AddComponent(entity, typeSet);

                        float3 localPosition = (axisX * x + axisY * y + centerOffset) * (1 + spacing);

                        SetComponent(entity, LocalTransform.FromPosition(localPosition));
                        SetComponent(entity, new URPMaterialPropertyBaseColor { Value = (Vector4)pixel });

                        entities.Add(entity);
                    }
                }

                // The rendering of entities requires an array of meshes and materials, and each entity identifies
                // the mesh and material it uses by index. In this case, all the entities we have created will use
                // the same mesh and the same material. Those still have to be provided as two arrays.
                AddComponentObject(mainEntity, new MeshArrayBakingType
                {
                    meshArray = new RenderMeshArray(new[] { authoring.Info.Material }, new[] { authoring.Info.Mesh })
                });

                // The only purpose of the current entity is to forward information to the baking system. It is
                // useless at runtime. By adding BakingOnlyEntity to it, we request that the baking process should
                // leave this entity behind when merging the results of baking into the destination world.
                AddComponent<BakingOnlyEntity>(mainEntity);
            }
        }
    }

    // RenderMeshArray is required to initialize entities for rendering. But this is done in a baking system
    // while the data (mesh and material) are coming from the baker. To communicate between the two, a baking
    // type is used. Baking types are not carried over to the destination world and remain in the baking world.
    [BakingType]
    public class MeshArrayBakingType : IComponentData
    {
        public RenderMeshArray meshArray;
    }

    // The set of entities created by the baker (one per pixel in the image) need to be sent to the baking system
    // in order to be properly setup as renderable entities. To this purpose, a baking type dynamic buffer is used.
    [BakingType]
    public struct ImageGeneratorEntity : IBufferElementData
    {
        Entity m_Value;

        // Implicit casting operators from and to the value embedded in a buffer element data makes it convenient to use.
        public static implicit operator Entity(ImageGeneratorEntity e) => e.m_Value;
        public static implicit operator ImageGeneratorEntity(Entity e) => new() { m_Value = e };
    }
#endif
}
