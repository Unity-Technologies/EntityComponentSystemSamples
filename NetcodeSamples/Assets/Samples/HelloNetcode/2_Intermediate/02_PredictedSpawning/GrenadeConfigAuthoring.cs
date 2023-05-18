using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct GrenadeConfig : IComponentData
    {
        public int InitialVelocity;
        public float BlastTimer;
        public int BlastRadius;
        public int BlastPower;
        public float ExplosionTimer;
    }

    [DisallowMultipleComponent]
    public class GrenadeConfigAuthoring : MonoBehaviour
    {
        public int InitialVelocity = 15;
        public float BlastTimer = 3f;
        public int BlastRadius = 40;
        public int BlastPower = 10;
        public float ExplosionTimer = 1.9f;

        class Baker : Baker<GrenadeConfigAuthoring>
        {
            public override void Bake(GrenadeConfigAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new GrenadeConfig
                {
                    InitialVelocity = authoring.InitialVelocity,
                    BlastTimer = authoring.BlastTimer,
                    BlastRadius = authoring.BlastRadius,
                    BlastPower = authoring.BlastPower,
                    ExplosionTimer = authoring.ExplosionTimer
                });
            }
        }
    }
}
