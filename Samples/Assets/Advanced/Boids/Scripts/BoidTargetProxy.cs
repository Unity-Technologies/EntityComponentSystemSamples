using System;
using Unity.Entities;

public struct BoidTarget : IComponentData { }

[UnityEngine.DisallowMultipleComponent]
public class BoidTargetProxy : ComponentDataProxy<BoidTarget> { }
