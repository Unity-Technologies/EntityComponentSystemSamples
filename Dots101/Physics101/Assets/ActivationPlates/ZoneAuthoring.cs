using Unity.Entities;
using UnityEngine;

namespace ActivationPlates
{
    public class ZoneAuthoring : MonoBehaviour
    {
        public ZoneType Type;

        private class Baker : Baker<ZoneAuthoring>
        {
            public override void Bake(ZoneAuthoring authoring)
            {
                var entity = GetEntity(authoring, TransformUsageFlags.None);

                AddComponent(entity, new Zone
                {
                    Type = authoring.Type,
                    State = ZoneState.Outside
                });
            }
        }
    }

    public struct Zone : IComponentData
    {
        public ZoneType Type;
        public ZoneState State;

        // count of the last physic update when this trigger emitted a trigger event
        // (ulong to make rollover a non-issue)
        public ulong LastPhysicsUpdateCount; 

        // elapsed time when last occupied
        public float LastTriggerTime; 
    }
    
    public enum ZoneState
    {
        Inside,    
        Outside,
        Enter,    // now inside, but was outside in the prior update
        Exit,     // now outside, but was inside in the prior update
    }
    
    public enum ZoneType
    {
        OneTime,
        Continuous,
        Reenterable,
        OnExit,
    }
}