using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Miscellaneous.AnimationWithGameObjects
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial struct SpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Execute.AnimationWithGameObjects>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<WarriorGOPrefab>().Build();
            var entities = query.ToEntityArray(Allocator.Temp);

            foreach (var entity in entities)
            {
                var warriorGOPrefab = state.EntityManager.GetComponentData<WarriorGOPrefab>(entity);
                var instance = GameObject.Instantiate(warriorGOPrefab.Prefab);
                instance.hideFlags |= HideFlags.HideAndDontSave;
                state.EntityManager.AddComponentObject(entity, instance.GetComponent<Transform>());
                state.EntityManager.AddComponentObject(entity, instance.GetComponent<Animator>());
                state.EntityManager.AddComponentData(entity, new WarriorGOInstance { Instance = instance });
                state.EntityManager.RemoveComponent<WarriorGOPrefab>(entity);
            }
        }
    }
}
