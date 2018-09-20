using System;
using Unity.Entities;

namespace Samples.Common
{
    /// <summary>
    /// Store float speed. This component requests that if another component is moving the PositionComponent
    /// it should respect this value and move the position at the constant speed specified.
    /// </summary>
    [Serializable]
    public struct MoveSpeed : IComponentData
    {
        public float speed;
    }

    [UnityEngine.DisallowMultipleComponent]
    public class MoveSpeedComponent : ComponentDataWrapper<MoveSpeed> { } 
}
