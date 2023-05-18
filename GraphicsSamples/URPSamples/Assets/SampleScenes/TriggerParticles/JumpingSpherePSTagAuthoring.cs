using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct JumpingSpherePSTag : IComponentData {}

[DisallowMultipleComponent]
public class JumpingSpherePSTagAuthoring : MonoBehaviour
{
    class JumpingSpherePSTagBaker : Baker<JumpingSpherePSTagAuthoring>
    {
        public override void Bake(JumpingSpherePSTagAuthoring authoring)
        {
            JumpingSpherePSTag component = default(JumpingSpherePSTag);
            AddComponent(component);
        }
    }
}
