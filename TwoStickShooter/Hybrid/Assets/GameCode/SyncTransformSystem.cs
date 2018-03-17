using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TwoStickHybridExample
{
    public class SyncTransformSystem : ComponentSystem
    {
        public struct Data
        {

            [ReadOnly] public Position2D Position;
            [ReadOnly] public Heading2D Heading;
            public Transform Output;
        }

        protected override void OnUpdate()
        {
            foreach (var entity in GetEntities<Data>())
            {
                
                float2 p = entity.Position.Value;
                float2 h = entity.Heading.Value;
                entity.Output.position = new float3(p.x, 0, p.y);
                if (!h.Equals(new float2(0f, 0f)))
                    entity.Output.rotation = Quaternion.LookRotation(new float3(h.x, 0f, h.y), new float3(0f, 1f, 0f));
            }
        }
    }

}

