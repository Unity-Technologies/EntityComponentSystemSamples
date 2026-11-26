using Unity.Netcode;
using UnityEngine;

namespace GameObjectSample
{
    [DefaultExecutionOrder(2)]
    public partial class PredictedBehaviour : GhostBehaviour
    {
        public GhostField<Vector3> Velocity;
        public float VelocityMagnitude;

        public override void PredictionUpdate(PredictionUpdateContext context)
        {
            this.transform.position += context.TickedDeltaTime * Velocity.Value;
        }
    }
}
