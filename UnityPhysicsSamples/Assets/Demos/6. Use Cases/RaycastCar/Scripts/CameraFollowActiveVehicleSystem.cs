using System;
using Unity.Entities;
using UnityEngine;

[UpdateAfter(typeof(ChangeActiveVehicleSystem))]
class CameraFollowActiveVehicleSystem : ComponentSystem
{
    CameraSmoothTrack m_CameraTracker;

    protected override void OnUpdate()
    {
        // TODO: implement camera tracker using entities
        if (m_CameraTracker == null)
            m_CameraTracker = GameObject.FindObjectOfType<CameraSmoothTrack>();

        if (m_CameraTracker == null)
            return;

        Entities.WithAllReadOnly<ActiveVehicle, VehicleReferences>().ForEach((VehicleReferences vehicle) =>
        {
            m_CameraTracker.Target = vehicle.CameraTarget.gameObject;
            m_CameraTracker.LookTo = vehicle.CameraTo.gameObject;
            m_CameraTracker.LookFrom = vehicle.CameraFrom.gameObject;
        });
    }
}