using Unity.Entities;
using Unity.Entities.Content;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace ContentManagement.Sample
{
    [UpdateBefore(typeof(LoadingRemoteCatalogSystem))]
    public partial class UpdateLoadingBarSystem : SystemBase
    {
        private ProgressBar m_ProgressBar;
        private bool isInitialized;
        
        protected override void OnCreate()
        {
            isInitialized = false;
            RequireForUpdate<HighLowWeakScene>();
        }
        
        private void UpdateStateCallback(ContentDeliveryGlobalState.ContentUpdateState contentUpdateState)
        {
            if (contentUpdateState >= ContentDeliveryGlobalState.ContentUpdateState.ContentReady)
            {
                // Track the state of your content and set when your content is ready to use
                m_ProgressBar.style.display = DisplayStyle.None;
            }
        }

        protected override void OnUpdate()
        {
            var progressBarUI = Object.FindAnyObjectByType<LoadingBarUI>();
            if (progressBarUI == null)
                return;
           
            if (!isInitialized)
            {
                isInitialized = true;
                m_ProgressBar = progressBarUI.ProgressBar;
                m_ProgressBar.style.display = DisplayStyle.Flex;
                ContentDeliveryGlobalState.RegisterForContentUpdateCompletion(UpdateStateCallback);
            }

            // Displays the loading bar used during the following loading and download steps:
            // - DownloadingCatalogInfo
            // - DownloadingCatalogs
            // - DownloadingLocalCatalogs
            // - DownloadingContentSet
            if (ContentDeliveryGlobalState.CurrentContentUpdateState < ContentDeliveryGlobalState.ContentUpdateState.ContentReady)
            {
                if (ContentDeliveryGlobalState.DeliveryService != null && ContentDeliveryGlobalState.DeliveryService.DownloadServices != null)
                {
                    foreach (var downloadService in ContentDeliveryGlobalState.DeliveryService.DownloadServices)
                    {
                        if(downloadService.TotalBytes <= 0)
                            continue;
                    
                        float progress = (float) downloadService.TotalDownloadedBytes / downloadService.TotalBytes; 
                        float normalizedProgress = math.max(0f, math.min(1f, progress));
                        m_ProgressBar.value = normalizedProgress;
                        m_ProgressBar.title = $"[{ContentDeliveryGlobalState.CurrentContentUpdateState}]: {downloadService.Name} - {normalizedProgress*100}% ";
                    }    
                }
            }
        }
    }
}