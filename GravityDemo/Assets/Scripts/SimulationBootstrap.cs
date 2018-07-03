using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class SimulationBootstrap
{
    public static SimulationSettings settings { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void InitializeSimulation()
    {
        var settingsGo = GameObject.Find("Settings");
        if (settingsGo == null)
        {
            SceneManager.sceneLoaded += OnScenLoaded;
            return;
        }

        StartSimulation();
    }

    private static void OnScenLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        StartSimulation();
    }

    private static void StartSimulation()
    {
        settings = GameObject.Find("Settings").GetComponent<SimulationSettings>();
        var entityManager = World.Active.GetOrCreateManager<EntityManager>();

        var instances = new NativeArray<Entity>(settings.SimSize, Allocator.Temp);

        entityManager.Instantiate(settings.asteroidPrefab, instances);
        var asteroidTransform = settings.asteroidPrefab.transform;

        for (var i = 0; i < instances.Length; ++i)
        {
            float3 position = asteroidTransform.position;
            position += math.cross(Random.insideUnitSphere, settings.Spread);

            entityManager.SetComponentData(instances[i], new Position { Value = position });
            entityManager.SetComponentData(instances[i], new Velocity { Value = (asteroidTransform.forward) * (settings.AsteroidSpeed + Random.Range(-settings.StartSpeedRandomness, settings.StartSpeedRandomness))});

            entityManager.SetComponentData(instances[i], new Mass { Value = Random.Range(-settings.MassRandomness, settings.MassRandomness) + (asteroidTransform.localScale.x * asteroidTransform.localScale.x) });
            entityManager.SetComponentData(instances[i], new Asteroid());
        }

        instances.Dispose();
    }
}
