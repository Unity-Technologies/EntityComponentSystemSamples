using Unity.Entities;

// Authoring MonoBehaviours are regular GameObject components.
// They constitute the inputs for the baking systems which generates ECS data.
class TurretAuthoring : UnityEngine.MonoBehaviour
{
    // Bakers convert authoring MonoBehaviours into entities and components.
    class TurretBaker : Baker<TurretAuthoring>
    {
        public override void Bake(TurretAuthoring authoring)
        {
            AddComponent<Turret>();
        }
    }
}

// An empty component is called a "tag component".
struct Turret : IComponentData
{
}