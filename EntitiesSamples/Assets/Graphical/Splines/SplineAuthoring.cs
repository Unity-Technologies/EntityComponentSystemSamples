using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Graphical.Splines
{
    public class SplineAuthoring : MonoBehaviour
    {
        public Transform[] ControlPoints;
        public int Subdivisions = 100;

        void OnValidate()
        {
            Subdivisions = math.max(1, Subdivisions);
        }

        class Baker : Baker<SplineAuthoring>
        {
            public override void Bake(SplineAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                var bufferEntity = AddBuffer<SplineControlPointEntity>(entity).Reinterpret<Entity>();
                var bufferPosition = AddBuffer<SplineControlPointPosition>(entity).Reinterpret<float3>();

                foreach (var point in authoring.ControlPoints)
                {
                    bufferEntity.Add(GetEntity(point, TransformUsageFlags.Dynamic));
                    bufferPosition.Add(GetComponent<Transform>(point).position);
                }

                AddComponent<Spline>(entity);
                AddComponent(entity, new SplineBakingData
                {
                    Subdivisions = authoring.Subdivisions
                });
            }
        }
    }

    [BakingType]
    public struct SplineControlPointEntity : IBufferElementData
    {
        public Entity Entity;
    }

    [BakingType]
    public struct SplineControlPointPosition : IBufferElementData
    {
        public float3 Position;
    }

    [BakingType]
    public struct SplineBakingData : IComponentData
    {
        public int Subdivisions;
    }
}
