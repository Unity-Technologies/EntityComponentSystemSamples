using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    public class BallAndSocketJoint : BaseJoint
    {
        // Editor only settings
        [HideInInspector]
        public bool EditPivots;

        [Tooltip("If checked, PositionLocal will snap to match PositionInConnectedEntity")]
        public bool AutoSetConnected = true;

        public float3 PositionLocal;
        public float3 PositionInConnectedEntity;

        public virtual void UpdateAuto()
        {
            if (AutoSetConnected)
            {
                RigidTransform bFromA = math.mul(math.inverse(worldFromB), worldFromA);
                PositionInConnectedEntity = math.transform(bFromA, PositionLocal);
            }
        }

        public override void Create(EntityManager entityManager, GameObjectConversionSystem conversionSystem)
        {
            UpdateAuto();
            conversionSystem.World.GetOrCreateSystem<EndJointConversionSystem>().CreateJointEntity(
                this,
                GetConstrainedBodyPair(conversionSystem),
                PhysicsJoint.CreateBallAndSocket(PositionLocal, PositionInConnectedEntity)
            );
        }
    }
}
