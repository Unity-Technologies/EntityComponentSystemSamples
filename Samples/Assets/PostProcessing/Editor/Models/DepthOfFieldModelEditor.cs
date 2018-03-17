using UnityEngine.PostProcessing;

namespace UnityEditor.PostProcessing
{
    [PostProcessingModelEditor(typeof(DepthOfFieldModel))]
    public class DepthOfFieldModelEditor : PostProcessingModelEditor
    {
        public override void OnEnable()
        {
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Work in progress.", MessageType.Warning);
        }
    }
}
