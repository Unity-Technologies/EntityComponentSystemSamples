using Unity.Entities;
using UnityEngine;

namespace RandomSpawn
{
    public class CubeAuthoring : MonoBehaviour { }

    public class CubeBaker : Baker<CubeAuthoring>
    {
        public override void Bake(CubeAuthoring authoring)
        {
            AddComponent<Cube>();
            AddComponent<NewSpawn>();
        }
    }

    public struct Cube : IComponentData { }
    public struct NewSpawn : IComponentData { }
}
