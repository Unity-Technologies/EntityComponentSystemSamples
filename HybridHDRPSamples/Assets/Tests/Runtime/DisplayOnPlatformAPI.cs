using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class DisplayOnPlatformAPI : MonoBehaviour
{
    public bool D3D11;
    public bool D3D12;
    [FormerlySerializedAs("VukanWindows")]
    public bool VulkanWindows;
    public bool VukanWindows
    {
        get { return VulkanWindows; }
        set { VulkanWindows = value; }
    }
    public bool Metal;
    public bool VulkanLinux;

    List<PlatformAPI> platformApis = new List<PlatformAPI>();

    void OnValidate()
    {
        platformApis.Clear();

        if (D3D11)
            platformApis.Add(new PlatformAPI(RuntimePlatform.WindowsEditor, GraphicsDeviceType.Direct3D11));

        if (D3D12)
            platformApis.Add(new PlatformAPI(RuntimePlatform.WindowsEditor, GraphicsDeviceType.Direct3D12));

        if (VulkanWindows)
            platformApis.Add(new PlatformAPI(RuntimePlatform.WindowsEditor, GraphicsDeviceType.Vulkan));

        if (Metal)
            platformApis.Add(new PlatformAPI(RuntimePlatform.OSXEditor, GraphicsDeviceType.Metal));

        if (VulkanLinux)
            platformApis.Add(new PlatformAPI(RuntimePlatform.LinuxEditor, GraphicsDeviceType.Vulkan));

        bool display = false;

        foreach (var platformApi in platformApis)
        {
            if (Application.platform == platformApi.platform && SystemInfo.graphicsDeviceType == platformApi.graphicsDeviceType)
            {
                display = true;
                break;
            }
        }

        var textMeshRenderer = gameObject.GetComponent<MeshRenderer>();
        textMeshRenderer.enabled = display;
    }

    public struct PlatformAPI
    {
        public PlatformAPI(RuntimePlatform inPlatform, GraphicsDeviceType inGraphicsDeviceType)
        {
            platform = inPlatform;
            graphicsDeviceType = inGraphicsDeviceType;
        }

        public RuntimePlatform platform;
        public GraphicsDeviceType graphicsDeviceType;
    }
}
