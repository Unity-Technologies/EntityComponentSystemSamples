using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Samples.HelloCube_1b
{
    // This system updates all entities in the scene with Translation components.
    // It treats entities differently depending on whether or not they also have a MoveUp component.
    public class MovementSystem_ForEachWithEntityChanges : ComponentSystem
    {
        protected override void OnUpdate()
        {
            // If a MoveUp component is present, then the system updates the Translation component to move the entity upwards.
            // Once the entity reaches a predetermined height, the function removes the MoveUp component.
            Entities.WithAllReadOnly<MovingCube_ForEachWithEntityChanges, MoveUp_ForEachWithEntityChanges>().ForEach(
                (Entity id, ref Translation translation) =>
                {
                    var deltaTime = Time.deltaTime;
                    translation = new Translation()
                    {
                        Value = new float3(translation.Value.x, translation.Value.y + deltaTime, translation.Value.z)
                    };

                    if (translation.Value.y > 10.0f)
                        EntityManager.RemoveComponent<MoveUp_ForEachWithEntityChanges>(id);
                }
            );

            // If an entity does not have a MoveUp component (but does have a Translation component),
            // then the system moves the entity down to its starting point and adds a MoveUp component.
            Entities.WithAllReadOnly<MovingCube_ForEachWithEntityChanges>().WithNone<MoveUp_ForEachWithEntityChanges>().ForEach(
                (Entity id, ref Translation translation) =>
                {
                    translation = new Translation()
                    {
                        Value = new float3(translation.Value.x, -10.0f, translation.Value.z)
                    };

                    EntityManager.AddComponentData(id, new MoveUp_ForEachWithEntityChanges());
                }
            );
        }
    }
}
