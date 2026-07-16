using Unity.Entities;
using UnityEngine;

public partial struct SpawnAnimatedCubeSystem : ISystem
{
    EntityQuery m_AnimatorRefComponentQuery;

    public void OnCreate(ref SystemState state)
    {
        m_AnimatorRefComponentQuery = SystemAPI.QueryBuilder()
            .WithAll<AnimatorRefComponent>()
            .WithNone<Animator>()
            .Build();
        state.RequireForUpdate(m_AnimatorRefComponentQuery);
    }

    public void OnUpdate(ref SystemState state)
    {
        var entities = SystemAPI.QueryBuilder()
            .WithAll<AnimatorRefComponent>()
            .WithNone<Animator>()
            .Build()
            .ToEntityArray(state.WorldUpdateAllocator);

        foreach (var entity in entities)
        {
            //Get the animator reference
            var animRef = SystemAPI.GetComponent<AnimatorRefComponent>(entity);

            //Instantiate the GO
            var rotatingCube = (GameObject)Object.Instantiate(animRef.AnimatorAsGO);

            //Add the animator to the entity
            state.EntityManager.AddComponentObject(entity, rotatingCube.GetComponent<Animator>());
        }
    }
}
