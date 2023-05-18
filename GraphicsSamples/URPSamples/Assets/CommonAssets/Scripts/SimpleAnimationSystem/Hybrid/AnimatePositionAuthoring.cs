using UnityEngine;

public class AnimatePositionAuthoring : MonoBehaviour
{
    [Tooltip("Local start position")]
    public Vector3 FromPosition = Vector3.zero;

    [Tooltip("Local target position")]
    public Vector3 ToPosition = Vector3.up;

    [Tooltip("Time in seconds to complete a single loop of the animation")]
    public float Phase = 1f;

    [Range(0f, 1f)]
    [Tooltip("Phase shift as a percentage (0 to 1.0)")]
    public float Offset;
}
