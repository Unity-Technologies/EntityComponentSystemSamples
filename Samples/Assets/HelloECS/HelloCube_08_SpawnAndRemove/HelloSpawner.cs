using Unity.Entities;

namespace Samples.HelloCube_08
{
    public struct HelloSpawner8 : IComponentData
    {
        public int CountX;
        public int CountY;
        public Entity Prefab;
    }    
}
