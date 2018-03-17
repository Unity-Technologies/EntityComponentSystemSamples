using Unity.Mathematics;
using UnityEngine;

namespace TwoStickClassicExample
{
    // Spawns new enemies.
    public class EnemySpawnSystem : MonoBehaviour
    {

        public int SpawnedEnemyCount;
        public float Cooldown;
        public Random.State RandomState;

        void Start()
        {
            var oldState = Random.state;
            Random.InitState(0xaf77);
            
            Cooldown = 0.0f;
            SpawnedEnemyCount = 0;
            RandomState = Random.state;
            Random.state = oldState;
        }

        protected void Update()
        {

            var oldState = Random.state;
            Random.state = RandomState;

            Cooldown -= Time.deltaTime;

            if (Cooldown <= 0.0f)
            {
                var settings = TwoStickBootstrap.Settings;
                var enemy = Object.Instantiate(settings.EnemyPrefab);
                //@TODO set transform
                ComputeSpawnLocation(enemy);
                SpawnedEnemyCount++;
                Cooldown = ComputeCooldown(SpawnedEnemyCount);
            }

            RandomState = Random.state;
            
            Random.state = oldState;
        }

        private float ComputeCooldown(int stateSpawnedEnemyCount)
        {
            return 0.15f;
        }

        private void ComputeSpawnLocation(Transform2D xform)
        {
            var settings = TwoStickBootstrap.Settings;
            
            float r = Random.value;
            float x0 = settings.playfield.xMin;
            float x1 = settings.playfield.xMax;
            float x = x0 + (x1 - x0) * r;

            xform.Position = new float2(x, settings.playfield.yMax);
            xform.Heading = new float2(0, -1);
        }
    }

}
