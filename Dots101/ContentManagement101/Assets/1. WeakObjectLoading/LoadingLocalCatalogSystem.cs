using Unity.Entities;
using Unity.Entities.Content;

namespace ContentManagement.Sample
{
    [UpdateBefore(typeof(WeakSceneLoadingSystem))]
    [UpdateBefore(typeof(WeakObjectLoadingSystem))]
    public partial struct LoadingLocalCatalogSystem : ISystem
    {
        private bool initialized;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LocalContent>();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            if (!initialized)
            {
                initialized = true;
                UnityEngine.Debug.Log($"<color=green>Loading Content Delivery From local source</color>");
                // When scriptable define ENABLE_CONTENT_DELIVERY is set,  
                // we must initialize the content catalog before loading assets.
                // We pass nulls in this case because the WeakObject sample doesn't use any remote catalog, 
                // instead RuntimeContentSystem will automatically use the content from StreamingAssets folder packed in the 
                // Binary build. (e.g. ContentManagementSample_Data/StreamingAssets/ for windows Standalone)
                RuntimeContentSystem.LoadContentCatalog(null, null, null, true);
                state.EntityManager.CreateEntity(typeof(ContentIsReady));
            }
        }
    }
}