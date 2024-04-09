using Unity.Burst;
using Unity.Entities;
using Unity.Scenes;

namespace Baking.PrefabReference
{
    public partial struct LoadPrefabSystem : ISystem
    {
        [BurstCompile]
         public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Enabled = false;

            var config = SystemAPI.GetSingleton<Config>();
            var configEntity = SystemAPI.GetSingletonEntity<Config>();

            // Adding the RequestEntityPrefabLoaded component will request the prefab to be loaded.
            // It will load the entity scene file corresponding to the prefab and add a PrefabLoadResult
            // component to the entity. The PrefabLoadResult component contains the entity you can use to
            // instantiate the prefab (see the PrefabReferenceSpawnerSystem system).
            state.EntityManager.AddComponentData(configEntity, new RequestEntityPrefabLoaded
            {
                Prefab = config.PrefabReference
            });
        }
    }
}
