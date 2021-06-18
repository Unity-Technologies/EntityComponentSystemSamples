using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// Very simple animation curve blob data that uses linear interpolation at fixed intervals.
/// Blob data is constructed from a UnityEngine.AnimationCurve
/// </summary>
public struct SimpleAnimationBlob
{
    BlobArray<float> Keys;
    float            InvLength;
    float            KeyCount;

    // When t exceeds the curve time, repeat it
    public float CalculateNormalizedTime(float t)
    {
        float normalizedT = t * InvLength;
        return normalizedT - math.floor(normalizedT);
    }

    public float Evaluate(float t)
    {
        // Loops time value between 0...1
        t = CalculateNormalizedTime(t);

        // Find index and interpolation value in the array
        float sampleT = t * KeyCount;
        var sampleTFloor = math.floor(sampleT);

        float interp = sampleT - sampleTFloor;
        var index = (int)sampleTFloor;

        return math.lerp(Keys[index], Keys[index+1], interp);
    }

    public static BlobAssetReference<SimpleAnimationBlob> CreateBlob(AnimationCurve curve, Allocator allocator)
    {
        using (var blob = new BlobBuilder(Allocator.TempJob))
        {
            ref var anim = ref blob.ConstructRoot<SimpleAnimationBlob>();
            int keyCount = 12;

            float endTime = curve[curve.length - 1].time;
            anim.InvLength = 1.0F / endTime;
            anim.KeyCount = keyCount;

            var array = blob.Allocate(ref anim.Keys, keyCount + 1);
            for (int i = 0; i < keyCount; i++)
            {
                float t = (float) i / (float)(keyCount - 1) * endTime;
                array[i] = curve.Evaluate(t);
            }
            array[keyCount] = array[keyCount-1];

            return blob.CreateBlobAssetReference<SimpleAnimationBlob>(allocator);
        }
    }
}
