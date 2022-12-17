using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Samples.HelloNetcode
{
#if !UNITY_DISABLE_MANAGED_COMPONENTS
    public class HealthUI : IComponentData
    {
        public Transform HealthBar;
        public Image HealthSlider;
        public float3 Offset;
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

        public void OnDestroy(ref SystemState state)
        {
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
