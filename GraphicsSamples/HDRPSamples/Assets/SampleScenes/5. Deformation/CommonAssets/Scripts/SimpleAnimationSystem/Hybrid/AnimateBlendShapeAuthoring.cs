using UnityEngine;

public class AnimateBlendShapeAuthoring : MonoBehaviour
{
    [Tooltip("Start BlendShape weight")]
    public float FromWeight = 0f;

    [Tooltip("Target BlendShape weight")]
    public float ToWeight = 100f;

    [Tooltip("Time in seconds to complete a single loop of the animation")]
    public float Phase = 1f;

    [Range(0f, 1f)]
    [Tooltip("Phase shift as a percentage (0 to 1.0)")]
    public float Offset; 
}
