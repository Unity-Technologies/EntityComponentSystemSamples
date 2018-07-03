using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public struct Star : IComponentData
{
}

class StarComponent : ComponentDataWrapper<Star>
{
}
