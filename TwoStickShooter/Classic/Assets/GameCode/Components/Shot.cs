using Unity.Mathematics;
using UnityEngine;

namespace TwoStickClassicExample
{

    public class Shot : MonoBehaviour
    {
        public float TimeToLive;
        public float Energy;

        private void Update()
        {
            // Move

            var transform2D = GetComponent<Transform2D>();
            
            // Collision
            var settings = TwoStickBootstrap.Settings;

            var receivers = FindObjectsOfType(typeof(Health));
            if (receivers.Length == 0)
            {
                Destroy(gameObject);
                return;
            }

            var faction = GetComponent<Faction>().Value;
            
            foreach (Health health in receivers)
            {
                var receiverFaction = health.GetComponent<Faction>().Value;
                float collisionRadius = GetCollisionRadius(settings, receiverFaction);
                float collisionRadiusSquared = collisionRadius * collisionRadius;

                var xform = health.GetComponent<Transform2D>();
                float2 receiverPos = xform.Position;

                if (faction != receiverFaction)
                {
                    float2 shotPos = transform2D.Position;
                    float2 delta = shotPos - receiverPos;
                    float distSquared = math.dot(delta, delta);
                    if (distSquared <= collisionRadiusSquared)
                    {

                        health.Value = health.Value - Energy;

                        // Set the shot's time to live to zero, so it will be collected by the shot destroy system 
                        Destroy(gameObject);
                        break;
                    }
                }
            }
            
            // Destroy
            TimeToLive -= Time.deltaTime;
            if (TimeToLive <= 0.0f)
            {
                Destroy(gameObject);
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
