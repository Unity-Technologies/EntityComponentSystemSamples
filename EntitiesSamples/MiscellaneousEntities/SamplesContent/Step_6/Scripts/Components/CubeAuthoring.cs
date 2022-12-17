using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace StateMachine
{
    public class CubeAuthoring : MonoBehaviour
    {
        public class CubeBaker : Baker<CubeAuthoring>
        {
            public override void Bake(CubeAuthoring enabledAuthoring)
            {
                AddComponent(new URPMaterialPropertyBaseColor { Value = (Vector4)Color.white });
                AddComponent<Cube>();
                AddComponent<Spinner>();
            }
        }
    }
    
    public struct Cube : IComponentData
    {
    }
}