using Unity.Entities;
using Unity.Entities.Content;

namespace ContentManagement.Sample
{
    [UpdateBefore(typeof(WeakSceneLoadingSystem))]
    public partial struct WeakSceneLoadingLocalCatalogSystem : ISystem
    {
        private bool initialized;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LocalContent>();
            state.RequireForUpdate<HighLowWeakScene>();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            if (!initialized)
            {
                initialized = true;
                UnityEngine.Debug.Log($"<color=green>Loading Content Delivery From local source</color>");
                // When scriptable define ENABLE_CONTENT_DELIVERY is set, (https://docs.unity3d.com/6000.2/Documentation/Manual/custom-scripting-symbols.html),  
                // we must initialize the content catalog before loading assets.
                // We pass nulls in this case because the WeakScene sample doesn't use any catalog.
                RuntimeContentSystem.LoadContentCatalog(null, null, null, true);
                state.EntityManager.CreateEntity(typeof(ContentIsReady));
            }
        }
    }
}