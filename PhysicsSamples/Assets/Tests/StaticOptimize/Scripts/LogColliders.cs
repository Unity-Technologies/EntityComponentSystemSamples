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
            AddComponent<ActivateColliderLogging>();
        }
    }
}

[RequireMatchingQueriesForUpdate]
partial class LogCollidersSystem : SystemBase
{
    EntityQuery _ActivateColliderLoggingQuery;
    EntityQuery _ColliderQuery;
    static int _PreviousCount;

    protected override void OnCreate()
    {
        _ColliderQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<PhysicsCollider>()
            .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities)
            .Build(this);

        _PreviousCount = 0;

        RequireForUpdate<ActivateColliderLogging>();
    }

    protected override void OnUpdate()
    {
        var newCount = _ColliderQuery.CalculateEntityCount();
        if (newCount == _PreviousCount)
            return;

        _PreviousCount = newCount;

        var entities = _ColliderQuery.ToEntityArray(Allocator.Temp);
        string str = EntityManager.GetName(entities[0]);
        for (int i = 1; i < entities.Length; ++i)
            str += $", {EntityManager.GetName(entities[i])}";

        Debug.Log($"Found {entities.Length} colliders: {str}");
    }
}
