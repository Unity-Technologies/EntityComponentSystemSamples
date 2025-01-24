using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace HelloCube.FirstPersonController
{
    public class ControllerAuthoring : MonoBehaviour
    {
        public float MouseSensitivity = 50.0f;
        public float PlayerSpeed = 5.0f;
        public float JumpSpeed = 5.0f;

        class Baker : Baker<ControllerAuthoring>
        {
            public override void Bake(ControllerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Controller
                {
                    MouseSensitivity = authoring.MouseSensitivity,
                    PlayerSpeed = authoring.PlayerSpeed,
                    JumpSpeed = authoring.JumpSpeed,
                });
                AddComponent<InputState>(entity);
            }
        }
    }

    public struct InputState : IComponentData
    {
        public float Horizontal;
        public float Vertical;
        public float MouseX;
        public float MouseY;
        public bool Space;
    }

    public struct Controller : IComponentData
    {
        public float MouseSensitivity;
        public float PlayerSpeed;
        public float JumpSpeed;
        public float VerticalSpeed;
        public float CameraPitch;
    }
}

