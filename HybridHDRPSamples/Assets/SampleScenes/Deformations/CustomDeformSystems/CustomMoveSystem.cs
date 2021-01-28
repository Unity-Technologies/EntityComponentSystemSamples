using Unity.Deformations;
using Unity.Entities;
using UnityEngine;


abstract class UpdateBlendWeightsValuesBase : SystemBase
{
    protected override void OnUpdate()
    {
        var et = (float)Time.ElapsedTime;
        Entities.ForEach((
                            ref DynamicBuffer<BlendShapeWeight> weights,
                            in UpdateBlendWeightData bwData
                         ) =>
            {
                var weightValue = Mathf.Abs(Mathf.Sin(et * bwData.Value) * 100f);
                for (int index = 0; index < weights.Length; ++index)
                {
                    weights[index] = new BlendShapeWeight { Value = weightValue };
                }
            }
        ).ScheduleParallel();
    }
}


abstract class UpdateBoneTransformsBase : SystemBase
{
    protected override void OnUpdate()
    {
        var sin = Mathf.Sin((float)Time.ElapsedTime);
        Entities.ForEach(
             (
                ref DynamicBuffer <ModifiedTransform> localToRoots,
                in DynamicBuffer<RotatingBoneTransform> bones
             ) =>
             {
                 for (int index = 0; index < localToRoots.Length; ++ index)
                 {
                     var rotData = bones[index];
                     var boneTrans = rotData.Value;
                     if(rotData.DoTheThing)
                     {
                         Matrix4x4 mat = boneTrans;
                         var rotation = Quaternion.Euler(0, 0, sin * rotData.Degrees);
                         boneTrans = (mat.inverse * Matrix4x4.Rotate(rotation)).inverse;
                     }

                     localToRoots[index] = new ModifiedTransform { Value = boneTrans };
                 }
             }
         ).ScheduleParallel();
    }
}
