using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Samples.HelloCube_07
{
    // This system updates all entities in the scene with Translation components.
    // It treats entities differently depending on whether or not they also have a MoveUp component.
    public class MovementSystem : ComponentSystem
    {
        protected override void OnUpdate()
        {
            // If a MoveUp component is present, then the system updates the Translation component to move the entity upwards.
            // Once the entity reaches a predetermined height, the function removes the MoveUp component.
            Entities.WithAllReadOnly<MovingCube, MoveUp>().ForEach(
                (Entity id, ref Translation translation) =>
                {
                    var deltaTime = Time.deltaTime;
                    translation = new Translation()
                    {
                        Value = new float3(translation.Value.x, translation.Value.y + deltaTime, translation.Value.z)
                    };

                    // Components can only be added or removed using PostUpdateCommands.
                    if (translation.Value.y > 10.0f)
                        PostUpdateCommands.RemoveComponent<MoveUp>(id);
                }
            );

            // If an entity does not have a MoveUp component (but does have a Translation component),
            // then the system moves the entity down to its starting point and adds a MoveUp component.
            Entities.WithAllReadOnly<MovingCube>().WithNone<MoveUp>().ForEach(
                (Entity id, ref Translation translation) =>
                {
                    translation = new Translation()
                    {
                        Value = new float3(translation.Value.x, -10.0f, translation.Value.z)
                    };

                    PostUpdateCommands.AddComponent(id, new MoveUp());
                }
            );
        }
    }
}
