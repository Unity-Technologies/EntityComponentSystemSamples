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
        public float3 Offset;
        
        public void Dispose()
        {
            //The Healbar is disposed by the client in two cases:
            //- By the DespawnHealthBarSystem (if the healt < 0)
            //- When a character is respawn (so the ghost get destroyed). Being the HealthUI this method is called in that case.
            if (HealthBar != null)
                Object.Destroy(HealthBar.gameObject);
        }

        public object Clone()
        {
            if (HealthBar == null || HealthBar.gameObject == null)
                return new HealthUI();
            var newHealtbar = Object.Instantiate(HealthBar.gameObject);
            var images = HealthBar.gameObject.GetComponentsInChildren<Image>();
            return new HealthUI
            {
                HealthBar = newHealtbar.GetComponent<Transform>(),
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
                    Offset = spawner.Offset,
                });
            }
            ecb.Playback(state.EntityManager);
        }
    }
#endif
}
