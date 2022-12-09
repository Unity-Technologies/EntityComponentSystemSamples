using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Hybrid.Baking;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

[TemporaryBakingType]
internal struct DeformationSampleData : IComponentData
{
    public UnityObjectRef<DeformationsSampleAuthoring> DeformationSample;
    public UnityObjectRef<SkinnedMeshRenderer> SkinnedMeshRenderer;
}

internal class DeformationSampleBaker : Baker<DeformationsSampleAuthoring>
{
    public override void Bake(DeformationsSampleAuthoring authoring)
    {
        var skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>(authoring);
        if (skinnedMeshRenderer == null)
            return;

        var rootTransform = skinnedMeshRenderer.rootBone ? skinnedMeshRenderer.rootBone : skinnedMeshRenderer.transform;
        DependsOn(rootTransform);
        foreach (var bone in skinnedMeshRenderer.bones)
            DependsOn(bone);

        DependsOn(skinnedMeshRenderer.sharedMesh);

        AddComponent( new DeformationSampleData()
        {
            DeformationSample = authoring,
            SkinnedMeshRenderer = skinnedMeshRenderer
        });
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
public partial class ComputeSkinMatricesBakingSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var bakingSystem = World.GetExistingSystemManaged<BakingSystem>();
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entities.ForEach((Entity entity, in DeformationSampleData deformMesh) =>
        {
            // Only execute this if we have a valid skinning setup
            var renderer = deformMesh.SkinnedMeshRenderer.Value;
            var bones = renderer.bones;
            var hasSkinning = bones.Length > 0 && renderer.sharedMesh.bindposes.Length > 0;
            if (hasSkinning)
            {
                // Setup Reference to root
                var rootBone = renderer.rootBone ? renderer.rootBone : renderer.transform;
                var rootEntity = bakingSystem.GetEntity(rootBone);
                ecb.AddComponent(entity, new RootEntity { Value = rootEntity });

                // World to local is required for root space conversion of the SkinMatrices
                ecb.AddComponent<WorldToLocal>(rootEntity);
                ecb.AddComponent<RootTag>(rootEntity);

                var boneEntityArray = ecb.AddBuffer<BoneEntity>(entity);
                boneEntityArray.ResizeUninitialized(bones.Length);

                for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                {
                    var bone = renderer.bones[boneIndex];
                    var boneEntity = bakingSystem.GetEntity(bone);
                    boneEntityArray[boneIndex] = new BoneEntity { Value = boneEntity };
                }

                // Store the bindpose for each bone
                var bindPoseArray = ecb.AddBuffer<BindPose>(entity);
                bindPoseArray.ResizeUninitialized(bones.Length);

                for (int boneIndex = 0; boneIndex != bones.Length; ++boneIndex)
                {
                    var bindPose = renderer.sharedMesh.bindposes[boneIndex];
                    bindPoseArray[boneIndex] = new BindPose { Value = bindPose };
                }

                // Add tags to the bones so we can find them later
                // when computing the SkinMatrices
                for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                {
                    var bone = renderer.bones[boneIndex];
                    var boneEntity = bakingSystem.GetEntity(bone);
                    ecb.AddComponent(boneEntity, new BoneTag());
                }
            }

            // Override the material color of the deformation materials
            var c = deformMesh.DeformationSample.Value.Color.linear;
            var color = new float4(c.r, c.g, c.b, c.a);
            var additionalEntities = EntityManager.GetBuffer<AdditionalEntitiesBakingData>(entity);
            foreach (var rendererEntity in additionalEntities.AsNativeArray())
            {
                if (EntityManager.HasComponent<RenderMesh>(rendererEntity.Value))
                {
                    ecb.AddComponent(rendererEntity.Value, new HDRPMaterialPropertyBaseColor { Value = color });
                }
            }
        }).WithEntityQueryOptions(EntityQueryOptions.IncludeDisabledEntities).WithoutBurst().WithStructuralChanges().Run();

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}

class AnimatePositionBaker : Baker<AnimatePositionAuthoring>
{
    public override void Bake(AnimatePositionAuthoring authoring)
    {
        var positionAnimation = new AnimatePosition
        {
            From = authoring.FromPosition,
            To = authoring.ToPosition,
            Frequency = 1f / authoring.Phase,
            PhaseShift = authoring.Offset * authoring.Phase,
        };

        AddComponent(positionAnimation);
        AddComponent<Translation>();
    }
}

class AnimateRotationBaker : Baker<AnimateRotationAuthoring>
{
    public override void Bake(AnimateRotationAuthoring authoring)
    {
        var rotationAnimation = new AnimateRotation
        {
            From = quaternion.Euler(math.radians(authoring.FromRotation)),
            To = quaternion.Euler(math.radians(authoring.ToRotation)),
            Frequency = 1f / authoring.Phase,
            PhaseShift = authoring.Offset * authoring.Phase,
        };
        AddComponent(rotationAnimation);
        AddComponent<Rotation>();
    }
}

class AnimateScaleBaker : Baker<AnimateScaleAuthoring>
{
    public override void Bake(AnimateScaleAuthoring authoring)
    {
        var scaleAnimation = new AnimateScale
        {
            From = authoring.FromScale,
            To = authoring.ToScale,
            Frequency = 1f / authoring.Phase,
            PhaseShift = authoring.Offset * authoring.Phase,
        };

        AddComponent(scaleAnimation);
        AddComponent<NonUniformScale>();
    }
}

class AnimateBlendShapeBaker : Baker<AnimateBlendShapeAuthoring>
{
    public override void Bake(AnimateBlendShapeAuthoring authoring)
    {
        var blendshapeAnimation = new AnimateBlendShape
        {
            From = authoring.FromWeight,
            To = authoring.ToWeight,
            Frequency = 1f / authoring.Phase,
            PhaseShift = authoring.Offset * authoring.Phase,
        };

        AddComponent(blendshapeAnimation);
    }
}
