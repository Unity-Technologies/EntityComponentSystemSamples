using Unity.Mathematics;
using UnityEngine;

namespace TwoStickClassicExample
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
                    
            var shotXform = newShot.GetComponent<Transform2D>();
            shotXform.Position = data.Position;
            shotXform.Heading = data.Heading;

            var shotFaction = newShot.GetComponent<Faction>();
            shotFaction.Value = data.Faction.Value;
        }
    }

}
