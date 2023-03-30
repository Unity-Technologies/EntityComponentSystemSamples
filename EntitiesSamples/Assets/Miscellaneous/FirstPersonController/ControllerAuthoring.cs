using Unity.Entities;
using UnityEngine;

namespace Miscellaneous.FirstPersonController
{
    public class ControllerAuthoring : MonoBehaviour
    {
        public float mouse_sensitivity = 50.0f;
        public float player_speed = 5.0f;
        public float jump_speed = 5.0f;

        class Baker : Baker<ControllerAuthoring>
        {
            public override void Bake(ControllerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Controller
                {
                    mouse_sensitivity = authoring.mouse_sensitivity,
                    player_speed = authoring.player_speed,
                    jump_speed = authoring.jump_speed,
                });
            }
        }
    }

    public struct Controller : IComponentData
    {
        public float mouse_sensitivity;
        public float player_speed;
        public float jump_speed;
        public float vertical_speed;
        public float camera_pitch;
    }
}

