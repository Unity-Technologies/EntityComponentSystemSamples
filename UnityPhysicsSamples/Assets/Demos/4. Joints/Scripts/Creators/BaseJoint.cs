using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    public abstract class BaseJoint : MonoBehaviour
    {
        public PhysicsBodyAuthoring ConnectedBody;

        public RigidTransform worldFromA => new RigidTransform(gameObject.transform.rotation, gameObject.transform.position);
        public RigidTransform worldFromB => (ConnectedBody == null) ? RigidTransform.identity : new RigidTransform(ConnectedBody.transform.rotation, ConnectedBody.transform.position);

        private Entity m_entityA = Entity.Null;
        public Entity entityA { get => m_entityA; set => m_entityA = value; }

        private Entity m_entityB = Entity.Null;
        public Entity entityB { get => m_entityB; set => m_entityB = value; }
        public bool EnableCollision;

        void OnEnable()
        {
            // included so tick box appears in Editor
        }

        protected unsafe void CreateJointEntity(BlobAssetReference<JointData> jointData, EntityManager entityManager)
        {
            var componentData = new PhysicsJoint
            {
                JointData = jointData,
                EntityA = entityA,
                EntityB = entityB,
                EnableCollision = EnableCollision ? 1 : 0,
            };

            ComponentType[] componentTypes = new ComponentType[1];
            componentTypes[0] = typeof(PhysicsJoint);
            Entity jointEntity = entityManager.CreateEntity(componentTypes);
#if UNITY_EDITOR
            var nameEntityA = entityManager.GetName(entityA);
            var nameEntityB = entityB == Entity.Null ? "PhysicsWorld" : entityManager.GetName(entityB);
            entityManager.SetName(jointEntity, $"Joining {nameEntityA} + {nameEntityB}");
#endif

            if (!entityManager.HasComponent<PhysicsJoint>(jointEntity))
            {
                entityManager.AddComponentData(jointEntity, componentData);
            }
            else
            {
                entityManager.SetComponentData(jointEntity, componentData);
            }
        }

        public abstract unsafe void Create(EntityManager entityManager);

    }
}
