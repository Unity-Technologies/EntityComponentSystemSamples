using Unity.Entities;
using UnityEngine;

namespace TwoStickHybridExample
{
    public class MoveSystem : ComponentSystem
    {
        struct Data
        {
#pragma warning disable 649
            public Position2D Position;
            public Heading2D Heading;
            public MoveSpeed MoveSpeed;
#pragma warning restore 649
        }

        protected override void OnUpdate()
        {
            var dt = Time.deltaTime;
            foreach (var entity in GetEntities<Data>())
            {
                var pos = entity.Position;
                pos.Value += entity.Heading.Value*entity.MoveSpeed.Value*dt;
            }
        }
    }
}