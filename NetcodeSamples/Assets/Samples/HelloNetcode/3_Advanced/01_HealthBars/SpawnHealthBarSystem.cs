using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Samples.HelloNetcode
{
#if !UNITY_DISABLE_MANAGED_COMPONENTS
    public class HealthUI : IComponentData, IDisposable, ICloneable
    {
        public Transform HealthBar;
        public Image HealthSlider;
        public float OpponentHeightOffset;
        public float PlayerHeightOffset;
        public float PlayerTowardCameraOffset;

        public void Dispose()
        {
            // As this is IDisposable, we can trigger the destruction of the HealthBar when this ghost entity is destroyed.
            if (HealthBar != null)
                Object.Destroy(HealthBar.gameObject);
        }

        public object Clone()
        {
            if (HealthBar == null || HealthBar.gameObject == null)
                return new HealthUI();
            var newHealthBar = Object.Instantiate(HealthBar.gameObject);
            var images = HealthBar.gameObject.GetComponentsInChildren<Image>();
            return new HealthUI
            {
                HealthBar = newHealthBar.GetComponent<Transform>(),
                HealthSlider = images[1]
            };
        }
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
                         .WithEntityAccess().WithNone<HealthUI>())
            {
                var go = Object.Instantiate(spawner.HealthBarPrefab);
                var image = go.GetComponentsInChildren<Image>();
                ecb.AddComponent(entity, new HealthUI
                {
                    HealthBar = go.transform,
                    HealthSlider = image[1],
                    OpponentHeightOffset = spawner.OpponentHeightOffset,
                    PlayerTowardCameraOffset = spawner.PlayerTowardCameraOffset,
                    PlayerHeightOffset = spawner.PlayerHeightOffset,
                });
            }
            ecb.Playback(state.EntityManager);
        }
    }
#endif
}
