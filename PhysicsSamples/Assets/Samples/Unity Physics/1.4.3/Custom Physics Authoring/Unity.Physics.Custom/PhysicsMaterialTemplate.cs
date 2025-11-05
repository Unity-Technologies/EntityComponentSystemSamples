using UnityEngine;

namespace Unity.Physics.Authoring
{
    [CreateAssetMenu(menuName = "Unity Physics/Physics Material Template", fileName = "Physics Material Template", order = 508)]
    public sealed class PhysicsMaterialTemplate : ScriptableObject, IPhysicsMaterialProperties
    {
        PhysicsMaterialTemplate() {}

        public CollisionResponsePolicy CollisionResponse { get => m_Value.CollisionResponse; set => m_Value.CollisionResponse = value; }

        public PhysicsMaterialCoefficient Friction { get => m_Value.Friction; set => m_Value.Friction = value; }

        public PhysicsMaterialCoefficient Restitution { get => m_Value.Restitution; set => m_Value.Restitution = value; }

        public PhysicsCategoryTags BelongsTo { get => m_Value.BelongsTo; set => m_Value.BelongsTo = value; }

        public PhysicsCategoryTags CollidesWith { get => m_Value.CollidesWith; set => m_Value.CollidesWith = value; }

        public CustomPhysicsMaterialTags CustomTags { get => m_Value.CustomTags; set => m_Value.CustomTags = value; }

        public PhysicsMaterialFlag DetailedStaticMeshCollision { get => m_Value.DetailedStaticMeshCollision; set => m_Value.DetailedStaticMeshCollision = value; }

        [SerializeField]
        PhysicsMaterialProperties m_Value = new PhysicsMaterialProperties(false);

        void Reset() => OnValidate();

        void OnValidate() => PhysicsMaterialProperties.OnValidate(ref m_Value, false);
    }
}
