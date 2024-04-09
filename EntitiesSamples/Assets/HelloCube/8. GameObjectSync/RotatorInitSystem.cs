using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace HelloCube.GameObjectSync
{
#if !UNITY_DISABLE_MANAGED_COMPONENTS
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct RotatorInitSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<DirectoryManaged>();
            state.RequireForUpdate<ExecuteGameObjectSync>();
        }

        // This OnUpdate accesses managed objects, so it cannot be burst compiled.
        public void OnUpdate(ref SystemState state)
        {
            var directory = SystemAPI.ManagedAPI.GetSingleton<DirectoryManaged>();
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // Instantiate the associated GameObject from the prefab.
            foreach (var (goPrefab, entity) in
                     SystemAPI.Query<RotationSpeed>()
                         .WithNone<RotatorGO>()
                         .WithEntityAccess())
            {
                var go = GameObject.Instantiate(directory.RotatorPrefab);

                // We can't add components to entities as we iterate over them, so we defer the change with an ECB.
                ecb.AddComponent(entity, new RotatorGO(go));
            }

            ecb.Playback(state.EntityManager);
        }
    }

    public class RotatorGO : IComponentData
    {
        public GameObject Value;

        public RotatorGO(GameObject value)
        {
            Value = value;
        }

        // Every IComponentData class must have a no-arg constructor.
        public RotatorGO()
        {
        }
    }
#endif
}

