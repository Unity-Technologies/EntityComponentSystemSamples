using Unity.Entities;
using Unity.Mathematics;

namespace StateMachine
{
    public struct Spinner : IComponentData, IEnableableComponent
    {
        // (the component should actually be empty, but this dummy field
        // was added as workaround for a bug in source generation)
        public int Value;   
    }
}