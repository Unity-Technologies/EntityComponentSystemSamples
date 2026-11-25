#if NETCODE_GAMEOBJECT_PHYSICS2D_EXPERIMENTAL
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace GameObjectSample.Physics2DSample
{
    // Server side: spawns a spinning box ghost for each drag shot a player releases. A projectile is a plain simulated
    // body, set going through ISampleBody2D.
    public class ProjectileLauncher2D : MonoBehaviour
    {
        // Meters per second. Also caps the client's aim arrow; the server clamps again since the velocity is input.
        public const float MaxShotSpeed = 60f;
        const float k_ShotSpinDegrees = 720f;

        struct Projectile
        {
            public GameObject GameObject;
            public float DespawnAt;
        }

        [SerializeField] GameObject m_ProjectilePrefab;
        [SerializeField] float m_Lifetime = 8f;
        [SerializeField] int m_MaxProjectiles = 40;

        readonly List<Projectile> m_Projectiles = new List<Projectile>();

        public static ProjectileLauncher2D Instance { get; private set; }

        void OnEnable() => Instance = this;

        void OnDisable()
        {
            if (Instance == this)
                Instance = null;
        }

        // Called on the server from the prediction loop, so the projectile spawns on the tick the shot was fired.
        public void Launch(Vector2 origin, Vector2 velocity)
        {
            if (!Netcode.IsServerRole || !StartChecks.Passed)
                return;

            while (m_Projectiles.Count >= m_MaxProjectiles)
            {
                Destroy(m_Projectiles[0].GameObject);
                m_Projectiles.RemoveAt(0);
            }

            var go = Instantiate(m_ProjectilePrefab, origin, Quaternion.identity);
            var body = go.GetComponent<ISampleBody2D>();
            body.LinearVelocity = Vector2.ClampMagnitude(velocity, MaxShotSpeed);
            body.AngularVelocity = k_ShotSpinDegrees;
            m_Projectiles.Add(new Projectile { GameObject = go, DespawnAt = Time.time + m_Lifetime });
        }

        void Update()
        {
            for (var i = m_Projectiles.Count - 1; i >= 0; --i)
            {
                var projectile = m_Projectiles[i];
                if (projectile.GameObject != null && Time.time < projectile.DespawnAt)
                    continue;
                if (projectile.GameObject != null)
                    Destroy(projectile.GameObject);
                m_Projectiles.RemoveAt(i);
            }
        }

        void OnDestroy()
        {
            foreach (var projectile in m_Projectiles)
            {
                if (projectile.GameObject != null)
                    Destroy(projectile.GameObject);
            }
        }
    }
}
#endif
