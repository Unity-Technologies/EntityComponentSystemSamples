using System;
using System.Threading;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Emits periodic log messages during player builds to prevent UTR's log-idle timeout
/// from killing the editor during long-running build steps (e.g. Burst AOT compilation).
/// </summary>
class BuildHeartbeat : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    static readonly TimeSpan k_Interval = TimeSpan.FromSeconds(120);

    public int callbackOrder => 0;

    static Timer s_HeartbeatTimer;

    public void OnPreprocessBuild(BuildReport report)
    {
        s_HeartbeatTimer?.Dispose();
        s_HeartbeatTimer = new Timer(
            _ => Debug.Log("[BuildHeartbeat] Player build still in progress..."),
            null, k_Interval, k_Interval);
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        s_HeartbeatTimer?.Dispose();
        s_HeartbeatTimer = null;
    }
}
