#if NETCODE_GAMEOBJECT_PHYSICS2D_EXPERIMENTAL
using Unity.Entities;
using Unity.Netcode;
using Unity.Netcode.NetcodeTime;
using UnityEngine;
#if GAMEOBJECTSAMPLE_MODULE_PHYSICSCORE2D
using Unity.U2D.Physics;
#endif

namespace GameObjectSample.Physics2DSample
{
    // One per scene. Marks the sample active and applies the same netcode settings as the 3D physics sample. Does
    // nothing unless the scene's StartChecks pass. Runs before the spawners, so they see the sample active.
    [DefaultExecutionOrder(-1000)]
    public class Physics2DSampleSetup : MonoBehaviour
    {
        void Awake()
        {
            if (!StartChecks.Passed)
                return;
            Physics2DSample.IsActive = true;

            ConfigureServer();
            ConfigureClients();
        }

#if GAMEOBJECTSAMPLE_MODULE_PHYSICSCORE2D
        void OnEnable()
        {
            GhostPhysicsCore2DWorlds.WorldCreated += ConfigurePhysicsWorld;
            foreach (var world in GhostPhysicsCore2DWorlds.Worlds)
                ConfigurePhysicsWorld(world);
        }

        void OnDisable() => GhostPhysicsCore2DWorlds.WorldCreated -= ConfigurePhysicsWorld;

        // Fixed for prediction consistency; client and server must match. Sub-steps resolve marginal contacts more
        // consistently, at CPU cost. Netcode already turns off warm starting and contact recycling on these worlds.
        const int k_SimulationSubSteps = 8;

        static void ConfigurePhysicsWorld(PhysicsWorld world)
        {
            if (!world.isValid)
                return;
            world.simulationSubSteps = k_SimulationSubSteps;
        }
#endif

        // Correction quality under lag: re-simulate rolled back ticks one by one. Batched prediction steps move physics
        // in bigger increments than the server did, which adds misprediction on top of what the network caused.
        static void ConfigureClients()
        {
            foreach (var world in ClientServerBootstrap.AllClientWorldsEnumerator())
            {
                if (!world.IsCreated || world.IsThinClient() || world.IsHost())
                    continue;

                using var tickRateQuery = world.EntityManager.CreateEntityQuery(typeof(ClientTickRate));
                var tickRate = tickRateQuery.IsEmptyIgnoreFilter ? NetworkTimeSystem.DefaultClientTickRate : tickRateQuery.GetSingleton<ClientTickRate>();
                tickRate.MaxPredictionStepBatchSizeRepeatedTick = 1;
                tickRate.MaxPredictionStepBatchSizeFirstTimeTick = 1;
                // By default only the ghosts in a snapshot roll back, so a replay simulates them against bodies
                // left on a newer timeline. Physics wants every interacting body rolled back together.
                tickRate.AlwaysRollbackAllPredictedGhosts = true;
                if (tickRateQuery.IsEmptyIgnoreFilter)
                    world.EntityManager.CreateSingleton(tickRate);
                else
                    tickRateQuery.SetSingleton(tickRate);
            }
        }

        void OnDestroy() => Physics2DSample.IsActive = false;

        // Tick batching runs several ticks in one step, which makes the physics mismatch worse.
        static void ConfigureServer()
        {
            foreach (var world in World.All)
            {
                if (!world.IsServer())
                    continue;

                using var tickRateQuery = world.EntityManager.CreateEntityQuery(typeof(ClientServerTickRate));
                if (!tickRateQuery.IsEmptyIgnoreFilter)
                {
                    var tickRate = tickRateQuery.GetSingleton<ClientServerTickRate>();
                    tickRate.MaxSimulationStepsPerFrame = 3;
                    tickRate.MaxSimulationStepBatchSize = 1;
                    tickRateQuery.SetSingleton(tickRate);
                }
            }
        }
    }
}
#endif
