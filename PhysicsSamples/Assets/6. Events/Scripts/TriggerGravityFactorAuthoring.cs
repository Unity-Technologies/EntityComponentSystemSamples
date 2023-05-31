using Unity.Entities;
using UnityEngine;

namespace Events
{
    public class TriggerGravityFactorAuthoring : MonoBehaviour
    {
        public float GravityFactor = 0f;
        public float DampingFactor = 0.9f;

        class Baker : Baker<TriggerGravityFactorAuthoring>
        {
            public override void Bake(TriggerGravityFactorAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new TriggerGravityFactor()
                {
                    GravityFactor = authoring.GravityFactor,
                    DampingFactor = authoring.DampingFactor,
                });
            }
        }
    }

    public struct TriggerGravityFactor : IComponentData
    {
        public float GravityFactor;
        public float DampingFactor;
    }
}
