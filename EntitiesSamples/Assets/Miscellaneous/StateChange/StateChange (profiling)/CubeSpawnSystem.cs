using System;
using Miscellaneous.Execute;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace Miscellaneous.StateChange
{
    public partial struct CubeSpawnSystem : ISystem
    {
        Config m_CurrentState;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<StateChangeProfiling>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var config = SystemAPI.GetSingleton<Config>();
            if (config.Equals(m_CurrentState)) return;
            m_CurrentState = config;

            var colorQuery = SystemAPI.QueryBuilder().WithAll<URPMaterialPropertyBaseColor>().Build();
            state.EntityManager.DestroyEntity(colorQuery);

            var prefab = state.EntityManager.Instantiate(config.Prefab);
            state.EntityManager.AddComponent<Prefab>(prefab);

            switch (config.UpdateType)
            {
                case Config.UpdateTypeEnum.ValueBranching:
                    state.EntityManager.AddComponent<SetStateValueChangeSystem.State>(prefab);
                    break;
                case Config.UpdateTypeEnum.StructuralChange:
                    state.EntityManager.AddComponent<SetStateStructuralChangeSystem.State>(prefab);
                    state.EntityManager.AddComponent<SetStateStructuralChangeSystem.StateEnabled>(prefab);
                    break;
                case Config.UpdateTypeEnum.Enableable:
                    state.EntityManager.AddComponent<SetStateEnableableSystem.State>(prefab);
                    state.EntityManager.AddComponent<SetStateEnableableSystem.StateEnabled>(prefab);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var instances = state.EntityManager.Instantiate(prefab, (int)(config.Size * config.Size), Allocator.Temp);
            var center = (config.Size - 1) / 2f;

            for (int i = 0; i < instances.Length; i++)
            {
                var position = float3.zero;
                position.x = (i % config.Size - center) * 1.5f;
                position.z = (i / config.Size - center) * 1.5f;
                SystemAPI.SetComponent(instances[i], new LocalTransform { Position = position, Scale = 1 });
            }

            state.EntityManager.DestroyEntity(prefab);
        }
    }
}
