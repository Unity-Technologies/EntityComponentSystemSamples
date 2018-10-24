using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace TwoStickHybridExample
{
    [UpdateAfter(typeof(ShotSpawnSystem))]
    public class ShotDestroySystem : ComponentSystem
    {
#pragma warning disable 649
        struct Data
        {
            public Shot Shot;
        }

        struct PlayerCheck
        {
            public readonly int Length;
            [ReadOnly] public ComponentArray<PlayerInput> PlayerInput;
        }

        [Inject] private PlayerCheck m_PlayerCheck;
#pragma warning restore 649

        protected override void OnUpdate()
        {
            var playerDead = m_PlayerCheck.Length == 0;
            float dt = Time.deltaTime;

            var toDestroy = new List<GameObject>();
            foreach (var entity in GetEntities<Data>())
            {
                var s = entity.Shot;
                s.TimeToLive -= dt;
                if (s.TimeToLive <= 0.0f || playerDead)
                {
                    toDestroy.Add(s.gameObject);
                }
            }

            foreach (var go in toDestroy)
            {
                Object.Destroy(go);
            }
        }
    }
}
