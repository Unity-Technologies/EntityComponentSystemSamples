using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;

namespace MotionProperties
{
    public class PhysicsMassAuthoring : MonoBehaviour
    {
        [Header("Physics Mass")]
        public bool InfiniteInertiaX = false;
        public bool InfiniteInertiaY = false;
        public bool InfiniteInertiaZ = false;
        public bool InfiniteMass = false;
        [Header("Physics Mass Override")]
        public bool IsKinematic = false;
        public bool SetVelocityToZero = false;

        class Baker : Baker<PhysicsMassAuthoring>
        {
            private bool HasPhysics()
            {
                return (GetComponent<Rigidbody>() != null || GetComponent<PhysicsBodyAuthoring>() != null);
            }

            public override void Bake(PhysicsMassAuthoring authoring)
            {
                if (HasPhysics())
                {
                    var entity = GetEntity(TransformUsageFlags.Dynamic);
                    if (authoring.IsKinematic || authoring.SetVelocityToZero)
                    {
                        AddComponent(entity, new PhysicsMassOverride()
                        {
                            IsKinematic = (byte)(authoring.IsKinematic ? 1 : 0),
                            SetVelocityToZero = (byte)(authoring.SetVelocityToZero ? 1 : 0)
                        });
                    }

                    AddComponent(entity, new PhysicsMassBaking()
                    {
                        InfiniteInertiaX = authoring.InfiniteInertiaX,
                        InfiniteInertiaY = authoring.InfiniteInertiaY,
                        InfiniteInertiaZ = authoring.InfiniteInertiaZ,
                        InfiniteMass = authoring.InfiniteMass
                    });
                }
            }
        }
    }

    [TemporaryBakingType]
    public struct PhysicsMassBaking : IComponentData
    {
        public bool InfiniteInertiaX;
        public bool InfiniteInertiaY;
        public bool InfiniteInertiaZ;
        public bool InfiniteMass;
    }
}
