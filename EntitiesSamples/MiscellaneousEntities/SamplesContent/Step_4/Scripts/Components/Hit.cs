using Unity.Entities;
using Unity.Mathematics;

namespace StateMachineValue
{
    public struct Hit : IComponentData
    {
        public float3 Value;
        public bool ChangedThisFrame;
    }

}