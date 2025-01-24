using Unity.Entities;
using UnityEngine;

namespace Tutorials.Kickball.Step1
{
    // An "authoring component" is a MonoBehaviour that has a corresponding Baker<T> class.
    public class ObstacleAuthoring : MonoBehaviour
    {
        // In this simple case, the authoring component itself doesn't need any fields.
        // Nesting a baker inside its associated authoring component is
        // the prescribed style, but it's not a requirement.
        // The name of the baker class doesn't really matter: the important part is that we've
        // defined a class inheriting Baker<ObstacleAuthoring>.
        class Baker : Baker<ObstacleAuthoring>
        {
            // Bake() is called every time the authoring component gets re-baked.
            // The authoring component is passed to the parameter (though in this case we ignore the parameter).
            public override void Bake(ObstacleAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                // Add the Obstacle component to the entity produced in baking
                // from the authoring component's GameObject.
                AddComponent<Obstacle>(entity);
            }
        }
    }

    // Basic entity component types are defined by a struct implementing IComponentData.
    // The interface has no methods: it simply marks the type as a component type.
    public struct Obstacle : IComponentData
    {
        // The struct can include any unmanaged fields, but in this case we leave it empty.
        // An empty struct is called a "tag component". Though they contain no data, tag components can
        // still be queried like any other component type, and so they are useful for marking entities.
        // This Obstacle tag component is added to all obstacle entities so that we can query for all obstacles.
    }
}
