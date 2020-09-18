using UnityEngine;

public class BasicJointInfo : MonoBehaviour
{
    public BasicJointType Type;
    public GameObject ConnectedGameObject;

    public enum BasicJointType
    {
        BallAndSocket,
        Hinge,
        Distance,
    }
}
