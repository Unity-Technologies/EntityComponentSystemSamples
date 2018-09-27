using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class Bootstrap
{
    public static EntityArchetype CubeSpawner;
    public static EntityArchetype Cube;
    public static EntityArchetype CubeAttach;
    public static EntityArchetype rotationFocus;

    public static Settingscs Settings;
    public static Entity transform;

    public static void Initialize()
    {
        var entityManager = World.Active.GetOrCreateManager<EntityManager>();

        Cube = entityManager.CreateArchetype(typeof(Position), typeof(CubeComp),
            typeof(MeshInstanceRenderer), typeof(Rotation), typeof(RotationSpeed));
        CubeAttach = entityManager.CreateArchetype(typeof(Attach));
        
        rotationFocus = World.Active.GetOrCreateManager<EntityManager>().CreateArchetype(typeof(Position),
            typeof(Rotation),typeof(RotationSpeed), typeof(RotationFocus));
        
        CubeSpawner = entityManager.CreateArchetype(typeof(Position), typeof(Radius));

        entityManager.CreateEntity(CubeSpawner);

        transform = entityManager.CreateEntity(rotationFocus);
        entityManager.SetComponentData(transform, new Rotation {Value = quaternion.identity});
        entityManager.SetComponentData(transform, new Position {Value = new float3(0, 0, 0)});
        entityManager.SetComponentData(transform, new RotationSpeed {Value = 5});
        
        var spawnSystem = World.Active.GetOrCreateManager<CubeSpawnSystem>();
        spawnSystem.Enabled = true;
    }
    
    private static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        Settings = GameObject.Find("Settings")?.GetComponent<Settingscs>();
        if (Settings == null)
        {
            return;
        }
        Initialize();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void InitializeWithScene()
    {
        Settings = GameObject.Find("Settings")?.GetComponent<Settingscs>();
        var spawnSystem = World.Active.GetOrCreateManager<CubeSpawnSystem>();
        spawnSystem.Enabled = false;
        if (Settings == null)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            return;
        }
        
        Initialize();
    }
}
