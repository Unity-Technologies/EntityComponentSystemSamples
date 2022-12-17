using UnityEngine;
using Unity.Entities;

namespace HelloCube.GameObjectSync
{
    [AddComponentMenu("HelloCube/GameObjectPrefab")]
    public class GameObjectPrefabAuthoring : MonoBehaviour
    {
        public GameObject Prefab;

        public class Baker : Baker<GameObjectPrefabAuthoring>
        {
            public override void Bake(GameObjectPrefabAuthoring authoring)
            {
                AddComponentObject(new GameObjectPrefab { Prefab = authoring.Prefab });
            }
        }
    }

    // Stores a GameObject prefab that will be instantiated and associated with the entity.
    public class GameObjectPrefab : IComponentData
    {
        public GameObject Prefab;
    }
    
    public class RotatingGameObject : IComponentData
    {
        public GameObject Value;

        public RotatingGameObject(GameObject value)
        {
            Value = value;
        }

        // Every class IComponentData must have a no-arg constructor.
        public RotatingGameObject()
        {
        }
    }
}