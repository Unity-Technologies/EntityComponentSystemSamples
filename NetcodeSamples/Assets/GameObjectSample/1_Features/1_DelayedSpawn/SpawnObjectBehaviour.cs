using Unity.Netcode;
using UnityEngine;

namespace GameObjectSample
{
    /// <summary>
    /// A general object that can be used for testing purposes
    /// </summary>
    public partial class SpawnObjectBehaviour : GhostBehaviour
    {
        [SerializeField]
        [Tooltip("This will make the spawned objects move around randomly")]
        private bool m_MoveRandomly = true;

        [SerializeField]
        private bool m_Rotate = false;

        [SerializeField]
        private Vector3 m_RotationAmount = new Vector3(0.0f, 1.0f, 0.0f);

        private Rigidbody m_RigidBody;
        private MeshRenderer m_MeshRenderer;
        private GhostField<Vector3> m_Direction;
        private GhostField<float> m_Velocity;


        [SerializeField]
        private float m_ObjectSpeed = 4;

        private void Start()
        {
            m_RigidBody = GetComponent<Rigidbody>();
            m_MeshRenderer = GetComponent<MeshRenderer>();

            float ang = Random.Range(0.0f, 2 * Mathf.PI);
            if (!Ghost.IsSpawned || IsServer)
            {
                SetDirectionAndVelocity(new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)), m_ObjectSpeed);
            }
        }

        /// <summary>
        /// Makes mesh renderer visible again
        /// </summary>
        public void Reset()
        {
            if (m_MeshRenderer == null)
            {
                m_MeshRenderer = GetComponent<MeshRenderer>();
            }
            m_MeshRenderer.enabled = true;
        }

        public void ShouldMoveRandomly(bool shouldMoveRandomly)
        {
            m_MoveRandomly = shouldMoveRandomly;
        }

        /// <summary>
        /// Sets the object's direction and velocity
        /// </summary>
        /// <param name="direction">vector3 direction</param>
        /// <param name="velocity">float velocity</param>
        public void SetDirectionAndVelocity(Vector3 direction, float velocity)
        {
            direction.Normalize();
            direction.y = 0;
            m_Direction.Value = direction;
            m_Velocity.Value = velocity;
        }

        /// <summary>
        /// Handles moving the object based on its current direction and velocity
        /// </summary>
        public override void PredictionPhysicsUpdate(PredictionPhysicsUpdateContext context)
        {
            var _transform = transform;

            var velocity = m_Velocity.Value;
            var direction = m_Direction.Value;
            m_RigidBody.MovePosition(_transform.position + direction * (velocity * context.TickedDeltaTime));
            _transform.position = m_RigidBody.position;

            if (m_Rotate)
            {
                _transform.rotation = Quaternion.Euler(_transform.rotation.eulerAngles + m_RotationAmount);
            }

            if (m_MoveRandomly && Random.Range(0.0f, 1.0f) < 0.01f)
            {
                if (IsServer)
                {
                    var label = GetComponentInChildren<ObjectLabel>();
                    if (label.existingStatus == ObjectLabel.SpawnStatus.SomeOtherValue)
                    {
                        label.UpdateStatus(ObjectLabel.SpawnStatus.Spawned);
                    }
                    else
                    {
                        label.UpdateStatus(ObjectLabel.SpawnStatus.SomeOtherValue);
                    }
                }
                var dir = Random.insideUnitCircle;
                direction.x = dir.x;
                direction.z = dir.y;
                m_Direction.Value = direction;
            }
        }
    }
}
