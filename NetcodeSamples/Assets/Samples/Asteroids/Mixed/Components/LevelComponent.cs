using System;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine.Serialization;

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

    public bool asteroidsDamageShips;
    /// <summary>Can ships destroy each other?</summary>
    public bool shipPvP;
    public bool asteroidsDestroyedOnShipContact;
    public bool bulletsDestroyedOnContact;

    /// <summary>When > 0, informs <see cref="Unity.NetCode.GhostRelevancyMode"/>. Optimization.</summary>
    /// <remarks>
    /// Note: If <see cref="enableGhostImportanceScaling"/> is checked, the package uses the const defined in
    /// <see cref="GhostDistanceImportance.BatchScaleWithRelevancyFunctionPointer"/> instead of this field.
    /// </remarks>
    public int relevancyRadius;
    public bool staticAsteroidOptimization;
    public bool enableGhostImportanceScaling;

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

        asteroidsDamageShips = true,
        shipPvP = false,
        asteroidsDestroyedOnShipContact = false,
        bulletsDestroyedOnContact = true,

        relevancyRadius = 0,
        staticAsteroidOptimization = false,
    };
}
