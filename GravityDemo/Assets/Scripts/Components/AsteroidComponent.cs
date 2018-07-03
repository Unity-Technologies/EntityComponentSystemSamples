using Unity.Entities;

public struct Asteroid : IComponentData
{
}

class AsteroidComponent : ComponentDataWrapper<Asteroid>
{
}