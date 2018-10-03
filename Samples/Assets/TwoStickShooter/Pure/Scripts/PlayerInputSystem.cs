using Unity.Entities;
using UnityEngine;

namespace TwoStickPureExample
{
    public class PlayerInputSystem : ComponentSystem
    {
        struct PlayerData
        {
#pragma warning disable 649
            public readonly int Length;
#pragma warning restore 649

            public ComponentDataArray<PlayerInput> Input;
        }

        [Inject] private PlayerData m_Players;

        protected override void OnUpdate()
        {
            float dt = Time.deltaTime;

            for (int i = 0; i < m_Players.Length; ++i)
            {
                UpdatePlayerInput(i, dt);
            }
        }

        private void UpdatePlayerInput(int i, float dt)
        {
            PlayerInput pi;

            pi.Move.x = Input.GetAxis("Horizontal");
            pi.Move.y = 0.0f;
            pi.Move.z = Input.GetAxis("Vertical");
            pi.Shoot.x = Input.GetAxis("ShootX");
            pi.Shoot.y = 0.0f;
            pi.Shoot.z = Input.GetAxis("ShootY");
            pi.FireCooldown = Mathf.Max(0.0f, m_Players.Input[i].FireCooldown - dt);

            m_Players.Input[i] = pi;
        }
    }
}
