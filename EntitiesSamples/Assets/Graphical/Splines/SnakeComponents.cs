using Unity.Entities;
using Unity.Mathematics;

namespace Graphical.Splines
{
    public struct SnakeSettings : IComponentData
    {
        public Entity Prefab;
        public int NumPartsPerSnake;
        public int NumSnakes;
        public float Speed;
        public float Spacing;
    }

    public struct SplineData
    {
        public BlobArray<float3> Points;
        public BlobArray<float> Distance;
    }

    public struct Spline : IComponentData
    {
        public BlobAssetReference<SplineData> Data;
    }

    [MaximumChunkCapacity(4)]
    public struct Snake : IComponentData
    {
        public float Offset;
        public float3 Anchor;
        public Entity SplineEntity;
        public float Speed;
        public float Spacing;
    }

    [InternalBufferCapacity(0)]
    public struct SnakePart : IBufferElementData
    {
        public Entity Value;
    }
}
