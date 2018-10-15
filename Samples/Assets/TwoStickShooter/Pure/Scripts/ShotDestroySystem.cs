using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace TwoStickPureExample
{
    [UpdateAfter(typeof(ShotSpawnSystem))]
    public class ShotDestroySystem : ComponentSystem
    {
#pragma warning disable 649
        public struct Data
        {
            public readonly int Length;
            public EntityArray Entities;
            public ComponentDataArray<Shot> Shot;
        }

        [Inject] private Data m_Data;

        private struct PlayerCheck
        {
            public readonly int Length;
            [ReadOnly] public ComponentDataArray<PlayerInput> PlayerInput;
        }

        [Inject] private PlayerCheck m_PlayerCheck;
#pragma warning restore 649
        
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
