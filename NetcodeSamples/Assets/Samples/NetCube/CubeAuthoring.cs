using Unity.Entities;
using Unity.Netcode;
using UnityEngine;

public struct Cube : IComponentData
{
}

[DisallowMultipleComponent]
public class CubeAuthoring : MonoBehaviour
{
    class Baker : Baker<CubeAuthoring>
    {
        public override void Bake(CubeAuthoring authoring)
        {
            // NonUniformScale bakes a PostTransformMatrix, used by CubeScaleSystem for the 3D scale demo
            var entity = GetEntity(TransformUsageFlags.Dynamic | TransformUsageFlags.NonUniformScale);
            AddComponent<Cube>(entity);
        }
    }
}
