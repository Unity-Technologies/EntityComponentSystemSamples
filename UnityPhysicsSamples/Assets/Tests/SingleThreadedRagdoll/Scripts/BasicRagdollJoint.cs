using UnityEngine;

public class BasicRagdollJoint : MonoBehaviour
{
    public RagdollDemoJointType Type;
    public GameObject ConnectedGameObject;

    public enum RagdollDemoJointType
    {
        Neck,
        Shoulder,
        Elbow,
        Wrist,
        Waist,
        Hip,
        Knee,
        Ankle
    }
}
