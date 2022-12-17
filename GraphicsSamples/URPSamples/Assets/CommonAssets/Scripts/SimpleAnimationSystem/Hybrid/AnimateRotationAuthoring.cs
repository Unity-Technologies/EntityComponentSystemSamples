using UnityEngine;

internal class AnimateRotationAuthoring : MonoBehaviour
{
    [Tooltip("Local start rotation in degrees")]
    public Vector3 FromRotation = new Vector3(0f, 0f, -10f);

    [Tooltip("Local target rotation in degrees")]
    public Vector3 ToRotation = new Vector3(0f, 0f, 10f);

    [Tooltip("Time in seconds to complete a single loop of the animation")]
    public float Phase = 5f;

    [Range(0f, 1f)]
    [Tooltip("Phase shift as a percentage (0 to 1.0)")]
    public float Offset;
}
