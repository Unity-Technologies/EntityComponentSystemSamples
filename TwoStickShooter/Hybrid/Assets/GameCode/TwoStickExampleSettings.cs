using UnityEngine;

namespace TwoStickHybridExample
{
    public class TwoStickExampleSettings : MonoBehaviour
    {
        public float playerMoveSpeed = 15.0f;
        public float playerFireCoolDown = 0.1f;
        public float enemySpeed = 8.0f;
        public float enemyShootRate = 1.0f;
        public float playerCollisionRadius = 1.0f;
        public float enemyCollisionRadius = 1.0f;
        public Rect playfield = new Rect { x = -30.0f, y = -30.0f, width = 60.0f, height = 60.0f }; 

        public Shot PlayerShotPrefab;
        public Shot EnemyShotPrefab;
        public GameObject PlayerPrefab;
        public GameObject EnemyPrefab;
        public EnemySpawnSystemState EnemySpawnState;
        public Faction EnemyFaction;
    }
}