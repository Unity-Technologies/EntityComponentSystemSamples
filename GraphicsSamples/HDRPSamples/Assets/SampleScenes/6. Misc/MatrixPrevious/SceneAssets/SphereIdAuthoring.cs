using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct SphereId : IComponentData
{
}

[DisallowMultipleComponent]
public class SphereIdAuthoring : MonoBehaviour
{
    class SphereIdBaker : Baker<SphereIdAuthoring>
    {
        public override void Bake(SphereIdAuthoring authoring)
        {
            SphereId component = default(SphereId);
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}
