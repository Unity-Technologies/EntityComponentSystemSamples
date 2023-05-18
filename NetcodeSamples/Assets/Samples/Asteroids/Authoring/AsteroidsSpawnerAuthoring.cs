using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class AsteroidsSpawnerAuthoring : MonoBehaviour
{
    public GameObject Ship;
    public GameObject Bullet;
    public GameObject Asteroid;
    public GameObject StaticAsteroid;

    class Baker : Baker<AsteroidsSpawnerAuthoring>
    {
        public override void Bake(AsteroidsSpawnerAuthoring authoring)
        {
            AsteroidsSpawner component = default(AsteroidsSpawner);
            component.Ship = GetEntity(authoring.Ship, TransformUsageFlags.Dynamic);
            component.Bullet = GetEntity(authoring.Bullet, TransformUsageFlags.Dynamic);
            component.Asteroid = GetEntity(authoring.Asteroid, TransformUsageFlags.Dynamic);
            component.StaticAsteroid = GetEntity(authoring.StaticAsteroid, TransformUsageFlags.Dynamic);
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}
