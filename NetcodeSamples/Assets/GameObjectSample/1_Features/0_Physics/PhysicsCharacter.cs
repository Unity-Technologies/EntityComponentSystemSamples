using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Samples.HelloNetcode.Physics
{
    // Just normal inputs, used for this particular sample. Nothing physics specific.
    public struct PhysicsInput : IInputComponentData
    {
        public float horizontal;
        public float vertical;
        public InputEvent doForce;
    }

    [RequireComponent(typeof(Rigidbody))]
    public partial class PhysicsCharacter : GhostBehaviour
    {
        public Rigidbody characterRigidbody;

        GhostInput<PhysicsInput> m_Input;

        public override void Awake()
        {
            base.Awake();
            characterRigidbody = GetComponent<Rigidbody>();
        }

        public override void PredictionGatherInput(PredictionGatherInputContext context)
        {
            var input = default(PhysicsInput);
            input.horizontal = Input.GetKey(KeyCode.A) ? -1f : Input.GetKey(KeyCode.D) ? 1f : 0f; // using Input.GetAxis caused discrepancies. how will that work for consoles?
            input.vertical = Input.GetKey(KeyCode.S) ? -1f : Input.GetKey(KeyCode.W) ? 1f : 0f;
            if (Input.GetKeyUp(KeyCode.T))
            {
                input.doForce.Set();
            }

            this.m_Input.Capture(input);
        }

        // The same way GO side we have FixedUpdate, then Physics.Simulate(), then Update, Netcode side we have PredictionPhysicsUpdate, then Simulate, then PredictionUpdate
        public override void PredictionPhysicsUpdate(PredictionPhysicsUpdateContext context)
        {
            var input = this.m_Input.GetForCurrentTick();
            var targetVelocity = new Vector3(input.horizontal, 0, input.vertical).normalized * 10f;
            var velocity = this.characterRigidbody.linearVelocity;

            // We can't set position directly. Even if this works fine in a singleplayer scenario, prediction requires a relatively deterministic simulation.
            // Continuously deoverlapping forced position changes isn't deterministic, so we need to use velocity
            this.characterRigidbody.AddForce(targetVelocity - new Vector3(velocity.x, 0, velocity.z), ForceMode.Impulse);

            if (input.doForce.IsSet
#if UNITY_EDITOR
                || (Input.GetKeyUp(KeyCode.T) && !IsClient)  // debug hack to trigger this in a dedicated server clone editor directly
#endif
                )
            {
                // make sure the seed is deterministic
                Random.InitState((int)Netcode.Time.ServerTick.TickIndexForValidTick);
                var direction = (Random.onUnitSphere + Vector3.up).normalized;
                foreach (var body in PushableBody.AllBodies[Ghost.World])
                {
                    body.AddForce(direction * 35, ForceMode.Impulse);
                }
            }
        }
    }
}
