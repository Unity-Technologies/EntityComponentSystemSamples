using Unity.Entities;

namespace Unity.Physics.Authoring
{
    public abstract class BaseJoint : BaseBodyPairConnector
    {
        public bool EnableCollision;

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
