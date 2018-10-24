﻿using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace TwoStickHybridExample
{
    public class RemoveDeadSystem : ComponentSystem
    {
#pragma warning disable 649
        public struct Entities
        {
            public readonly int Length;
            public GameObjectArray gameObjects;
            public ComponentArray<Health> healths;
        }

        struct PlayerCheck
        {
            public readonly int Length;
            [ReadOnly] public ComponentArray<PlayerInput> PlayerInput;
        }

        [Inject] private PlayerCheck m_PlayerCheck;
        [Inject] private Entities entities;
#pragma warning restore 649

        protected override void OnUpdate()
        {
            var playerDead = m_PlayerCheck.Length == 0;
            var toDestroy = new List<GameObject>();
            for (var i = 0; i < entities.Length; ++i)
            {

                if (entities.healths[i].Value <= 0 || playerDead)
                {
                    toDestroy.Add(entities.gameObjects[i]);
                }
            }

            foreach (var go in toDestroy)
            {
                Object.Destroy(go);
            }
        }
    }
}
