using System.ComponentModel.Design;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms2D;

namespace TwoStickPureExample
{

    /// <summary>
    /// Assigns out damage from shots colliding with entities of other factions.
    /// </summary>
    class ShotDamageSystem : JobComponentSystem
    {
        struct Players
        {
            public int Length;
            public ComponentDataArray<Health> Health;
            [ReadOnly] public ComponentDataArray<Position2D> Position;
            [ReadOnly] public ComponentDataArray<PlayerInput> PlayerMarker;
        }

        [Inject] Players m_Players;

        struct Enemies
        {
            public int Length;
            public ComponentDataArray<Health> Health;
            [ReadOnly] public ComponentDataArray<Position2D> Position;
            [ReadOnly] public ComponentDataArray<Enemy> EnemyMarker;
        }

        [Inject] Enemies m_Enemies;

        /// <summary>
        /// All player shots.
        /// </summary>
        struct PlayerShotData
        {
            public int Length;
            public ComponentDataArray<Shot> Shot;
            [ReadOnly] public ComponentDataArray<Position2D> Position;
            [ReadOnly] public ComponentDataArray<PlayerShot> PlayerShotMarker;
        }
        [Inject] PlayerShotData m_PlayerShots;

        /// <summary>
        /// All enemy shots.
        /// </summary>
        struct EnemyShotData
        {
            public int Length;
            public ComponentDataArray<Shot> Shot;
            [ReadOnly] public ComponentDataArray<Position2D> Position;
            [ReadOnly] public ComponentDataArray<EnemyShot> EnemyShotMarker;
        }
        [Inject] EnemyShotData m_EnemyShots;

        [ComputeJobOptimization]
        struct CollisionJob : IJobParallelFor
        {
            public float CollisionRadiusSquared;

            public ComponentDataArray<Health> Health;
            [ReadOnly] public ComponentDataArray<Position2D> Positions;

            [NativeDisableParallelForRestriction]
            public ComponentDataArray<Shot> Shots;

            [NativeDisableParallelForRestriction]
            [ReadOnly] public ComponentDataArray<Position2D> ShotPositions;

            public void Execute(int index)
            {
                float damage = 0.0f;

                float2 receiverPos = Positions[index].Value;

                for (int si = 0; si < Shots.Length; ++si)
                {
                    float2 shotPos = ShotPositions[si].Value;
                    float2 delta = shotPos - receiverPos;
                    float distSquared = math.dot(delta, delta);
                    if (distSquared <= CollisionRadiusSquared)
                    {
                        var shot = Shots[si];

                        damage += shot.Energy;

                        // Set the shot's time to live to zero, so it will be collected by the shot destroy system
                        shot.TimeToLive = 0.0f;

                        Shots[si] = shot;
                    }
                }

                var h = Health[index];
                h.Value = math.max(h.Value - damage, 0.0f);
                Health[index] = h;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var settings = TwoStickBootstrap.Settings;

            if (settings == null)
                return inputDeps;

            var enemiesVsPlayers = new CollisionJob
            {
                ShotPositions = m_EnemyShots.Position,
                Shots = m_EnemyShots.Shot,
                CollisionRadiusSquared = settings.playerCollisionRadius * settings.playerCollisionRadius,
                Health = m_Players.Health,
                Positions = m_Players.Position,
            }.Schedule(m_Players.Length, 1, inputDeps);

            var playersVsEnemies = new CollisionJob
            {
                ShotPositions = m_PlayerShots.Position,
                Shots = m_PlayerShots.Shot,
                CollisionRadiusSquared = settings.enemyCollisionRadius * settings.enemyCollisionRadius,
                Health = m_Enemies.Health,
                Positions = m_Enemies.Position,
            }.Schedule(m_Enemies.Length, 1, enemiesVsPlayers);

            return playersVsEnemies;
        }
    }
}
