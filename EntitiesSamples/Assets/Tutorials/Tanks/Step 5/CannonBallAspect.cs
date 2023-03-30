using Tutorials.Tanks.Step4;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Tutorials.Tanks.Step5
{
    public readonly partial struct CannonBallAspect : IAspect
    {
        // An Entity field in an aspect provides access to the entity itself.
        // This is required for registering commands in an EntityCommandBuffer for example.
        public readonly Entity Self;

        // Aspects can contain other aspects.
        readonly RefRW<LocalTransform> Transform;

        // A RefRW field provides read write access to a component. If the aspect is taken as an "in"
        // parameter, the field will behave as if it was a RefRO and will throw exceptions on write attempts.
        readonly RefRW<CannonBall> CannonBall;

        // Properties like this are not mandatory, the Transform field could just have been made public instead.
        // But they improve readability by avoiding chains of "aspect.aspect.aspect.component.value.value".
        public float3 Position
        {
            get => Transform.ValueRO.Position;
            set => Transform.ValueRW.Position = value;
        }

        public float3 Velocity
        {
            get => CannonBall.ValueRO.Velocity;
            set => CannonBall.ValueRW.Velocity = value;
        }
    }
}
