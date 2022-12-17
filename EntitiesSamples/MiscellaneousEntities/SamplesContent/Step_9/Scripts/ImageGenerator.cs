using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace ImageGeneratorSample
{
    public class ImageGeneratorAuthoring : MonoBehaviour
    {
        // The image is expected to have compression set to none and read/write enabled.
        public Texture2D Image;
        public ImageGeneratorInfo Info;

        class Baker : Baker<ImageGeneratorAuthoring>
        {
            static ComponentTypeSet s_ComponentsToAdd = new(new ComponentType[]
            {
                typeof(LocalTransform),
                typeof(LocalToWorld),
                typeof(URPMaterialPropertyBaseColor)
            });

            public override void Bake(ImageGeneratorAuthoring authoring)
            {
                // The following 5 things should cause this baker to rerun:

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
                var entities = AddBuffer<ImageGeneratorEntity>();

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
                        AddComponent(entity, s_ComponentsToAdd);

                        float3 localPosition = (axisX * x + axisY * y + centerOffset) * (1 + spacing);

                        SetComponent(entity, LocalTransform.FromPosition(localPosition));
                        SetComponent(entity, new URPMaterialPropertyBaseColor { Value = (Vector4)pixel });

                        entities.Add(entity);
                    }
                }

                // The rendering of entities requires an array of meshes and materials, and each entity identifies
                // the mesh and material it uses by index. In this case, all the entities we have created will use
                // the same mesh and the same material. Those still have to be provided as two arrays.
                AddComponentObject(new MeshArrayBakingType
                {
                    meshArray = new RenderMeshArray(new[] { authoring.Info.Material }, new[] { authoring.Info.Mesh })
                });

                // The only purpose of the current entity is to forward information to the baking system. It is
                // useless at runtime. By adding BakingOnlyEntity to it, we request that the baking process should
                // leave this entity behind when merging the results of baking into the destination world.
                AddComponent<BakingOnlyEntity>();
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

    // This is a baking system, a system that runs only in the baking world, after the bakers have run. It provides
    // more flexibility (e.g. accessing any entity) but doesn't allow expressing dependencies. This is the reason why
    // this sample relies on communication between the baker (registering dependencies) and the system (doing the
    // setup required by the renderable entities, for which no API exists to do so directly in the baker).
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    [BurstCompile]
    public partial struct ImageGeneratorBakingSystem : ISystem
    {
        EntityQuery m_ImageGeneratorEntitiesQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Gathering all the entities that have been processed by the ImageGenerator baker.
            m_ImageGeneratorEntitiesQuery = SystemAPI.QueryBuilder().WithAll<ImageGeneratorEntity, MeshArrayBakingType>().Build();

            // And in order to only process the ones that have been updated, this system is made reactive. This means
            // that it will only process the entities for which the ImageGeneratorEntity buffer has been updated. In other words,
            // the entities for which the baker has run during this baking pass. This filter could have also used the
            // MeshArrayBakingType component instead, since both are added by the same baker it doesn't make a difference.
            m_ImageGeneratorEntitiesQuery.SetChangedVersionFilter(ComponentType.ReadOnly<ImageGeneratorEntity>());
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            // The query can be empty if there is either no entity with the required components, or not entity that has
            // been processed during the current baking pass.
            if (m_ImageGeneratorEntitiesQuery.IsEmpty) return; 

            var renderMeshDescription = new RenderMeshDescription(UnityEngine.Rendering.ShadowCastingMode.Off);
            var materialMeshInfo = MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0);

            // Since the following loops does structural changes, the contents of the query cannot be iterated with a foreach.
            // So instead, each one is processed by doing a random access of the component values. This is more expensive, but
            // the amount of entities is expected to be low. Because there is only one such entity per ImageGenerator, and also
            // because the system being reactive, only the ones that have been updated need to be processed here.
            var entities = m_ImageGeneratorEntitiesQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
            {
                var bakingType = SystemAPI.ManagedAPI.GetComponent<MeshArrayBakingType>(entity);
                var bakingEntities = SystemAPI.GetBuffer<ImageGeneratorEntity>(entity).Reinterpret<Entity>().ToNativeArray(Allocator.Temp);

                foreach (var bakingEntity in bakingEntities)
                {
                    RenderMeshUtility.AddComponents(bakingEntity, state.EntityManager, renderMeshDescription, bakingType.meshArray, materialMeshInfo);
                }
            }
        }
    }
}
