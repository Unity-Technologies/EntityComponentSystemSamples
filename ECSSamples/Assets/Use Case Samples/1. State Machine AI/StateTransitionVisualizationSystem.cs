using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;

#if !UNITY_DISABLE_MANAGED_COMPONENTS
[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateBefore(typeof(RenderMeshSystemV2))]
public partial class StateTransitionVisualizationSystem : SystemBase
{
    private EntityQuery m_StateTransitionMaterialSingletonQuery;

    protected override void OnCreate()
    {
        m_StateTransitionMaterialSingletonQuery = GetEntityQuery(typeof(StateTransitionMaterials));
    }

    protected override void OnUpdate()
    {
        var materials = EntityManager.GetComponentData<StateTransitionMaterials>(
            m_StateTransitionMaterialSingletonQuery.GetSingletonEntity());

        Entities
            .WithStructuralChanges()
            .WithAll<IsInTransitionTag>()
            .ForEach((Entity e) =>
            {
                var meshRenderer = EntityManager.GetSharedComponentData<RenderMesh>(e);

                if (HasComponent<IdleTimer>(e))
                    meshRenderer.material = materials.m_IdleMaterial;
                else if (HasComponent<IsChasingTag>(e))
                    meshRenderer.material = materials.m_ChasingMaterial;
                else
                    meshRenderer.material = materials.m_PatrollingMaterial;

                EntityManager.SetSharedComponentData(e, meshRenderer);
                EntityManager.RemoveComponent<IsInTransitionTag>(e);
            }).Run();
    }
}
#endif
