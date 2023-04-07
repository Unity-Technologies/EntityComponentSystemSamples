using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace LearnSample
{
    public struct MoveSpeedComponent : IComponentData
    {
        public float value;
    }
}
