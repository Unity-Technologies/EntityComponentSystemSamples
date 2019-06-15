using System;
using Unity.Entities;

namespace HelloCube.MySample._03
{
    public struct RotationSpeed : IComponentData
    {
        public float RadianPerSecond;
    }
}