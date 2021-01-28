using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Deformations;


abstract class SkinMatrixToBindPoseSpaceSystemBase : SystemBase
{
    protected override void OnUpdate()
    {
        Entities.ForEach((  ref DynamicBuffer<SkinMatrix> skinMatrices,
                            in DynamicBuffer<ModifiedTransform> modifiedMatrices,
                            in DynamicBuffer<BindPose> bindposes) =>
        {
            for (int i = 0; i < skinMatrices.Length; ++i)
            {
                var skinMat = math.mul(modifiedMatrices[i].Value,
                                       bindposes[i].Value);

                skinMatrices[i] = new SkinMatrix
                {
                    Value = new float3x4(skinMat.c0.xyz,
                                         skinMat.c1.xyz,
                                         skinMat.c2.xyz,
                                         skinMat.c3.xyz)
                };
            }
        }).ScheduleParallel();

    }
}

