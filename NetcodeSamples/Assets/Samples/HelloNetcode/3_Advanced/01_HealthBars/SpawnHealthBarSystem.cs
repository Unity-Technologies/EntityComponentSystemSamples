using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Samples.HelloNetcode
{
    public struct HealthUI : IComponentData
    {
        public UnityObjectRef<Transform> HealthBar;
        public UnityObjectRef<Image> HealthSlider;
        public float OpponentHeightOffset;
        public float PlayerHeightOffset;
        public float PlayerTowardCameraOffset;
    }

    // Mirrors HealthUI so the spawned UI GameObject can be destroyed when the ghost entity goes away.
    public struct HealthUICleanup : ICleanupComponentData
    {
        public UnityObjectRef<Transform> HealthBar;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct SpawnHealthBarSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<HealthBarSpawner>();
            state.RequireForUpdate<Health>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var query = state.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<HealthBarSpawner>());
            var spawner = query.GetSingleton<HealthBarSpawner>();
            foreach (var (_, entity) in SystemAPI.Query<RefRO<Health>>()
                         .WithEntityAccess().WithNone<HealthUI>().WithNone<HealthUICleanup>())
            {
                var go = Object.Instantiate(spawner.HealthBarPrefab.Value);
                var image = go.GetComponentsInChildren<Image>();
                ecb.AddComponent(entity, new HealthUI
                {
                    HealthBar = go.transform,
                    HealthSlider = image[1],
                    OpponentHeightOffset = spawner.OpponentHeightOffset,
                    PlayerTowardCameraOffset = spawner.PlayerTowardCameraOffset,
                    PlayerHeightOffset = spawner.PlayerHeightOffset,
                });
                ecb.AddComponent(entity, new HealthUICleanup
                {
                    HealthBar = go.transform,
                });
            }
            ecb.Playback(state.EntityManager);
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial struct HealthUICleanupSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (cleanup, entity) in SystemAPI.Query<RefRO<HealthUICleanup>>()
                         .WithNone<HealthUI>().WithEntityAccess())
            {
                DestroyHealthBar(cleanup.ValueRO);
                ecb.RemoveComponent<HealthUICleanup>(entity);
            }
            ecb.Playback(state.EntityManager);
        }

        public void OnDestroy(ref SystemState state)
        {
            foreach (var cleanup in SystemAPI.Query<RefRO<HealthUICleanup>>())
                DestroyHealthBar(cleanup.ValueRO);
        }

        static void DestroyHealthBar(in HealthUICleanup cleanup)
        {
            var t = cleanup.HealthBar.Value;
            if (t != null)
                Object.Destroy(t.gameObject);
        }
    }
}
