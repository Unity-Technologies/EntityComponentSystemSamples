using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class AsteroidsSpawnerAuthoring : MonoBehaviour
{
    public GameObject Ship;
    public GameObject Bullet;
    public GameObject Asteroid;
    public GameObject StaticAsteroid;

    class AsteroidsSpawnerBaker : Baker<AsteroidsSpawnerAuthoring>
    {
        public override void Bake(AsteroidsSpawnerAuthoring authoring)
        {
            AsteroidsSpawner component = default(AsteroidsSpawner);
            component.Ship = GetEntity(authoring.Ship);
            component.Bullet = GetEntity(authoring.Bullet);
            component.Asteroid = GetEntity(authoring.Asteroid);
            component.StaticAsteroid = GetEntity(authoring.StaticAsteroid);
            AddComponent(component);
        }
    }
}
