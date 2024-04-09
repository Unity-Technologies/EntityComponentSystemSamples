#if !UNITY_DISABLE_MANAGED_COMPONENTS
using System;
using Unity.Entities;
using UnityEngine;

namespace Graphical.AnimationWithGameObjects
{
    public class WarriorAuthoring : MonoBehaviour
    {
        public GameObject Prefab;

        class Baker : Baker<WarriorAuthoring>
        {
            public override void Bake(WarriorAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponentObject(entity, new WarriorGOPrefab
                {
                    Prefab = authoring.Prefab
                });
                AddComponent<WanderState>(entity);
            }
        }
    }

    public class WarriorGOPrefab : IComponentData
    {
        public GameObject Prefab;
    }

    public class WarriorGOInstance : IComponentData, IDisposable
    {
        public GameObject Instance;

        public void Dispose()
        {
            UnityEngine.Object.DestroyImmediate(Instance);
        }
    }

    public struct WanderState : IComponentData
    {
        public float NextActionTime;
        public float Period;
    }
}

#endif
