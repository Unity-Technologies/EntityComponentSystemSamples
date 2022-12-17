using Unity.Entities;

class TankAuthoring : UnityEngine.MonoBehaviour
{
    class TankBaker : Baker<TankAuthoring>
    {
        public override void Bake(TankAuthoring authoring)
        {
            AddComponent<Tank>();
        }
    }
}

// Just like we did with the turret, we create a tag component to identify the tank (cube).
struct Tank : IComponentData
{
}