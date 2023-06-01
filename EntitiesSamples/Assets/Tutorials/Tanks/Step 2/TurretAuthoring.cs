using Unity.Entities;
using UnityEngine;

namespace Tutorials.Tanks.Step2
{
    // Authoring MonoBehaviours are regular GameObject components.
    // They constitute the inputs for the baking systems which generates ECS data.
    class TurretAuthoring : MonoBehaviour
    {
        // Bakers convert authoring MonoBehaviours into entities and components.
        public GameObject CannonBallPrefab;
        public Transform CannonBallSpawn;

        class Baker : Baker<TurretAuthoring>
        {
            public override void Bake(TurretAuthoring authoring)
            {
                // GetEntity returns the baked Entity form of a GameObject.
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new Turret
                {
                    CannonBallPrefab = GetEntity(authoring.CannonBallPrefab, TransformUsageFlags.Dynamic),
                    CannonBallSpawn = GetEntity(authoring.CannonBallSpawn, TransformUsageFlags.Dynamic)
                });

                AddComponent<Shooting>(entity);
            }
        }
    }

    public struct Turret : IComponentData
    {
        // These fields will be used in step 4.


        // This entity will reference the prefab to be instantiated when the cannon shoots.
        public Entity CannonBallPrefab;

        // This entity will reference the nozzle of the cannon, where cannon balls should be spawned.
        public Entity CannonBallSpawn;
    }

    // This component will be used in Step 8.
    // This is a tag component that is also an "enableable component".
    // Such components can be toggled on and off without removing the component from the entity,
    // which would be less efficient and wouldn't retain the component's value.
    // An Enableable component is initially enabled.
    public struct Shooting : IComponentData, IEnableableComponent
    {
    }
}
