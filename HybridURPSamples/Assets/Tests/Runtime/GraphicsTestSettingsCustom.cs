//This script is copied from UniversalRP TestProject
// https://github.com/Unity-Technologies/ScriptableRenderPipeline/blob/master/TestProjects/UniversalGraphicsTest/Assets/Test/Runtime/UniversalGraphicsTestSettings.cs

using UnityEngine.TestTools.Graphics;

public class GraphicsTestSettingsCustom : GraphicsTestSettings
{
    public int WaitFrames = 2;

    public GraphicsTestSettingsCustom()
    {
        ImageComparisonSettings.TargetWidth = 1024;
        ImageComparisonSettings.TargetHeight = 576;
        ImageComparisonSettings.AverageCorrectnessThreshold = 0.0015f;
        ImageComparisonSettings.PerPixelCorrectnessThreshold = 0.001f;
    }
}
