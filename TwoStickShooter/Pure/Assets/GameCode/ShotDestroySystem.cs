using Unity.Collections;
using Unity.Entities;
using Unity.Transforms2D;
using UnityEngine;

namespace TwoStickPureExample
{
    [UpdateAfter(typeof(ShotSpawnSystem))]
    [UpdateAfter(typeof(MoveForward2DSystem))]
    public class ShotDestroySystem : ComponentSystem
    {
        public struct Data
        {
            public int Length;
            public EntityArray Entities;
            public ComponentDataArray<Shot> Shot;
        }

        [Inject] private Data m_Data;

        private struct PlayerCheck
        {
            public int Length;
            [ReadOnly] public ComponentDataArray<PlayerInput> PlayerInput;
        }

        [Inject] private PlayerCheck m_PlayerCheck;

        protected override void OnUpdate()
        {
            bool playerDead = m_PlayerCheck.Length == 0;
            float dt = Time.deltaTime;

            for (int i = 0; i < m_Data.Length; ++i)
            {
                Shot s = m_Data.Shot[i];
                s.TimeToLive -= dt;
                if (s.TimeToLive <= 0.0f || playerDead)
                {
                    PostUpdateCommands.DestroyEntity(m_Data.Entities[i]);
                }
                m_Data.Shot[i] = s;
            }
        }
    }
}
