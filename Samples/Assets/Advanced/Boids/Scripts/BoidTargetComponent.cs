using System;
using Unity.Entities;

[Serializable]
public struct BoidTarget : IComponentData { }

[UnityEngine.DisallowMultipleComponent]
public class BoidTargetComponent : ComponentDataWrapper<BoidTarget> { }
