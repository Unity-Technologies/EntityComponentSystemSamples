using Tutorials.Tanks.Step3;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Tutorials.Tanks.Step8
{
    // This system should run after the transform system has been updated, otherwise the camera
    // will lag one frame behind the tank.
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct CameraSystem : ISystem
    {
        Entity target;
        Random random;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Execute.Camera>();
            random = new Random(123);
        }

        // Because this OnUpdate accesses managed objects, it cannot be Burst-compiled.
        public void OnUpdate(ref SystemState state)
        {
            if (target == Entity.Null || Input.GetKeyDown(KeyCode.Space))
            {
                var tankQuery = SystemAPI.QueryBuilder().WithAll<Tank>().Build();
                var tanks = tankQuery.ToEntityArray(Allocator.Temp);
                if (tanks.Length == 0)
                {
                    return;
                }

                target = tanks[random.NextInt(tanks.Length)];
            }

            var cameraTransform = CameraSingleton.Instance.transform;
            var tankTransform = SystemAPI.GetComponent<LocalToWorld>(target);
            cameraTransform.position = tankTransform.Position;
            cameraTransform.position -= 10.0f * (Vector3)tankTransform.Forward;  // move the camera back from the tank
            cameraTransform.position += new Vector3(0, 5f, 0);  // raise the camera by an offset
            cameraTransform.LookAt(tankTransform.Position);
        }
    }
}
