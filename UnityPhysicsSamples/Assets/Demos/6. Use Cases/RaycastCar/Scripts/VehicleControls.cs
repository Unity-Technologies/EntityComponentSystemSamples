using Demos;
using UnityEngine;

public class VehicleControls : MonoBehaviour
{
    public GameObject vehicleMechanicsNode;
    public float topSpeed = 10.0f;
    public float maxSteeringAngle = 30.0f;

    public GameObject cameraNode;
    public bool lockCameraOrientation = false;

    private float desiredSteeringAngle;
    private float desiredSpeed;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!vehicleMechanicsNode) return;

        float x = Input.GetAxis("Horizontal"); //
        float a = Input.GetAxis("Accelerator"); // 3rdAxis Triggers
        float z = -Input.GetAxis("RotateX"); // 4thAxis RightStickX

        desiredSpeed = a * topSpeed;
        desiredSteeringAngle = x * maxSteeringAngle;

        if (cameraNode)
        {
            if (lockCameraOrientation)
            {
                cameraNode.transform.rotation *= Quaternion.Euler(0, z, 0);
            }
            else
            {
                cameraNode.transform.rotation = Quaternion.Euler(0, z * 180, 0);
            }
        }

        var mechanics = vehicleMechanicsNode.GetComponent<VehicleMechanics>();
        if (mechanics)
        {
            mechanics.steeringAngle = Mathf.Lerp(mechanics.steeringAngle, desiredSteeringAngle, 0.1f);
            mechanics.driveDesiredSpeed = Mathf.Lerp(mechanics.driveDesiredSpeed, desiredSpeed, 0.01f);
            mechanics.driveEngaged = (0.0f != desiredSpeed);
        }
    }
}
