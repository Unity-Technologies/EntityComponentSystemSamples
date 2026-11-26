using Unity.Netcode;
using UnityEngine;

namespace GameObjectSample
{
    public class PredictionBubble : GhostBehaviour
    {
        [SerializeField] float m_EnterRadius;
        [SerializeField] float m_ExitRadius;
        [SerializeField] float m_TransitionTime;

        Transform m_Transform;

        public override void Awake()
        {
            base.Awake();
            if (IsServer)
            {
                enabled = false;
                return;
            }

            MovementBehaviour.AllSwitchedCubes.TryAdd(Ghost.World, new ());
            m_Transform = this.transform;
        }

        //This should run only for the ship the client owns (that are the predicted ones).
        //On the other spawned ship it should be ideally destroyed (but we don't support right now the
        //stripping per-ghost-mode.
        private void OnEnable()
        {
            enabled = this.Ghost.CanWriteState;
        }

        void Update()
        {
            var enterSqrRadius = m_EnterRadius * m_EnterRadius;
            var exitSqrRadius = m_ExitRadius * m_ExitRadius;
            var position = m_Transform.localPosition;
            foreach (var asteroid in MovementBehaviour.AllSwitchedCubes[Ghost.World])
            {
                if (!asteroid)
                {
                    Debug.LogWarning("A destroyed asteroid present in the list");
                    continue;
                }
                var distance = Vector3.SqrMagnitude(asteroid.transform.localPosition - position);
                var switchColor = asteroid.SwitchColor;
                if (distance < enterSqrRadius)
                {
                    if (asteroid.Ghost.ConvertToPredicted(m_TransitionTime))
                    {
                        switchColor.SwitchToPredicted();
                    }
                }
                else if (distance > exitSqrRadius )
                {
                    if (asteroid.Ghost.ConvertToInterpolated(m_TransitionTime))
                    {
                        switchColor.SwitchToInterpolated();
                    }
                }
            }
        }
    }
}
