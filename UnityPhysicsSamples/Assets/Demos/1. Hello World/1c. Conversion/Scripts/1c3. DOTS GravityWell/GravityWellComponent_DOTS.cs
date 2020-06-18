using Unity.Entities;
using Unity.Mathematics;

// 
// Note we are not using [GenerateAuthoringComponent] here.
// Using an Authoring component with conversion instead provides 
// a clear UI in the editor.
//
public struct GravityWellComponent_DOTS : IComponentData
{
    public float Strength;
    public float Radius;
    public float3 Position;
}
