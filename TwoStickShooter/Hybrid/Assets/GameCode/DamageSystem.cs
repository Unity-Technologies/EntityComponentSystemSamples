using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms2D;
using UnityEngine;
using UnityEngine.UI;

namespace TwoStickHybridExample
{

    public class PlayerDamageSystem : ComponentSystem
    {
        public struct ReceiverData
        {
            public int Length;
            public ComponentArray<Health> Health;
            public ComponentArray<Faction> Faction;
            public ComponentArray<Position2D> Position;
        }

        [Inject] ReceiverData m_Receivers;

        public struct ShotData
        {
            public int Length;
            public ComponentArray<Shot> Shot;
            public ComponentArray<Position2D> Position;
            public ComponentArray<Faction> Faction;
        }
        [Inject] ShotData m_Shots;

        protected override void OnUpdate()
        {
            if (0 == m_Receivers.Length || 0 == m_Shots.Length)
                return;

            var settings = TwoStickBootstrap.Settings;

            for (int pi = 0; pi < m_Receivers.Length; ++pi)
            {
                float damage = 0.0f;
                float collisionRadius = GetCollisionRadius(settings, m_Receivers.Faction[pi].Value);
                float collisionRadiusSquared = collisionRadius * collisionRadius;

                float2 receiverPos = m_Receivers.Position[pi].Value;
                Faction.Type receiverFaction = m_Receivers.Faction[pi].Value;

                for (int si = 0; si < m_Shots.Length; ++si)
                {
                    if (m_Shots.Faction[si].Value != receiverFaction)
                    {
                        float2 shotPos = m_Shots.Position[si].Value;
                        float2 delta = shotPos - receiverPos;
                        float distSquared = math.dot(delta, delta);
                        if (distSquared <= collisionRadiusSquared)
                        {
                            var shot = m_Shots.Shot[si];

                            damage += shot.Energy;

                            // Set the shot's time to live to zero, so it will be collected by the shot destroy system
                            shot.TimeToLive = 0.0f;
                        }
                    }
                }

                var h = m_Receivers.Health[pi];
                h.Value = math.max(h.Value - damage, 0.0f);
            }
        }

        static float GetCollisionRadius(TwoStickExampleSettings settings, Faction.Type faction)
        {
            // This simply picks the collision radius based on whether the receiver is the player or not.
            // In a real game, this would be much more sophisticated, perhaps with a CollisionRadius component.
            return faction == Faction.Type.Player ? settings.playerCollisionRadius : settings.enemyCollisionRadius;
        }
    }
}
