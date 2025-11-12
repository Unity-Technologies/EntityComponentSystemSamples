using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using UnityEditor;
using UnityEngine;

namespace Unity.Physics.Editor
{
    [InitializeOnLoad]
    static class EditorGUIControls
    {
        static EditorGUIControls()
        {
            if (k_SoftSlider == null)
                Debug.LogException(new MissingMemberException("Could not find expected signature of EditorGUI.Slider() for soft slider."));
        }

        static class Styles
        {
            public static readonly string CompatibilityWarning = L10n.Tr("Not compatible with {0}.");
        }

        public static void DisplayCompatibilityWarning(Rect position, GUIContent label, string incompatibleType)
        {
            EditorGUI.HelpBox(
                EditorGUI.PrefixLabel(position, label),
                string.Format(Styles.CompatibilityWarning, incompatibleType),
                MessageType.Error
            );
        }

        static readonly MethodInfo k_SoftSlider = typeof(EditorGUI).GetMethod(
            "Slider",
            BindingFlags.Static | BindingFlags.NonPublic,
            null,
            new[]
            {
                typeof(Rect),          // position
                typeof(GUIContent),    // label
                typeof(float),         // value
                typeof(float),         // sliderMin
                typeof(float),         // sliderMax
                typeof(float),         // textFieldMin
                typeof(float)          // textFieldMax
            },
            Array.Empty<ParameterModifier>()
        );

        static readonly object[] k_SoftSliderArgs = new object[7];

        public static void SoftSlider(
            Rect position, GUIContent label, SerializedProperty property,
            float sliderMin, float sliderMax,
            float textFieldMin, float textFieldMax
        )
        {
            if (property.propertyType != SerializedPropertyType.Float)
            {
                DisplayCompatibilityWarning(position, label, property.propertyType.ToString());
            }
            else if (k_SoftSlider == null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(position, property, label);
                if (EditorGUI.EndChangeCheck())
                    property.floatValue = math.clamp(property.floatValue, textFieldMin, textFieldMax);
            }
            else
            {
                k_SoftSliderArgs[0] = position;
                k_SoftSliderArgs[1] = label;
                k_SoftSliderArgs[2] = property.floatValue;
                k_SoftSliderArgs[3] = sliderMin;
                k_SoftSliderArgs[4] = sliderMax;
                k_SoftSliderArgs[5] = textFieldMin;
                k_SoftSliderArgs[6] = textFieldMax;
                EditorGUI.BeginProperty(position, label, property);
                EditorGUI.BeginChangeCheck();
                var result = k_SoftSlider.Invoke(null, k_SoftSliderArgs);
                if (EditorGUI.EndChangeCheck())
                    property.floatValue = (float)result;
                EditorGUI.EndProperty();
            }
        }
    }
}
