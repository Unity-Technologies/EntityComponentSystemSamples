using System;
using Data;
using Other;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Systems
{
    /// <summary>
    /// Iterates over the planets to see if any of them want to spawn ships
    /// If so it spawns them and the ShipMovementSystem guides them on their way
    /// </summary>
    [UpdateAfter(typeof(UserInputSystem))]
    public class ShipSpawnSystem : ComponentSystem
    {
        public ShipSpawnSystem()
        {
            _entityManager = World.Active.GetOrCreateManager<EntityManager>();
        }
        struct SpawningPlanets
        {
            public readonly int Length;
            public ComponentDataArray<PlanetShipLaunchData> Data;
        }

        struct ShipSpawnData
        {
            public PlanetShipLaunchData PlanetShipLaunchData;
            public PlanetData TargetPlanetData;
            public int ShipCount;
        }

        protected override void OnCreateManager(int capacity)
        {
            _shipsToSpawn = new NativeList<ShipSpawnData>(Allocator.Persistent);
        }

        protected override void OnDestroyManager()
        {
            _shipsToSpawn.Dispose();
        }

        protected override void OnStartRunning()
        {
            _prefabManager = GameObject.FindObjectOfType<PrefabManager>();

            // If we run through the scene switcher we need to wait for OnStartRunning to trigger before we can setup the onetime stuff
            // since the objects we expect won't be available otherwise
            if (_shipRenderer != null)
                return;
            var prefabRenderer = _prefabManager.ShipPrefab.GetComponent<MeshInstanceRendererComponent>().Value;

            var planetSpawner = GameObject.FindObjectOfType<PlanetSpawner>();
            _shipRenderer = new MeshInstanceRenderer[planetSpawner._teamMaterials.Length];
            for (var i = 0; i < _shipRenderer.Length; ++i)
            {
                _shipRenderer[i] = prefabRenderer;
                _shipRenderer[i].material = new Material(prefabRenderer.material)
                {
                    color = planetSpawner._teamMaterials[i].color
                };
                _shipRenderer[i].material.SetColor("_EmissionColor", planetSpawner._teamMaterials[i].color);
            }
            base.OnStartRunning();
        }

        [Inject]
        SpawningPlanets _planets;
        PrefabManager _prefabManager;
        EntityManager _entityManager;

        NativeList<ShipSpawnData> _shipsToSpawn;

        MeshInstanceRenderer[] _shipRenderer;

        protected override void OnUpdate()
        {

            for(var planetIndex = 0; planetIndex < _planets.Length; planetIndex++)
            {
                var planetLaunchData = _planets.Data[planetIndex];
                if (planetLaunchData.NumberToSpawn == 0)
                {
                    continue;
                }
                var shipsToSpawn = planetLaunchData.NumberToSpawn;

                var dt = Time.deltaTime;
                var deltaSpawn = Math.Max(1, Convert.ToInt32(1000.0f * dt));

                if (deltaSpawn < shipsToSpawn)
                    shipsToSpawn = deltaSpawn;
                var targetPlanet = _entityManager.GetComponentData<PlanetData>(planetLaunchData.TargetEntity);

                _shipsToSpawn.Add(new ShipSpawnData
                {
                    ShipCount = shipsToSpawn,
                    PlanetShipLaunchData = planetLaunchData,
                    TargetPlanetData = targetPlanet
                });

                var launchData = new PlanetShipLaunchData
                {
                    TargetEntity = planetLaunchData.TargetEntity,
                    NumberToSpawn = planetLaunchData.NumberToSpawn - shipsToSpawn,
                    TeamOwnership = planetLaunchData.TeamOwnership,
                    SpawnLocation = planetLaunchData.SpawnLocation,
                    SpawnRadius = planetLaunchData.SpawnRadius
                };
                _planets.Data[planetIndex] = launchData;
            }

            for (int spawnIndex = 0; spawnIndex < _shipsToSpawn.Length; ++spawnIndex)
            {
                var spawnCount = _shipsToSpawn[spawnIndex].ShipCount;
                var planet = _shipsToSpawn[spawnIndex].PlanetShipLaunchData;
                var targetPlanet = _shipsToSpawn[spawnIndex].TargetPlanetData;

                var planetPos = planet.SpawnLocation;
                var planetDistance = Vector3.Distance(planetPos, targetPlanet.Position);
                var planetRadius = planet.SpawnRadius;

                var prefabShipEntity =_entityManager.Instantiate(_prefabManager.ShipPrefab);
                _entityManager.SetSharedComponentData(prefabShipEntity, _shipRenderer[planet.TeamOwnership]);

                var entities = new NativeArray<Entity>(spawnCount, Allocator.Temp);
                _entityManager.Instantiate(prefabShipEntity, entities);
                _entityManager.DestroyEntity(prefabShipEntity);

                for (int i = 0; i < spawnCount; i++)
                {
                    float3 shipPos;
                    do
                    {
                        var insideCircle = Random.insideUnitCircle.normalized;
                        var onSphere = new float3(insideCircle.x, 0, insideCircle.y);
                        shipPos = planetPos + (onSphere * (planetRadius + _prefabManager.ShipPrefab.transform.localScale.x));
                    } while (math.lengthSquared(shipPos - planetPos) > planetDistance * planetDistance);

                    var data = new ShipData
                    {
                        TargetEntity = planet.TargetEntity,
                        TeamOwnership = planet.TeamOwnership
                    };
                    _entityManager.AddComponentData(entities[i], data);
                    var spawnPosition = new Position
                    {
                        Value = shipPos
                    };

                    var transformMatix = new TransformMatrix
                    {
                        Value = float4x4.scale(new float3(0.02f, 0.02f, 0.02f))
                    };

                    _entityManager.SetComponentData(entities[i], transformMatix);
                    _entityManager.SetComponentData(entities[i], spawnPosition);
                }

                entities.Dispose();
            }

            _shipsToSpawn.Clear();
        }
    }
}
