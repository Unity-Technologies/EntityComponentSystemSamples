using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Netcode;
using UnityEngine;

namespace GameObjectSample
{
    public struct CharacterInput : IInputComponentData
    {
        public Vector2 Movement;
        // InputEvent needed to make sure we're not missing inputs when capturing inputs outside fixed rate ticks
        public InputEvent Jump;
        public InputEvent DoHit;
    }

    // This is a for a poor man's OnValueChanged. We'll be looking at something better for this.
    public struct HealthComponent : IComponentData
    {
        [GhostField] public int health;
        public int pastHealth;
    }

    [DefaultExecutionOrder(12)] // This to show you can set execution order for ghost methods too
    public partial class PredictedCharacterController : GhostBehaviour
    {
        GhostInput<CharacterInput> input;
        GhostField<float> m_GravityVelocity;

        [SerializeField] float VelocityConfig = 5;
        [SerializeField] float JumpVelocityConfig = 5;
        GhostComponentRef<HealthComponent> healthRef;

        EntityQuery m_PredictedGhostsWithHealthQuery;

        public override void Awake()
        {
            base.Awake();
            // Make sure to add Simulate to your query, to guarantee the ghost is predicted and in the same timeline as you are. Predicted ghost interacting with a interpolated ghosts often cause mispredictions/jitter.
            m_PredictedGhostsWithHealthQuery = Ghost.World.EntityManager.CreateEntityQuery(typeof(HealthComponent), typeof(Simulate));
        }

        public override void OnDestroy()
        {
            m_PredictedGhostsWithHealthQuery.Dispose();
            base.OnDestroy();
        }

        public override void PredictionGatherInput(PredictionGatherInputContext context)
        {
            var inputValue = input.PrepareToCapture(); // this could be "default" as well.

            {
                if (Input.GetKey("left") || Input.GetKey("a"))
                    inputValue.Movement.x -= 1;
                if (Input.GetKey("right") || Input.GetKey("d"))
                    inputValue.Movement.x += 1;
                if (Input.GetKey("down") || Input.GetKey("s"))
                    inputValue.Movement.y -= 1;
                if (Input.GetKey("up") || Input.GetKey("w"))
                    inputValue.Movement.y += 1;
            }
            if (Input.GetKeyUp(KeyCode.E))
            {
                // KeyUp or Down need to be captured with InputEvent, else input sampling rate higher than your tick rate means you'd lose inputs.
                Debug.Log($"setting input for tick {Netcode.Time.ServerTick.TickIndexForValidTick}");
                inputValue.DoHit.Set();
            }

            if (Input.GetKeyDown("space"))
                inputValue.Jump.Set();
            input.Capture(inputValue);
        }

        void Update()
        {
            if (IsClient)
            {
                var health = healthRef.Read();

                // Debug.Log($"Update: health {health.health} previous {health.pastHealth}", this.gameObject);
                if (health.health < health.pastHealth)
                {
                    DoVFX();
                }

                health.pastHealth = health.health;
                healthRef.Write(health);
            }
        }

        private void DoVFX()
        {
            Debug.Log($"Health decrease VFX!!! {healthRef.Read().health} previous:{healthRef.Read().pastHealth}", this.gameObject);
        }

        public override void PredictionUpdate(PredictionUpdateContext context)
        {
            var inputValue = input.GetForCurrentTick();
            if (inputValue.Jump.IsSet)
                m_GravityVelocity.Value = JumpVelocityConfig;
            m_GravityVelocity.Value -= 9.8f * context.TickedDeltaTime;
            var horizontalMovement = new Vector3(inputValue.Movement.x, 0, inputValue.Movement.y).normalized * VelocityConfig;
            transform.position += (Vector3.up * m_GravityVelocity.Value + horizontalMovement) * context.TickedDeltaTime;
            if (transform.position.y < 0)
            {
                transform.position = new Vector3(transform.position.x, 0, transform.position.z);
                m_GravityVelocity.Value = 0f;
            }

            if (inputValue.DoHit.IsSet)
            {
                // This is a small demo on how you'd do hybrid GO + ECS code from GO code. This implements a simple query to get all ghosts that have health and changes their health.
                var em = this.Ghost.World.EntityManager;
                Debug.Log($"isclient:{IsClient} input set! for tick {Netcode.Time.ServerTick.TickIndexForValidTick}");

                if (!m_PredictedGhostsWithHealthQuery.IsEmpty)
                {
                    var healthComponents = m_PredictedGhostsWithHealthQuery.ToComponentDataArray<HealthComponent>(Allocator.Temp);
                    var entities = m_PredictedGhostsWithHealthQuery.ToEntityArray(Allocator.Temp);
                    for (int i = 0; i < entities.Length; i++)
                    {
                        if (entities[i] != Ghost.Entity)
                        {
                            em.SetComponentData(entities[i], new HealthComponent(){health = healthComponents[i].health - 1, pastHealth = healthComponents[i].pastHealth});
                        }
                    }
                }
            }
        }
    }
}
