using Unity.Transforms;
using Unity.Mathematics;
using Unity.NetCode;

namespace Asteroids.Mixed
{

    [GhostComponentVariation(typeof(LocalTransform), "Transform - 2D")]
    [GhostComponent(PrefabType=GhostPrefabType.All, SendTypeOptimization=GhostSendType.AllClients)]
    public struct PositionRotation2d
    {
        //Will serialize just x,y positions
        [GhostField(Quantization=1000, Smoothing=SmoothingAction.InterpolateAndExtrapolate, SubType=GhostFieldSubType.Translation2D)]
        public float3 Position;
        //Will serialize just the one angle
        [GhostField(Quantization=1000, Smoothing=SmoothingAction.InterpolateAndExtrapolate, SubType=GhostFieldSubType.Rotation2D)]
        public quaternion Rotation;
    }

}
