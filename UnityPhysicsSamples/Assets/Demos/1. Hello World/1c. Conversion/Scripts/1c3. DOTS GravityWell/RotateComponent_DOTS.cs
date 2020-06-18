using Unity.Entities;
using Unity.Mathematics;

// 
// Note we are not using [GenerateAuthoringComponent] here.
// Using an Authoring component with conversion instead provides 
// a pipeline to convert degrees, used in the UI, to radians, used at runtime.
// If we did use [GenerateAuthoringComponent] the editor user would need
// to input radians values or the system would need to convert from degrees
// on every update.
//

public struct RotateComponent_DOTS : IComponentData
{
    public float3 LocalAngularVelocity; // in radian/sec
}


