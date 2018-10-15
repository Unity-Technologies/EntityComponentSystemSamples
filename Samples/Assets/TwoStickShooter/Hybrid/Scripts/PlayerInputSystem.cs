using Unity.Entities;
using UnityEngine;

namespace TwoStickHybridExample
{
    public class PlayerInputSystem : ComponentSystem
    {
        struct PlayerData
        {

#pragma warning disable 649
            public PlayerInput Input;
#pragma warning restore 649
        }

        protected override void OnUpdate()
        {
            float dt = Time.deltaTime;

            foreach (var entity in GetEntities<PlayerData>())
            {
                var pi = entity.Input;

                pi.Move.x = Input.GetAxis("Horizontal");
                pi.Move.y = Input.GetAxis("Vertical");
                pi.Shoot.x = Input.GetAxis("ShootX");
                pi.Shoot.y = Input.GetAxis("ShootY");

                pi.FireCooldown = Mathf.Max(0.0f, pi.FireCooldown - dt);
            }
        }
    }
}
