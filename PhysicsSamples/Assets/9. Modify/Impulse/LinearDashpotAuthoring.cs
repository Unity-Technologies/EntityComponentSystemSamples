using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEngine;

/*
 * Issues:
 *  - setting up constraints if not using GameObjects
 *  - providing utility functions for Component and Direct data manipulation
 *  - assigning multiple Components of the same type to a single Entity
 */

namespace Modify
{
    public class LinearDashpotAuthoring : MonoBehaviour
    {
        public PhysicsBodyAuthoring parentBody;
        public float3 parentOffset;
        public float3 localOffset;

        public bool dontApplyImpulseToParent = false;
        public float strength;
        public float damping;

        void OnEnable()
        {
        }

        class Baker : Baker<LinearDashpotAuthoring>
        {
            public override void Bake(LinearDashpotAuthoring authoring)
            {
                if (authoring.enabled)
                {
                    // Note: GetPrimaryEntity currently creates a new Entity
                    //       if the parentBody is not a child in the scene hierarchy
                    var componentData = new LinearDashpot
                    {
                        localEntity = GetEntity(TransformUsageFlags.Dynamic),
                        localOffset = authoring.localOffset,
                        parentEntity = authoring.parentBody == null
                            ? Entity.Null
                            : GetEntity(authoring.parentBody, TransformUsageFlags.Dynamic),
                        parentOffset = authoring.parentOffset,
                        dontApplyImpulseToParent = authoring.dontApplyImpulseToParent ? 1 : 0,
                        strength = authoring.strength,
                        damping = authoring.damping
                    };

                    Entity dashpotEntity = CreateAdditionalEntity(TransformUsageFlags.Dynamic);
                    AddComponent(dashpotEntity, componentData);
                }
            }
        }
    }

    public struct LinearDashpot : IComponentData
    {
        public Entity localEntity;
        public float3 localOffset;
        public Entity parentEntity;
        public float3 parentOffset;

        public int dontApplyImpulseToParent;
        public float strength;
        public float damping;
    }
}
