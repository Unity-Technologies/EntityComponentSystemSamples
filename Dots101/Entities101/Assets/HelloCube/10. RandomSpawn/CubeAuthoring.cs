using Unity.Entities;
using UnityEngine;

namespace HelloCube.RandomSpawn
{
    public class CubeAuthoring : MonoBehaviour
    {
        public class Baker : Baker<CubeAuthoring>
        {
            public override void Bake(CubeAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<Cube>(entity);
                AddComponent<NewSpawn>(entity);
            }
        }
    }

    public struct Cube : IComponentData
    {
    }

    public struct NewSpawn : IComponentData
    {
    }
}
