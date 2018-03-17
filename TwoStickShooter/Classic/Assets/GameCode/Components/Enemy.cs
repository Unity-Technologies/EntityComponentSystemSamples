using Unity.Mathematics;
using UnityEngine;

namespace TwoStickClassicExample
{

    public class Enemy : MonoBehaviour
    {
        private void Update()
        {

            var player = FindObjectOfType<Player>();
            if (!player)
            {
                Destroy(gameObject);
                return;
            }
            // Movement
            var settings = TwoStickBootstrap.Settings;
            var minY = settings.playfield.yMin;
            var maxY = settings.playfield.yMax;

            var xform = GetComponent<Transform2D>();

            if (xform.Position.y > maxY || xform.Position.y < minY)
            {
                GetComponent<Health>().Value = -1;
            }
            
            // Shooting
            var playerPos = player.GetComponent<Transform2D>().Position;

            var state = GetComponent<EnemyShootState>();

            state.Cooldown -= Time.deltaTime;
            
            if (state.Cooldown <= 0.0)
            {
                state.Cooldown = TwoStickBootstrap.Settings.enemyShootRate;
                var position = GetComponent<Transform2D>().Position;

                ShotSpawnData spawn = new ShotSpawnData()
                {
                    Position = position,
                    Heading = math.normalize(playerPos - position),
                    Faction = TwoStickBootstrap.Settings.EnemyFaction
                };
                ShotSpawnSystem.SpawnShot(spawn);
            }
        }
    }
}
