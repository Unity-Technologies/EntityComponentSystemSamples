using System;
using Unity.Entities;

[Serializable]
public struct RotationFocus : IComponentData
{
}

public class RotationFocusComponent : ComponentDataWrapper<RotationFocus>
{
}
