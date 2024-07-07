using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Samples.HelloNetcode
{
    public struct GrenadeConfig : IComponentData
    {
        public int InitialVelocity;
        public float BlastTimer;
        public int BlastRadius;
        public int BlastPower;
        public float BlastPowerClampY;
        public float ChainReactionForceExplodeDurationSeconds;
    }

    [DisallowMultipleComponent]
    public class GrenadeConfigAuthoring : MonoBehaviour
    {
        public int InitialVelocity = 15;
        public float BlastTimer = 3f;
        public int BlastRadius = 40;
        public int BlastPower = 10;
        [Tooltip("Force some verticality to the blast direction, by clamping abs(positionDelta.y) to this value. Applied BEFORE the BlastPower calculation. In velocity (m/s).")]
        public float BlastPowerClampY = 1.5f;
        [Tooltip("When a grenade is hit by another grenade, force the victim grenade to explode at a maximum of this value later, in seconds. I.e. Causing a grenade 'chain reaction'.")]
        public float ChainReactionForceExplodeDurationSeconds = 0.4f;

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
                    BlastPowerClampY = authoring.BlastPowerClampY,
                    ChainReactionForceExplodeDurationSeconds = authoring.ChainReactionForceExplodeDurationSeconds,
                });
            }
        }
    }
}
