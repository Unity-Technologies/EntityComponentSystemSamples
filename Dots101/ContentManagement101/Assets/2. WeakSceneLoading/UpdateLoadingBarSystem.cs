using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.UIElements;
using Unity.Entities.Content;

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

        protected override void OnUpdate()
        {
#region InitializingUI
            if (!isInitialized)
            {
                var progressBarUI = Object.FindAnyObjectByType<LoadingBarUI>();
                if (progressBarUI == null)
                    return;
                
                isInitialized = true;
                m_ProgressBar = progressBarUI.ProgressBar;
                m_ProgressBar.style.display = DisplayStyle.Flex;    
            }
#endregion

#region ReadingLoadedBytesFromContentManagementAPI
            // Displays the loading bar used during the following loading and download steps:
            // - DownloadingCatalogInfo
            // - DownloadingCatalogs (catalog.bin)
            // - DownloadingLocalCatalogs (catalog.bin from StreamingAssets)
            // - DownloadingContentSet (artifacts folders/files)
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
            else
            {
                m_ProgressBar.style.display = DisplayStyle.None;
            }
#endregion
        }
    }
}