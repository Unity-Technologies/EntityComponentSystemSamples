using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

[ConverterVersion("unity", 1)]
[UpdateInGroup(typeof(GameObjectConversionGroup))]
[UpdateAfter(typeof(SkinnedMeshRendererConversion))]
class ComputeSkinMatricesConversion : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((DeformationsSampleAuthoring deformMesh, SkinnedMeshRenderer renderer) =>
        {
            var entity = GetPrimaryEntity(renderer);

            // Only execute this if we have a valid skinning setup
            var bones = renderer.bones;
            var hasSkinning = bones.Length > 0 && renderer.sharedMesh.bindposes.Length > 0;
            if (hasSkinning)
            {
                // Setup Reference to root
                var rootBone = renderer.rootBone ? renderer.rootBone : renderer.transform;
                var rootEntity = GetPrimaryEntity(rootBone);
                DstEntityManager.AddComponentData(entity, new RootEntity { Value = rootEntity });

                // World to local is required for root space conversion of the SkinMatrices
                DstEntityManager.AddComponent<WorldToLocal>(rootEntity);
                DstEntityManager.AddComponent<RootTag>(rootEntity);

                DstEntityManager.AddBuffer<BoneEntity>(entity);
                var boneEntityArray = DstEntityManager.GetBuffer<BoneEntity>(entity);
                boneEntityArray.ResizeUninitialized(bones.Length);

                for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                {
                    var bone = renderer.bones[boneIndex];
                    var boneEntity = GetPrimaryEntity(bone);
                    boneEntityArray[boneIndex] = new BoneEntity { Value = boneEntity };
                }

                // Store the bindpose for each bone
                DstEntityManager.AddBuffer<BindPose>(entity);
                var bindPoseArray = DstEntityManager.GetBuffer<BindPose>(entity);
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
                    var boneEntity = GetPrimaryEntity(bone);
                    DstEntityManager.AddComponentData(boneEntity, new BoneTag());
                }
            }

            // Override the material color of the deformation materials
            var c = deformMesh.Color.linear;
            var color = new float4(c.r, c.g, c.b, c.a);
            foreach (var rendererEntity in GetEntities(renderer))
            {
                if (DstEntityManager.HasComponent<RenderMesh>(rendererEntity))
                {
                    DstEntityManager.AddComponentData(rendererEntity, new URPMaterialPropertyBaseColor { Value = color });
                }
            }
        });
    }
}

[ConverterVersion("unity", 1)]
[UpdateInGroup(typeof(GameObjectConversionGroup))]
class AnimatePositionConversion : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((AnimatePositionAuthoring data) =>
        {
            var entity = GetPrimaryEntity(data);

            var positionAnimation = new AnimatePosition
            {
                From = data.FromPosition,
                To = data.ToPosition,
                Frequency = 1f / data.Phase,
                PhaseShift = data.Offset * data.Phase,
            };

            DstEntityManager.AddComponentData(entity, positionAnimation);
            DstEntityManager.AddComponent<Translation>(entity);
        });
    }
}

[ConverterVersion("unity", 1)]
[UpdateInGroup(typeof(GameObjectConversionGroup))]
class AnimateRotationConversion : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((AnimateRotationAuthoring data) =>
        {
            var entity = GetPrimaryEntity(data);

            var rotationAnimation = new AnimateRotation
            {
                From = quaternion.Euler(math.radians(data.FromRotation)),
                To = quaternion.Euler(math.radians(data.ToRotation)),
                Frequency = 1f / data.Phase,
                PhaseShift = data.Offset * data.Phase,
            };

            DstEntityManager.AddComponentData(entity, rotationAnimation);
            DstEntityManager.AddComponent<Rotation>(entity);
        });
    }
}

[ConverterVersion("unity", 1)]
[UpdateInGroup(typeof(GameObjectConversionGroup))]
class AnimateScaleConversion : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((AnimateScaleAuthoring data) =>
        {
            var entity = GetPrimaryEntity(data);

            var scaleAnimation = new AnimateScale
            {
                From = data.FromScale,
                To = data.ToScale,
                Frequency = 1f / data.Phase,
                PhaseShift = data.Offset * data.Phase,
            };

            DstEntityManager.AddComponentData(entity, scaleAnimation);
            DstEntityManager.AddComponent<NonUniformScale>(entity);
        });
    }
}

[ConverterVersion("unity", 1)]
[UpdateInGroup(typeof(GameObjectConversionGroup))]
class AnimateBlendShapeConversion : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((AnimateBlendShapeAuthoring data) =>
        {
            var entity = GetPrimaryEntity(data);

            var blendshapeAnimation = new AnimateBlendShape
            {
                From = data.FromWeight,
                To = data.ToWeight,
                Frequency = 1f / data.Phase,
                PhaseShift = data.Offset * data.Phase,
            };

            DstEntityManager.AddComponentData(entity, blendshapeAnimation);
        });
    }
}
