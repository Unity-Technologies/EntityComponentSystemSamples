using System;
using Unity.Entities;

[Serializable]
public struct RandomInitialHeading : IComponentData { }

[UnityEngine.DisallowMultipleComponent]
public class RandomInitialHeadingComponent : ComponentDataWrapper<RandomInitialHeading> { }
