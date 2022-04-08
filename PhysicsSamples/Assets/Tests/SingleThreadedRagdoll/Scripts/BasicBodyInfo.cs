using UnityEngine;

public class BasicBodyInfo : MonoBehaviour
{
    public BodyType Type;
    public bool IsStatic = false;
    public float Mass = 5.0f;

    public enum BodyType
    {
        Sphere,
        Box,
        ConvexHull,
        Capsule
    }
}
