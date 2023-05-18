using Unity.Mathematics;

namespace Unity.Physics.Authoring
{
    public abstract class BaseJoint : BaseBodyPairConnector
    {
        public bool EnableCollision;
        public float3 MaxImpulse = float.PositiveInfinity;

        void OnEnable()
        {
            // included so tick box appears in Editor
        }
    }
}
