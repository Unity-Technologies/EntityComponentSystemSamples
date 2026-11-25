using System;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace GameObjectSample
{
    [DefaultExecutionOrder(1)] // should execute before PredictedBehaviour to change the velocity
    [RequireComponent(typeof(PredictedBehaviour))]
    public partial class PredictedBehaviourExecuteWithSortOrder : GhostBehaviour
    {
        PredictedBehaviour movingBehaviour;

        public override void Awake()
        {
            base.Awake();
            movingBehaviour = GetComponent<PredictedBehaviour>();
        }

        public override void PredictionUpdate(PredictionUpdateContext context)
        {
            // This is a mock example of dependency between state updates. PredictionUpdate will respect DefaultExecutionOrder.
            var value = movingBehaviour.Velocity.Value;
            value.x = math.cos(Netcode.Time.ServerTick.TickIndexForValidTick * 0.05f) * movingBehaviour.VelocityMagnitude;
            value.z = math.sin(Netcode.Time.ServerTick.TickIndexForValidTick * 0.05f) * movingBehaviour.VelocityMagnitude;
            movingBehaviour.Velocity.Value = value;
        }
    }
}
