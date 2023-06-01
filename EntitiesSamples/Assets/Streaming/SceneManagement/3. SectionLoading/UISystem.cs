using Streaming.SceneManagement.SceneState;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using SceneReference = Unity.Entities.SceneReference;

namespace Streaming.SceneManagement.SectionLoading
{
    public partial struct UISystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SceneReference>();
        }

        public void OnUpdate(ref SystemState state)
        {
            if (SectionUI.Singleton == null)
            {
                return;
            }
            var ui = SectionUI.Singleton;

            var sectionQuery = SystemAPI.QueryBuilder().WithAll<ResolvedSectionEntity>().Build();
            var entities = sectionQuery.ToEntityArray(Allocator.Temp);
            if (entities.Length <= 0 || !state.EntityManager.HasBuffer<ResolvedSectionEntity>(entities[0]))
            {
                return;
            }

            var sectionEntities = state.EntityManager.GetBuffer<ResolvedSectionEntity>(entities[0]);
            ui.CreateRows(sectionEntities.Length);

            // Update the information for each scene UI
            for (int index = 0; index < sectionEntities.Length; ++index)
            {
                var entity = sectionEntities[index].SectionEntity;
                var sectionState = SceneSystem.GetSectionStreamingState(state.WorldUnmanaged, entity);
                bool disabled = state.EntityManager.HasComponent<DisableSceneResolveAndLoad>(entity);
                ui.UpdateRow(index, disabled, sectionState);
            }

            if (ui.GetAction(out var rowIndex))
            {
                var entity = sectionEntities[rowIndex].SectionEntity;
                var sectionState = SceneSystem.GetSectionStreamingState(state.WorldUnmanaged, entity);
                if (sectionState != SceneSystem.SectionStreamingState.Unloaded &&
                    sectionState != SceneSystem.SectionStreamingState.UnloadRequested)
                {
                    state.EntityManager.RemoveComponent<RequestSceneLoaded>(entity);
                }
                else
                {
                    state.EntityManager.AddComponent<RequestSceneLoaded>(entity);
                }
            }
        }
    }
}
