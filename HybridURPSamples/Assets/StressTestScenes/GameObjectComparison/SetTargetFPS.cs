using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SetTargetFPS : MonoBehaviour
{
    public int VsyncCount = 0;
    public int TargetFPS = 240;

    // Start is called before the first frame update
    void Start()
    {
        var os = SystemInfo.operatingSystemFamily;
        var gfx = SystemInfo.graphicsDeviceType;
        bool isMobile = Application.isMobilePlatform;

        Debug.Log($"SetTargetFPS platform {os} {gfx} isMobile: {isMobile}, FPS: {TargetFPS}, Vsync: {VsyncCount}");

        QualitySettings.vSyncCount = VsyncCount;
        Application.targetFrameRate = TargetFPS;
    }
}
