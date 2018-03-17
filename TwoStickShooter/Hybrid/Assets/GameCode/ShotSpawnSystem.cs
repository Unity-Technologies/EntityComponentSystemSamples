using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TwoStickHybridExample
{

    public class ShotSpawnData
    {
        public float2 Position;
        public float2 Heading;
        public Faction Faction;
    }

    public static class ShotSpawnSystem
    {
        public static void SpawnShot(ShotSpawnData data)
        {
            var settings = TwoStickBootstrap.Settings;
            var prefab = data.Faction.Value == Faction.Type.Player
                ? settings.PlayerShotPrefab
                : settings.EnemyShotPrefab;
            var newShot = Object.Instantiate(prefab);

            var shotPos = newShot.GetComponent<Position2D>();
            shotPos.Value = data.Position;
            var shotHeading = newShot.GetComponent<Heading2D>();
            shotHeading.Value = data.Heading;

            var shotFaction = newShot.GetComponent<Faction>();
            shotFaction.Value = data.Faction.Value;
        }
    }

}
