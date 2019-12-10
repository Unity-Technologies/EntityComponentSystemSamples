using UnityEditor;

#if UNITY_EDITOR
public class RotationSpeedSetting : Unity.Build.IBuildSettingsComponent
{
    public float RotationSpeed;
    public float Offset;

    public string Name => "RotationSpeedSetting";

    public bool OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        RotationSpeed = EditorGUILayout.FloatField("RotationSpeed", RotationSpeed);
        Offset = EditorGUILayout.FloatField("Offset", Offset);
        return EditorGUI.EndChangeCheck();
    }
}
#endif