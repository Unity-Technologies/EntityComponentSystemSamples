using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine;
using LegacyRigidBody = UnityEngine.Rigidbody;

public class SetPhysicsMassBehaviour : MonoBehaviour
{
    [Header("Physics Mass")]
    public bool InfiniteInertiaX = false;
    public bool InfiniteInertiaY = false;
    public bool InfiniteInertiaZ = false;
    public bool InfiniteMass = false;
    [Header("Physics Mass Override")]
    public bool IsKinematic = false;
    public bool SetVelocityToZero = false;

    [TemporaryBakingType]
    public struct SetPhysicsMassAuthoring : IComponentData
    {
        public bool InfiniteInertiaX;
        public bool InfiniteInertiaY;
        public bool InfiniteInertiaZ;
        public bool InfiniteMass;
    }

    class SetPhysicsMassBehaviourBaker : Baker<SetPhysicsMassBehaviour>
    {
        private bool HasPhysics()
        {
            return (GetComponent<LegacyRigidBody>() != null || GetComponent<PhysicsBodyAuthoring>() != null);
        }

        public override void Bake(SetPhysicsMassBehaviour authoring)
        {
            if (HasPhysics())
            {
                if (authoring.IsKinematic || authoring.SetVelocityToZero)
                {
                    AddComponent(new PhysicsMassOverride()
                    {
                        IsKinematic = (byte)(authoring.IsKinematic ? 1 : 0),
                        SetVelocityToZero = (byte)(authoring.SetVelocityToZero ? 1 : 0)
                    });
                }

                AddComponent(new SetPhysicsMassAuthoring()
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

[UpdateAfter(typeof(PhysicsBodyBakingSystem))]
[UpdateAfter(typeof(LegacyRigidbodyBakingSystem))]
[UpdateAfter(typeof(EndJointBakingSystem))]
[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
public partial class SetPhysicsMassBehaviourSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // Fill in the MassProperties based on the potential calculated value by BuildCompoundColliderBakingSystem
        Entities
            .ForEach(
            (ref PhysicsMass mass, in SetPhysicsMassBehaviour.SetPhysicsMassAuthoring setPhysicsMass) =>
            {
                mass.InverseInertia[0] = setPhysicsMass.InfiniteInertiaX ? 0 : mass.InverseInertia[0];
                mass.InverseInertia[1] = setPhysicsMass.InfiniteInertiaY ? 0 : mass.InverseInertia[1];
                mass.InverseInertia[2] = setPhysicsMass.InfiniteInertiaZ ? 0 : mass.InverseInertia[2];
                mass.InverseMass = setPhysicsMass.InfiniteMass ? 0 : mass.InverseMass;
            }).Run();
    }
}
