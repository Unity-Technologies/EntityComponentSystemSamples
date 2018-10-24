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

            var receivers = Health.All;
            if (receivers.Count == 0)
            {
                Destroy(gameObject);
                return;
            }

            var faction = GetComponent<Faction>().Value;
            
            foreach (var health in receivers)
            {
                var receiverFaction = health.GetComponent<Faction>().Value;
                var collisionRadius = GetCollisionRadius(settings, receiverFaction);
                var collisionRadiusSquared = collisionRadius * collisionRadius;

                var xform = health.GetComponent<Transform2D>();
                var receiverPos = xform.Position;

                if (faction != receiverFaction)
                {
                    var shotPos = transform2D.Position;
                    var delta = shotPos - receiverPos;
                    var distSquared = math.dot(delta, delta);
                    if (distSquared <= collisionRadiusSquared)
                    {

                        health.Value = health.Value - Energy;

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
