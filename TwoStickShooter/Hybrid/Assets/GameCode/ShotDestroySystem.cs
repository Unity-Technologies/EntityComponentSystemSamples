using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms2D;
using UnityEngine;

namespace TwoStickHybridExample
{
    [UpdateAfter(typeof(ShotSpawnSystem))]
    [UpdateAfter(typeof(MoveForward2DSystem))]
    public class ShotDestroySystem : ComponentSystem
    {
        struct Data
        {
            public Shot Shot;
        }

        struct PlayerCheck
        {
            public int Length;
            [ReadOnly] public ComponentArray<PlayerInput> PlayerInput;
        }

        [Inject] private PlayerCheck m_PlayerCheck;

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
