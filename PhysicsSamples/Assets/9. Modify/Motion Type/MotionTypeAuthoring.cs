using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace Modify
{
    public class MotionTypeAuthoring : MonoBehaviour
    {
        public UnityEngine.Material DynamicMaterial;
        public UnityEngine.Material KinematicMaterial;
        public UnityEngine.Material StaticMaterial;

        [Range(0, 10)] public float TimeToSwap = 1.0f;
        public bool SetVelocityToZero = false;

        class Baker : Baker<MotionTypeAuthoring>
        {
            public override void Bake(MotionTypeAuthoring authoring)
            {
                var velocity = new PhysicsVelocity();
                var physicsBodyAuthoring = GetComponent<PhysicsBodyAuthoring>();
                if (physicsBodyAuthoring != null)
                {
                    velocity.Linear = physicsBodyAuthoring.InitialLinearVelocity;
                    velocity.Angular = physicsBodyAuthoring.InitialAngularVelocity;
                }

                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new MotionType
                {
                    NewMotionType = BodyMotionType.Dynamic,
                    DynamicInitialVelocity = velocity,
                    TimeLimit = authoring.TimeToSwap,
                    Timer = authoring.TimeToSwap,
                    SetVelocityToZero = authoring.SetVelocityToZero
                });
                AddSharedComponentManaged(entity, new MotionMaterials
                {
                    DynamicMaterial = authoring.DynamicMaterial,
                    KinematicMaterial = authoring.KinematicMaterial,
                    StaticMaterial = authoring.StaticMaterial
                });
                AddComponent<PhysicsMassOverride>(entity);
            }
        }
    }

    public struct MotionType : IComponentData
    {
        public BodyMotionType NewMotionType;
        public PhysicsVelocity DynamicInitialVelocity;
        public float TimeLimit;
        public bool SetVelocityToZero;
        internal float Timer;
    }

    public struct MotionMaterials : ISharedComponentData, IEquatable<MotionMaterials>
    {
        public UnityEngine.Material DynamicMaterial;
        public UnityEngine.Material KinematicMaterial;
        public UnityEngine.Material StaticMaterial;

        public bool Equals(MotionMaterials other) =>
            Equals(DynamicMaterial, other.DynamicMaterial)
            && Equals(KinematicMaterial, other.KinematicMaterial)
            && Equals(StaticMaterial, other.StaticMaterial);

        public override bool Equals(object obj) => obj is MotionMaterials other && Equals(other);

        public override int GetHashCode() =>
            unchecked((int)math.hash(new int3(
                DynamicMaterial != null ? DynamicMaterial.GetHashCode() : 0,
                KinematicMaterial != null ? KinematicMaterial.GetHashCode() : 0,
                StaticMaterial != null ? StaticMaterial.GetHashCode() : 0
            )));
    }
}
