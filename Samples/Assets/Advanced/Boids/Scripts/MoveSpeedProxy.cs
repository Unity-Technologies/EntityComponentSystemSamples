using System;
using Unity.Entities;

/// <summary>
/// Store float speed. This component requests that if another component is moving the PositionProxy
/// it should respect this value and move the position at the constant speed specified.
/// </summary>
[Serializable]
public struct MoveSpeed : IComponentData
{
    public float speed;
}

[UnityEngine.DisallowMultipleComponent]
public class MoveSpeedProxy : ComponentDataProxy<MoveSpeed> { }
