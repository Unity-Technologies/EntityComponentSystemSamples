using System;
using UnityEngine;

namespace KickBall
{
    [CreateAssetMenu(fileName = "Config", menuName = "ScriptableObjects/ConfigScriptableObject", order = 1)]
    public class ConfigScriptableObject : ScriptableObject
    {
        public PlayerConfig Player;
        public ObstacleConfig Obstacle;
        public BallConfig Ball;

        [Header("Prefabs")] 
        public GameObject ObstaclePrefab;
        public GameObject PlayerPrefab;
        public GameObject BallPrefab;
    }
}