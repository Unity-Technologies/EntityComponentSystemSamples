using System.Collections.Generic;
using Unity.Entities;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;

namespace GameObjectSample
{
    /// <summary>
    /// Moves the cube in a circle around its spawn point. The movement doesn't depend on the player, so the field of cubes
    /// forms a predictable pattern the player can walk through to observe the switch between interpolated and predicted.
    /// </summary>
    public partial class MovementBehaviour : GhostBehaviour
    {
        const float k_FullTurn = 2f * Mathf.PI;

        public static Dictionary<World, List<MovementBehaviour>> AllSwitchedCubes = new();

        // ghost fields so they're rolled back on prediction
        GhostField<Vector3> m_Center = new(fieldConfig: new FieldConfig() {Quantization = 100});
        GhostField<float> m_Angle = new(fieldConfig: new FieldConfig() {Quantization = 1000});

        [SerializeField]
        float m_RotationSpeed = 5;
        [SerializeField]
        float m_OrbitRadius = 1;
        [SerializeField]
        float m_OrbitSpeed = 2; // radians per second
        Transform m_CachedTransform;
        internal SwitchColor SwitchColor;

        public override void Awake()
        {
            base.Awake();
            if (!AllSwitchedCubes.ContainsKey(Ghost.World))
                AllSwitchedCubes.Add(Ghost.World, new List<MovementBehaviour>());
            AllSwitchedCubes[Ghost.World].Add(this);
            m_CachedTransform = transform;
            if (IsServer)
                SetOrbit(m_CachedTransform.position, 0f);

            SwitchColor = GetComponent<SwitchColor>();
        }

        public override void OnDestroy()
        {
            AllSwitchedCubes[Ghost.World].Remove(this);
            base.OnDestroy();
        }

        public override void PredictionUpdate(PredictionUpdateContext context)
        {
            m_CachedTransform.Rotate(new Vector3(0f, 0f, m_RotationSpeed * context.TickedDeltaTime));
            m_Angle.Value = Mathf.Repeat(m_Angle.Value + m_OrbitSpeed * context.TickedDeltaTime, k_FullTurn);
            m_CachedTransform.position = OrbitPosition(m_Angle.Value);
        }

        /// <summary>
        /// Called by the spawning routine, server-side only, to set the circle's center and where on the circle the cube starts.
        /// Different start angles for neighboring cubes make the field move in a wave. Can't be called on OnEnable because of pooling.
        /// </summary>
        public void SetOrbit(Vector3 center, float startAngle)
        {
            Assert.IsNotNull(Ghost);
            Assert.IsTrue(IsServer);

            m_Center.Value = center;
            m_Angle.Value = Mathf.Repeat(startAngle, k_FullTurn);
            m_CachedTransform.position = OrbitPosition(m_Angle.Value);
        }

        Vector3 OrbitPosition(float angle)
        {
            return m_Center.Value + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * m_OrbitRadius;
        }
    }
}
