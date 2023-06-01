using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace Unity.Physics.Extensions
{
    [DisallowMultipleComponent]
    public class MouseHoverAuthoring : MonoBehaviour
    {
        public GameObject HoverPrefab;
        public bool IgnoreTriggers = true;
        public bool IgnoreStatic = true;

        protected void OnEnable() {}

        class Baker : Baker<MouseHoverAuthoring>
        {
            public override void Bake(MouseHoverAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new MouseHover()
                {
                    PreviousEntity = Entity.Null,
                    CurrentEntity = Entity.Null,
                    IgnoreTriggers = authoring.IgnoreTriggers,
                    IgnoreStatic = authoring.IgnoreStatic,
                    HoverEntity = GetEntity(authoring.HoverPrefab, TransformUsageFlags.Dynamic),
                });
            }
        }
    }

    public struct MouseHover : IComponentData, IEquatable<MouseHover>
    {
        public bool IgnoreTriggers;
        public bool IgnoreStatic;
        public Entity PreviousEntity;
        public Entity CurrentEntity;
        public Entity HoverEntity;
        public MaterialMeshInfo OriginalMeshInfo;

        public bool Equals(MouseHover other) =>
            Equals(PreviousEntity, other.PreviousEntity)
            && Equals(CurrentEntity, other.CurrentEntity)
            && Equals(OriginalMeshInfo, other.OriginalMeshInfo)
            && Equals(HoverEntity, other.HoverEntity);

        public override bool Equals(object obj) => obj is MouseHover other && Equals(other);

        public override int GetHashCode() =>
            unchecked((int)math.hash(new int4x2(
                new int4(
                    IgnoreTriggers ? 1 : 0,
                    IgnoreStatic ? 1 : 0,
                    PreviousEntity.GetHashCode(),
                    CurrentEntity.GetHashCode()),
                new int4(
                    HoverEntity != Entity.Null ? HoverEntity.GetHashCode() : 0,
                    OriginalMeshInfo.GetHashCode(),
                    0, 0))
            ));
    }
}
