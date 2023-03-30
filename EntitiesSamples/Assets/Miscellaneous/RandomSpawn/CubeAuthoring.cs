using Unity.Entities;
using UnityEngine;

namespace Miscellaneous.RandomSpawn
{
    public class CubeAuthoring : MonoBehaviour { }

    public class CubeBaker : Baker<CubeAuthoring>
    {
        public override void Bake(CubeAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<Cube>(entity);
            AddComponent<NewSpawn>(entity);
        }
    }

    public struct Cube : IComponentData { }
    public struct NewSpawn : IComponentData { }
}
