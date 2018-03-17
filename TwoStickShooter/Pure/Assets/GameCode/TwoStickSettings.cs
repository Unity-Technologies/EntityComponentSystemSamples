using UnityEngine;

namespace TwoStickPureExample
{
    public class TwoStickSettings : MonoBehaviour
    {
        public float playerMoveSpeed = 15.0f;
        public float bulletMoveSpeed = 30.0f;
        public float bulletTimeToLive = 2.0f;
        public float playerFireCoolDown = 0.1f;
        public float enemySpeed = 8.0f;
        public float enemyShootRate = 1.0f;
        public float enemyShotSpeed = 20.0f;
        public float enemyShotTimeToLive = 2.0f;
        public float playerInitialHealth = 5.0f;
        public float enemyInitialHealth = 100.0f;
        public float playerShotEnergy = 3.0f;
        public float enemyShotEnergy = 3.0f;
        public float playerCollisionRadius = 1.0f;
        public float enemyCollisionRadius = 1.0f;
        public Rect playfield = new Rect {x = -30.0f, y = -30.0f, width = 60.0f, height = 60.0f};
    }
}