using System.IO;
using Unity.Entities;
using Unity.Entities.Content;
using UnityEngine;

namespace ContentManagement.Sample
{
    /// <summary>
    /// Enables the Content Management API to retrieve content from a remote source,
    /// load it into memory, and then connect all references.
    /// </summary>
    [UpdateBefore(typeof(WeakSceneLoadingSystem))]
    public partial struct LoadingRemoteCatalogSystem : ISystem
    {
        private bool initialized;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RemoteContent>();
            state.RequireForUpdate<HighLowWeakScene>();
            initialized = false;
            ContentDeliveryGlobalState.RegisterForContentUpdateCompletion(UpdateStateCallback);
        }
        
        private void UpdateStateCallback(ContentDeliveryGlobalState.ContentUpdateState contentUpdateState)
        {
            Debug.Log($"<color=green>Content Delivery Global State:</color> {contentUpdateState}");
            if (contentUpdateState >= ContentDeliveryGlobalState.ContentUpdateState.ContentReady)
            {
                // Track the state of your content and set when your content is ready to use
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            if (!initialized)
            {
                var settings = SystemAPI.GetSingleton<RemoteContent>();
                initialized = true;
                var contentPath = Path.Combine( settings.URL.ToString(), WeakSceneListScriptableObject.ContentDir) + "/";

                Debug.Log($"<color=green>Loading Content Delivery From Remote source:</color>{contentPath}");
                // When the ENABLE_CONTENT_DELIVERY scriptable define is set (https://docs.unity3d.com/6000.2/Documentation/Manual/custom-scripting-symbols.html),
                // the content catalog must be initialized before any assets are loaded.
                // We specify the remote content path, the local cache path (for storing downloaded content),
                // and prevent unnecessary downloads by only fetching content if needed.
                // The content set name refers to the specific content bundle defined during the build process.
                RuntimeContentSystem.LoadContentCatalog(contentPath, WeakSceneListScriptableObject.CachePath,
                    WeakSceneListScriptableObject.ContentSetName, true);
            }

            
            var entityQuery = SystemAPI.QueryBuilder().WithAll<ContentIsReady>().Build();
            
            if (entityQuery.CalculateEntityCount() < 1 &&
                ContentDeliveryGlobalState.CurrentContentUpdateState >=
                ContentDeliveryGlobalState.ContentUpdateState.ContentReady)
            {
                Debug.Log($"Content Delivery is <color=green>Ready From Remote</color> source");
                state.EntityManager.CreateEntity(typeof(ContentIsReady));
            }
        }
    }
}