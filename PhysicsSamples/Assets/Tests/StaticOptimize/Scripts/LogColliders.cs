using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

struct ActivateColliderLogging : IComponentData {}

public class LogColliders : MonoBehaviour
{
    class LogColliderBaker : Baker<LogColliders>
    {
        public override void Bake(LogColliders authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<ActivateColliderLogging>(entity);
        }
    }
}

[RequireMatchingQueriesForUpdate]
partial struct LogCollidersSystem : ISystem
{
    EntityQuery _ActivateColliderLoggingQuery;
    EntityQuery _ColliderQuery;
    static int _PreviousCount;

    public void OnCreate(ref SystemState state)
    {
        _ColliderQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<PhysicsCollider>()
            .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
            .Build(ref state);

        _PreviousCount = 0;

        state.RequireForUpdate<ActivateColliderLogging>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var newCount = _ColliderQuery.CalculateEntityCount();
        if (newCount == _PreviousCount)
            return;

        _PreviousCount = newCount;

        var entities = _ColliderQuery.ToEntityArray(Allocator.Temp);
        string str = state.EntityManager.GetName(entities[0]);
        for (int i = 1; i < entities.Length; ++i)
            str += $", {state.EntityManager.GetName(entities[i])}";

        Debug.Log($"Found {entities.Length} colliders: {str}");
    }
}
