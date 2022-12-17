using Unity.Entities;
using Unity.Mathematics;

namespace StateMachine
{
    public struct Hit : IComponentData
    {
        public float3 Value;
        public bool ChangedThisFrame;  
    }
}