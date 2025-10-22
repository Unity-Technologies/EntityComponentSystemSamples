using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

/// <summary>Serializable attribute ensures the Inspector can expose fields as this struct is a field inside ServerSettings.</summary>
[Serializable]
public struct LevelComponent : IComponentData
{
    public int levelWidth;
    public int levelHeight;

    public float shipForwardForce;
    public float shipRotationRate;
    public float shipCollisionRadius;

    public float bulletVelocity;
    public float bulletCollisionRadius;
    /// <summary>Value of 0 implies one bullet per simulation tick. ROF cannot go higher than that.</summary>
    public uint bulletRofCooldownTicks;

    public float asteroidVelocity;
    public float asteroidCollisionRadius;
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
    public GhostDistanceData distanceImportanceTileConfig;

    /// <summary>Distributes the CollisionSystem work over N ticks (1 = OFF).</summary>
    [Min(1)]
    public uint collisionSystemRoundRobinSegments;

    public static LevelComponent Default = new LevelComponent
    {
        levelWidth = 2048,
        levelHeight = 2048,

        shipForwardForce = 50,
        shipRotationRate = 140,
        shipCollisionRadius = 10,

        bulletVelocity = 500,
        bulletCollisionRadius = 5,
        bulletRofCooldownTicks = 3,

        asteroidVelocity = 10,
        asteroidCollisionRadius = 15,
        numAsteroids = 800,

        asteroidsDamageShips = true,
        shipPvP = true,
        asteroidsDestroyedOnShipContact = true,
        bulletsDestroyedOnContact = true,

        enableGhostImportanceScaling = true,
        distanceImportanceTileConfig = new GhostDistanceData
        {
            TileSize = new int3(512, 512, 10240),
            TileCenter = new int3(0, 0, 0),
            TileBorderWidth = new float3(1f),
        },
        relevancyRadius = 1400,
        staticAsteroidOptimization = false,
        collisionSystemRoundRobinSegments = 1,
    };
}
