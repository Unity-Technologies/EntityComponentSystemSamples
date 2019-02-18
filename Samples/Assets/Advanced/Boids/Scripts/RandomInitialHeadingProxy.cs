using System;
using Unity.Entities;

public struct RandomInitialHeading : IComponentData { }

[UnityEngine.DisallowMultipleComponent]
public class RandomInitialHeadingProxy : ComponentDataProxy<RandomInitialHeading> { }
