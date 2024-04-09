using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace HelloCube.FirstPersonController
{
    [UpdateAfter(typeof(ControllerSystem))]
    public partial struct CameraSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Controller>();
            state.RequireForUpdate<ExecuteFirstPersonController>();
        }

        // This OnUpdate accesses managed objects and so cannot be Burst-compiled
        public void OnUpdate(ref SystemState state)
        {
            if (Camera.main != null)
            {
                var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
                var controllerEntity = SystemAPI.GetSingletonEntity<Controller>();
                var controller = SystemAPI.GetSingleton<Controller>();

                var controllerTransform = transformLookup[controllerEntity];
                var cameraTransform = Camera.main.transform;
                cameraTransform.position = controllerTransform.Position;
                cameraTransform.rotation = math.mul(controllerTransform.Rotation,
                    quaternion.RotateX(controller.CameraPitch));
            }
        }
    }
}
