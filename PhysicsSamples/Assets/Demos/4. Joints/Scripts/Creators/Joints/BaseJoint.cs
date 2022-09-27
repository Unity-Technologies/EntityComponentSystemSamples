using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Physics.Authoring
{
    public abstract class BaseJoint : BaseBodyPairConnector
    {
        public bool EnableCollision;
        public bool RaiseImpulseEvents;
        public float3 MaxImpulse = float.PositiveInfinity;

        void OnEnable()
        {
            // included so tick box appears in Editor
        }

        protected PhysicsConstrainedBodyPair GetConstrainedBodyPair(GameObjectConversionSystem conversionSystem)
        {
            return new PhysicsConstrainedBodyPair(
                conversionSystem.GetPrimaryEntity(this),
                ConnectedBody == null ? Entity.Null : conversionSystem.GetPrimaryEntity(ConnectedBody),
                EnableCollision
            );
        }
    }
}
