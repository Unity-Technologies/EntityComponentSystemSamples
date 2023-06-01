using UnityEngine;

public class AnimateScaleAuthoring : MonoBehaviour
{
    [Tooltip("Local start scale")]
    public Vector3 FromScale = Vector3.one;

    [Tooltip("Local target scale")]
    public Vector3 ToScale = 2f * Vector3.one;

    [Tooltip("Time in seconds to complete a single loop of the animation")]
    public float Phase = 1f;

    [Range(0f, 1f)]
    [Tooltip("Phase shift as a percentage (0 to 1.0)")]
    public float Offset;
}
