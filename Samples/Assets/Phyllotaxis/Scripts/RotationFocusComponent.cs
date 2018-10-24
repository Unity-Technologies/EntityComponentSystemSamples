using System;
using Unity.Entities;

[Serializable]
public struct RotationFocus : IComponentData
{
}

[UnityEngine.DisallowMultipleComponent]
public class RotationFocusComponent : ComponentDataWrapper<RotationFocus>
{
}
