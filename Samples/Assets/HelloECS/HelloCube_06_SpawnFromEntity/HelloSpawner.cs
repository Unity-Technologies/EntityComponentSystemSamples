using Unity.Entities;

namespace Samples.HelloCube_06
{
    public struct HelloSpawner : IComponentData
    {
        public int CountX ;
        public int CountY;
        public Entity Prefab;
    }    
}
