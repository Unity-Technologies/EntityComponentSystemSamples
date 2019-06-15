using Unity.Entities;
using UnityEngine;

namespace HelloCube.MySample._02
{
    public struct MoveTo : IComponentData
    {
        public float speed;
        public Vector3 to;
        public Vector3 velocity;
        public float smoothTime;        
    }
}