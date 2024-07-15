using System;
using Unity.Entities;

/// <summary>Serializable attribute ensures the Inspector can expose fields as this struct is a field inside ServerSettings.</summary>
[Serializable]
public struct LevelComponent : IComponentData
{
    public int levelWidth;
    public int levelHeight;

    public float shipForwardForce;
    public float shipRotationRate;

    public float bulletVelocity;
    /// <summary>Value of 0 implies one bullet per simulation tick. ROF cannot go higher than that.</summary>
    public uint bulletRofCooldownTicks;

    public float asteroidVelocity;
    public int numAsteroids;

    [UnityEngine.SerializeField] private byte _asteroidsDamageShips;
    public bool asteroidsDamageShips => _asteroidsDamageShips != 0;

    /// <summary>Can ships destroy each other?</summary>
    [UnityEngine.SerializeField] private byte _shipPvP;
    public bool shipPvP => _shipPvP != 0;

    [UnityEngine.SerializeField] private byte _asteroidsDestroyedOnShipContact;
    public bool asteroidsDestroyedOnShipContact => _asteroidsDestroyedOnShipContact != 0;

    [UnityEngine.SerializeField] private byte _bulletsDestroyedOnContact;
    public bool bulletsDestroyedOnContact => _bulletsDestroyedOnContact != 0;

    /// <summary>When > 0, informs <see cref="Unity.NetCode.GhostRelevancyMode"/>. Optimization.</summary>
    public int relevancyRadius;

    [UnityEngine.SerializeField] private byte _staticAsteroidOptimization;
    public bool staticAsteroidOptimization => _staticAsteroidOptimization != 0;

    [UnityEngine.SerializeField] private byte _enableGhostImportanceScaling;
    public bool enableGhostImportanceScaling => _enableGhostImportanceScaling != 0;

    [UnityEngine.SerializeField] private byte _useBatchScalingFunction;
    public bool useBatchScalingFunction => _useBatchScalingFunction != 0;

    public static LevelComponent Default = new LevelComponent
    {
        levelWidth = 2048,
        levelHeight = 2048,

        shipForwardForce = 50,
        shipRotationRate = 100,

        bulletVelocity = 500,
        bulletRofCooldownTicks = 10,

        asteroidVelocity = 10,
        numAsteroids = 200,

        _asteroidsDamageShips = 1,
        _shipPvP = 0,
        _asteroidsDestroyedOnShipContact = 0,
        _bulletsDestroyedOnContact = 1,

        relevancyRadius = 0,
        _staticAsteroidOptimization = 0,
    };
}
