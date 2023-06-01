using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Extensions
{
    [DisallowMultipleComponent]
    public class MousePickAuthoring : MonoBehaviour
    {
        public bool IgnoreTriggers = true;
        public bool IgnoreStatic = true;

        protected void OnEnable()
        {
        }

        class MousePickBaker : Baker<MousePickAuthoring>
        {
            public override void Bake(MousePickAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new MousePick()
                {
                    IgnoreTriggers = authoring.IgnoreTriggers,
                    IgnoreStatic = authoring.IgnoreStatic
                });
            }
        }
    }

    public struct MousePick : IComponentData
    {
        public bool IgnoreTriggers;
        public bool IgnoreStatic;
    }
}
