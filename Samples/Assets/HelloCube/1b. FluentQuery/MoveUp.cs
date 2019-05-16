using System;
using Unity.Entities;

namespace Samples.HelloCube_07
{
    // Serializable attribute is for editor support.
    [Serializable]
    public struct MoveUp : IComponentData
    {
        // MoveUp is a "tag" component and contains no data. Tag components can be used to mark entities that a system should process.
    }
}

