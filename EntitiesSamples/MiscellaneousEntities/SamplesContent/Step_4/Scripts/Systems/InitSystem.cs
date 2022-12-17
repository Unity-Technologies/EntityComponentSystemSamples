using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace StateMachineValue
{
    [BurstCompile]
    public partial struct InitSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            var config = SystemAPI.GetSingleton<Config>();
            
            var cubes = state.EntityManager.Instantiate(config.Prefab,
                (int)(config.Size * config.Size), Allocator.Temp);
            var center = (config.Size - 1) / 2f;

            for (int i = 0; i < cubes.Length; i++)
            {
                var trans = new LocalTransform { Scale = 1 };
                trans.Position.x = (i % config.Size - center) * 1.5f;
                trans.Position.z = (i / config.Size - center) * 1.5f;
                SystemAPI.SetComponent(cubes[i], trans);
            }
        }
    }
}