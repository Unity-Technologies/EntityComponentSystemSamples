using System.Collections.Generic;
using UnityEngine;

public class SwapVehicles : MonoBehaviour
{
    public List<GameObject> vehicles;
    private int vehicleIdx = -1;



    // Start is called before the first frame update
    void Start()
    {
        ChangeVehicle(0);
    }


    private void ChangeVehicle(int newVehicleIdx)
    {
        // Disable old vehicle
        {
            if (0 <= vehicleIdx && vehicleIdx < vehicles.Count )
            {
                GameObject vehicleNode = vehicles[vehicleIdx];
                var vehicleControls = vehicleNode.GetComponentInChildren<VehicleControls>();
                if (vehicleControls)
                    vehicleControls.enabled = false;
            }
        }

        vehicleIdx = newVehicleIdx;

        // enable new vehicle
        {
            if (0 <= vehicleIdx && vehicleIdx < vehicles.Count)
            {
                GameObject vehicleNode = vehicles[vehicleIdx];
                var vehicleControls = vehicleNode.GetComponentInChildren<VehicleControls>();
                if (vehicleControls)
                    vehicleControls.enabled = true;

                var cameraTracker = GetComponent<CameraSmoothTrack>();
                if (cameraTracker)
                {
                    cameraTracker.Target = vehicleNode;
                    cameraTracker.LookTo = vehicleNode.transform.Find("CameraTo").gameObject;
                    cameraTracker.LookFrom = vehicleNode.transform.Find("CameraTo/CameraFrom").gameObject;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        int newVehicleIdx = vehicleIdx;

        for (KeyCode kc = KeyCode.Alpha1; kc <= KeyCode.Alpha9; kc++)
            if (Input.GetKeyDown(kc)) newVehicleIdx = (kc - KeyCode.Alpha1);

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetButtonDown("JoypadRB"))
            if (++newVehicleIdx >= vehicles.Count) newVehicleIdx -= vehicles.Count;

        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetButtonDown("JoypadLB"))
            if (--newVehicleIdx < 0) newVehicleIdx += vehicles.Count;

        newVehicleIdx = Mathf.Clamp(newVehicleIdx, 0, vehicles.Count - 1);

        if (newVehicleIdx != vehicleIdx) 
            ChangeVehicle(newVehicleIdx);
    }
}
