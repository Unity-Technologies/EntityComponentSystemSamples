using UnityEngine.PostProcessing;

namespace UnityEditor.PostProcessing
{
    using Settings = GrainModel.Settings;

    [PostProcessingModelEditor(typeof(GrainModel))]
    public class GrainModelEditor : PostProcessingModelEditor
    {
        SerializedProperty m_Mode;
        SerializedProperty m_Amount;
        SerializedProperty m_Size;
        SerializedProperty m_LuminanceContribution;

        public override void OnEnable()
        {
            m_Mode = FindSetting((Settings x) => x.mode);
            m_Amount = FindSetting((Settings x) => x.intensity);
            m_Size = FindSetting((Settings x) => x.size);
            m_LuminanceContribution = FindSetting((Settings x) => x.luminanceContribution);
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(m_Mode);
            EditorGUILayout.PropertyField(m_Amount);
            EditorGUILayout.PropertyField(m_LuminanceContribution);

            if (m_Mode.intValue == (int)GrainModel.Mode.Filmic)
                EditorGUILayout.PropertyField(m_Size);
        }
    }
}
