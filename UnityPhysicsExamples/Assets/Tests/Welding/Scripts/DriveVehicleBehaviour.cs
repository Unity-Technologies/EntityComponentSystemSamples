using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DriveVehicleBehaviour : MonoBehaviour
{
    public GameObject vehicleMechanicsNode;
    public float DesiredSpeed = 0;
    public float DesiredSteeringAngle = 0;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!vehicleMechanicsNode) return;

        var mechanics = vehicleMechanicsNode.GetComponent<Demos.VehicleMechanics>();
        if (mechanics)
        {
            mechanics.steeringAngle = Mathf.Lerp(mechanics.steeringAngle, DesiredSteeringAngle, 0.1f);
            mechanics.driveDesiredSpeed = Mathf.Lerp(mechanics.driveDesiredSpeed, DesiredSpeed, 0.01f);
            mechanics.driveEngaged = (0.0f != DesiredSpeed);
        }
    }
}
