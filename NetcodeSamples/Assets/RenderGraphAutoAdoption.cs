#if UNITY_6000_0_OR_NEWER && UNITY_EDITOR && !URP_COMPATIBILITY_MODE

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class RenderGraphAutoAdoption : IPreprocessBuildWithReport
{
    public int callbackOrder => int.MinValue + 99; // just before URPPreprocessBuild

    void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
    {
        if (GraphicsSettings.currentRenderPipelineAssetType != typeof(UniversalRenderPipelineAsset))
            return;

        //changing the boolean through serialization as it is private
        var settings = GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();
        var global = GraphicsSettings.GetSettingsForRenderPipeline<UniversalRenderPipeline>();
        var so = new SerializedObject(global);
        SerializedProperty settingsProperty = null;

        //finding back RenderGraphSettings in global settings serilized object
        var propertyIterator = so.FindProperty("m_Settings.m_SettingsList.m_List"); //start from the root of settings collection
        var end = propertyIterator.GetEndProperty();
        propertyIterator.NextVisible(true); //enter collection
        while (!SerializedProperty.EqualContents(propertyIterator, end))
        {
            if (propertyIterator?.boxedValue == settings)
            {
                settingsProperty = propertyIterator;
                break;
            }
            propertyIterator.NextVisible(false);
        }
        if (settingsProperty == null)
            throw new BuildFailedException("Missing RenderGraphSettings in UniversalRenderPipeline's IRenderPipelineGraphicsSettings");

        //update to use RenderGraph
        var flag = settingsProperty.FindPropertyRelative("m_EnableRenderCompatibilityMode");
        if (!flag.boolValue)
            return;

        flag.boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssetIfDirty(global);
    }
}

#endif