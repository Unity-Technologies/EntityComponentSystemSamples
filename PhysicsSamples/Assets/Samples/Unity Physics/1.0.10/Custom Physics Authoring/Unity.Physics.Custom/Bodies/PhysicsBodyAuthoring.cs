using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Authoring
{
    /// <summary>   The physics body authoring. This class cannot be inherited. </summary>
#if UNITY_2021_2_OR_NEWER
    [Icon(k_IconPath)]
#endif
    [AddComponentMenu("Entities/Physics/Physics Body")]
    [DisallowMultipleComponent]
    public sealed class PhysicsBodyAuthoring : MonoBehaviour
    {
        const string k_IconPath = "Packages/com.unity.physics/Unity.Physics.Editor/Editor Default Resources/Icons/d_Rigidbody@64.png";

        PhysicsBodyAuthoring() {}

        public BodyMotionType MotionType { get => m_MotionType; set => m_MotionType = value; }
        [SerializeField]
        [Tooltip("Specifies whether the body should be fully physically simulated, moved directly, or fixed in place.")]
        BodyMotionType m_MotionType;

        public BodySmoothing Smoothing { get => m_Smoothing; set => m_Smoothing = value; }
        [SerializeField]
        [Tooltip("Specifies how this body's motion in its graphics representation should be smoothed when the rendering framerate is greater than the fixed step rate used by physics.")]
        BodySmoothing m_Smoothing = BodySmoothing.None;

        const float k_MinimumMass = 0.001f;

        public float Mass
        {
            get => m_MotionType == BodyMotionType.Dynamic ? m_Mass : float.PositiveInfinity;
            set => m_Mass = math.max(k_MinimumMass, value);
        }
        [SerializeField]
        float m_Mass = 1.0f;

        public float LinearDamping { get => m_LinearDamping; set => m_LinearDamping = math.max(0f, value); }
        [SerializeField]
        [Tooltip("This is applied to a body's linear velocity reducing it over time.")]
        float m_LinearDamping = 0.01f;

        public float AngularDamping { get => m_AngularDamping; set => m_AngularDamping = math.max(0f, value); }
        [SerializeField]
        [Tooltip("This is applied to a body's angular velocity reducing it over time.")]
        float m_AngularDamping = 0.05f;

        public float3 InitialLinearVelocity { get => m_InitialLinearVelocity; set => m_InitialLinearVelocity = value; }
        [SerializeField]
        [Tooltip("The initial linear velocity of the body in world space")]
        float3 m_InitialLinearVelocity = float3.zero;

        public float3 InitialAngularVelocity { get => m_InitialAngularVelocity; set => m_InitialAngularVelocity = value; }
        [SerializeField]
        [Tooltip("This represents the initial rotation speed around each axis in the local motion space of the body i.e. around the center of mass")]
        float3 m_InitialAngularVelocity = float3.zero;

        public float GravityFactor
        {
            get => m_MotionType == BodyMotionType.Dynamic ? m_GravityFactor : 0f;
            set => m_GravityFactor = value;
        }
        [SerializeField]
        [Tooltip("Scales the amount of gravity to apply to this body.")]
        float m_GravityFactor = 1f;

        public bool OverrideDefaultMassDistribution
        {
#pragma warning disable 618
            get => m_OverrideDefaultMassDistribution;
            set => m_OverrideDefaultMassDistribution = value;
#pragma warning restore 618
        }
        [SerializeField]
        [Tooltip("Default mass distribution is based on the shapes associated with this body.")]
        bool m_OverrideDefaultMassDistribution;

        public MassDistribution CustomMassDistribution
        {
            get => new MassDistribution
            {
                Transform = new RigidTransform(m_Orientation, m_CenterOfMass),
                InertiaTensor =
                    m_MotionType == BodyMotionType.Dynamic ? m_InertiaTensor : new float3(float.PositiveInfinity)
            };
            set
            {
                m_CenterOfMass = value.Transform.pos;
                m_Orientation.SetValue(value.Transform.rot);
                m_InertiaTensor = value.InertiaTensor;
#pragma warning disable 618
                m_OverrideDefaultMassDistribution = true;
#pragma warning restore 618
            }
        }

        [SerializeField]
        float3 m_CenterOfMass;

        [SerializeField]
        EulerAngles m_Orientation = EulerAngles.Default;

        [SerializeField]
        // Default value to solid unit sphere : https://en.wikipedia.org/wiki/List_of_moments_of_inertia
        float3 m_InertiaTensor = new float3(2f / 5f);

        public uint WorldIndex { get => m_WorldIndex; set => m_WorldIndex = value; }
        [SerializeField]
        [Tooltip("The index of the physics world this body belongs to. Default physics world has index 0.")]
        uint m_WorldIndex = 0;

        public CustomPhysicsBodyTags CustomTags { get => m_CustomTags; set => m_CustomTags = value; }
        [SerializeField]
        CustomPhysicsBodyTags m_CustomTags = CustomPhysicsBodyTags.Nothing;

        void OnEnable()
        {
            // included so tick box appears in Editor
        }

        void OnValidate()
        {
            m_Mass = math.max(k_MinimumMass, m_Mass);
            m_LinearDamping = math.max(m_LinearDamping, 0f);
            m_AngularDamping = math.max(m_AngularDamping, 0f);
        }
    }
}
