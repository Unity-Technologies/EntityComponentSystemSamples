using UnityEngine;

namespace TwoStickHybridExample
{
    // TODO: Call out that this is better than storing state in the system, because it can support things like replay.
    public class EnemySpawnSystemState : MonoBehaviour
    {
        public int SpawnedEnemyCount;
        public float Cooldown;
        public Random.State RandomState;
    }
}
