using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[ExecuteAlways]
[UpdateAfter(typeof(TransformSystemGroup))]
class MoveAnimatedMatrices : UpdateBoneTransformsBase
{
}

[ExecuteAlways]
[UpdateAfter(typeof(MoveAnimatedMatrices))]
class UpdateBlendWeights : UpdateBlendWeightsValuesBase
{
}

[ExecuteAlways]
[UpdateAfter(typeof(UpdateBlendWeights))]
class SkinMatrixToBindPoseSpaceSystem : SkinMatrixToBindPoseSpaceSystemBase
{
}

