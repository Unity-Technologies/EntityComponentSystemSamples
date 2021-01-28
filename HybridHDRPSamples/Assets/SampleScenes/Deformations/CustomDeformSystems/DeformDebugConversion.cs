using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Deformations;

[ConverterVersion("deformdebug", 2)]
[UpdateInGroup(typeof(GameObjectConversionGroup))]
class DeformDebugConversion : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((DeformDebugComponent deformMesh) =>
        {
            Debug.Assert(deformMesh.GetComponentsInChildren<SkinnedMeshRenderer>().Length == 1, "More than one SMR in DeformDebug component children");

            var meshRenderer = deformMesh.GetComponentInChildren<SkinnedMeshRenderer>();
            var entity = GetPrimaryEntity(meshRenderer);
            var bones = meshRenderer.bones;

            DstEntityManager.AddBuffer<RotatingBoneTransform>(entity);
            DstEntityManager.AddBuffer<ModifiedTransform>(entity);

            var rotatingBonesArray = DstEntityManager.GetBuffer<RotatingBoneTransform>(entity);
            var movedTransformsArray = DstEntityManager.GetBuffer<ModifiedTransform>(entity);

            rotatingBonesArray.ResizeUninitialized(bones.Length);
            movedTransformsArray.ResizeUninitialized(bones.Length);

            for (int boneIndex = 0; boneIndex != bones.Length; ++boneIndex)
            {
                var bone = meshRenderer.bones[boneIndex];
                var boneMovementComponent = new RotatingBoneTransform
                {
                    Value = meshRenderer.bones[boneIndex].localToWorldMatrix
                };


                var rotationObject = bone.GetComponent<RotateTransformComponent>();
                boneMovementComponent.DoTheThing = rotationObject != null;
                if (rotationObject)
                {
                    boneMovementComponent.Degrees = rotationObject.Degrees;
                }
                rotatingBonesArray[boneIndex] = boneMovementComponent;
            
            }

            var sharedMesh = meshRenderer.sharedMesh;
            if (sharedMesh.boneWeights.Length > 0 && sharedMesh.bindposes.Length > 0)
            {
                DstEntityManager.AddBuffer<BindPose>(entity);
                var bindPoseArray = DstEntityManager.GetBuffer<BindPose>(entity);
                bindPoseArray.ResizeUninitialized(bones.Length);
                for (int boneIndex = 0; boneIndex != bones.Length; ++boneIndex)
                {
                    var bindPose = meshRenderer.sharedMesh.bindposes[boneIndex];
                    bindPoseArray[boneIndex] = new BindPose { Value = bindPose };
                }
            }

            if(sharedMesh.blendShapeCount > 0)
            {

                DstEntityManager.AddComponentData(entity, new UpdateBlendWeightData { Value = 0.3f });
            }
        });
    }
}

internal struct RotatingBoneTransform : IBufferElementData
{
    public float4x4 Value;
    public float Degrees;
    public bool DoTheThing;
}

internal struct ModifiedTransform : IBufferElementData
{
    public float4x4 Value;
}

internal struct BindPose : IBufferElementData
{
    public float4x4 Value;
}

internal struct UpdateBlendWeightData : IComponentData
{
    public float Value;
}
