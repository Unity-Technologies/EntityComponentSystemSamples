using System.Collections.Generic;
using Unity.Physics.Authoring;
using UnityEditor;
using UnityEngine;

namespace Unity.Physics.Editor
{
    [CustomPropertyDrawer(typeof(PhysicsMaterialProperties))]
    class PhysicsMaterialPropertiesDrawer : BaseDrawer
    {
        static class Content
        {
            public static readonly GUIContent AdvancedGroupFoldout = EditorGUIUtility.TrTextContent("Advanced");
            public static readonly GUIContent BelongsToLabel = EditorGUIUtility.TrTextContent(
                "Belongs To",
                "Specifies the categories to which this object belongs."
            );
            public static readonly GUIContent CollidesWithLabel = EditorGUIUtility.TrTextContent(
                "Collides With",
                "Specifies the categories of objects with which this object will collide, " +
                "or with which it will raise events if intersecting a trigger."
            );
            public static readonly GUIContent CollisionFilterGroupFoldout =
                EditorGUIUtility.TrTextContent("Collision Filter");
            public static readonly GUIContent CustomFlagsLabel =
                EditorGUIUtility.TrTextContent("Custom Tags", "Specify custom tags to read at run-time.");
            public static readonly GUIContent FrictionLabel = EditorGUIUtility.TrTextContent(
                "Friction",
                "Specifies how resistant the body is to motion when sliding along other surfaces, " +
                "as well as what value should be used when colliding with an object that has a different value."
            );
            public static readonly GUIContent RestitutionLabel = EditorGUIUtility.TrTextContent(
                "Restitution",
                "Specifies how bouncy the object will be when colliding with other surfaces, " +
                "as well as what value should be used when colliding with an object that has a different value."
            );
            public static readonly GUIContent CollisionResponseLabel = EditorGUIUtility.TrTextContent(
                "Collision Response",
                "Specifies whether the shape should collide normally, raise trigger events when intersecting other shapes, " +
                "collide normally and raise notifications of collision events with other shapes, " +
                "or completely ignore collisions (but still move and intercept queries)."
            );

            public static readonly GUIContent DetailedStaticMeshCollisionLabel = EditorGUIUtility.TrTextContent(
               "Detailed Static Mesh Collision",
               "When enabled, this option processes contact detection for dynamic objects colliding with static colliders " +
               "across both the current and next frame. This helps predict and refine collision accuracy, reducing the " +
               "likelihood of ghost collisions. Disable this if detailed precision is not required, as it may impact performance."
           );
        }

        const string k_CollisionFilterGroupKey = "m_BelongsToCategories";
        const string k_AdvancedGroupKey = "m_CustomMaterialTags";

        Dictionary<string, SerializedObject> m_SerializedTemplates = new Dictionary<string, SerializedObject>();

        SerializedProperty GetTemplateValueProperty(SerializedProperty property)
        {
            var key = property.propertyPath;
            var template = property.FindPropertyRelative("m_Template").objectReferenceValue;
            SerializedObject serializedTemplate;
            if (
                !m_SerializedTemplates.TryGetValue(key, out serializedTemplate)
                || serializedTemplate?.targetObject != template
            )
                m_SerializedTemplates[key] = serializedTemplate = template == null ? null : new SerializedObject(template);
            serializedTemplate?.Update();
            return serializedTemplate?.FindProperty("m_Value");
        }

        void FindToggleAndValueProperties(
            SerializedProperty property, SerializedProperty templateValueProperty, string relativePath,
            out SerializedProperty toggle, out SerializedProperty value
        )
        {
            var relative = property.FindPropertyRelative(relativePath);
            toggle = relative.FindPropertyRelative("m_Override");
            value = toggle.boolValue || templateValueProperty == null
                ? relative.FindPropertyRelative("m_Value")
                : templateValueProperty.FindPropertyRelative(relativePath).FindPropertyRelative("m_Value");
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var templateValueProperty = GetTemplateValueProperty(property);

            // m_CollisionResponse, collision filter foldout, advanced foldout
            var height = 3f * EditorGUIUtility.singleLineHeight + 2f * EditorGUIUtility.standardVerticalSpacing;

            // m_BelongsTo, m_CollidesWith
            var group = property.FindPropertyRelative(k_CollisionFilterGroupKey);
            if (group.isExpanded)
                height += 2f * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

            // m_CustomTags
            group = property.FindPropertyRelative(k_AdvancedGroupKey);
            if (group.isExpanded)
                height += 2f * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

            // m_Template
            if (property.FindPropertyRelative("m_SupportsTemplate").boolValue)
                height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            // m_Friction, m_Restitution
            FindToggleAndValueProperties(property, templateValueProperty, "m_CollisionResponse", out _, out var collisionResponse);
            // Check if regular collider
            CollisionResponsePolicy collisionResponseEnum = (CollisionResponsePolicy)collisionResponse.intValue;
            if (collisionResponseEnum == CollisionResponsePolicy.Collide ||
                collisionResponseEnum == CollisionResponsePolicy.CollideRaiseCollisionEvents)
                height += 2f * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

            return height;
        }

        protected override bool IsCompatible(SerializedProperty property) => true;

        static void DisplayOverridableProperty(
            Rect position, GUIContent label, SerializedProperty toggle, SerializedProperty value, bool templateAssigned
        )
        {
            if (templateAssigned)
            {
                var labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth -= 16f + EditorGUIUtility.standardVerticalSpacing;
                var togglePosition = new Rect(position) { width = EditorGUIUtility.labelWidth + 16f + EditorGUIUtility.standardVerticalSpacing };
                EditorGUI.PropertyField(togglePosition, toggle, label);
                EditorGUIUtility.labelWidth = labelWidth;

                EditorGUI.BeginDisabledGroup(!toggle.boolValue);
                var indent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                EditorGUI.PropertyField(new Rect(position) { xMin = togglePosition.xMax }, value, GUIContent.none, true);
                EditorGUI.indentLevel = indent;
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUI.PropertyField(position, value,  label, true);
            }
        }

        protected override void DoGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var template = property.FindPropertyRelative("m_Template");
            var templateAssigned = template.objectReferenceValue != null;
            var supportsTemplate = property.FindPropertyRelative("m_SupportsTemplate");
            if (supportsTemplate.boolValue)
            {
                position.height = EditorGUI.GetPropertyHeight(template);
                EditorGUI.PropertyField(position, template);

                position.y = position.yMax + EditorGUIUtility.standardVerticalSpacing;
            }

            var templateValue = GetTemplateValueProperty(property);

            FindToggleAndValueProperties(property, templateValue, "m_CollisionResponse", out var collisionResponseDropDown, out var collisionResponse);
            position.height = EditorGUIUtility.singleLineHeight;
            DisplayOverridableProperty(position, Content.CollisionResponseLabel, collisionResponseDropDown, collisionResponse, templateAssigned);

            SerializedProperty toggle;

            // Check if regular collider
            CollisionResponsePolicy collisionResponseEnum = (CollisionResponsePolicy)collisionResponse.intValue;
            if (collisionResponseEnum == CollisionResponsePolicy.Collide ||
                collisionResponseEnum == CollisionResponsePolicy.CollideRaiseCollisionEvents)
            {
                FindToggleAndValueProperties(property, templateValue, "m_Friction", out toggle, out var friction);
                position.y = position.yMax + EditorGUIUtility.standardVerticalSpacing;
                position.height = EditorGUIUtility.singleLineHeight;
                DisplayOverridableProperty(position, Content.FrictionLabel, toggle, friction, templateAssigned);

                FindToggleAndValueProperties(property, templateValue, "m_Restitution", out toggle, out var restitution);
                position.y = position.yMax + EditorGUIUtility.standardVerticalSpacing;
                position.height = EditorGUIUtility.singleLineHeight;
                DisplayOverridableProperty(position, Content.RestitutionLabel, toggle, restitution, templateAssigned);
            }

            // collision filter group
            var collisionFilterGroup = property.FindPropertyRelative(k_CollisionFilterGroupKey);
            position.y = position.yMax + EditorGUIUtility.standardVerticalSpacing;
            position.height = EditorGUIUtility.singleLineHeight;
            collisionFilterGroup.isExpanded =
                EditorGUI.Foldout(position, collisionFilterGroup.isExpanded, Content.CollisionFilterGroupFoldout, true);
            if (collisionFilterGroup.isExpanded)
            {
                ++EditorGUI.indentLevel;

                FindToggleAndValueProperties(property, templateValue, "m_BelongsToCategories", out toggle, out var belongsTo);
                position.y = position.yMax + EditorGUIUtility.standardVerticalSpacing;
                position.height = EditorGUIUtility.singleLineHeight;
                DisplayOverridableProperty(position, Content.BelongsToLabel, toggle, belongsTo, templateAssigned);

                FindToggleAndValueProperties(property, templateValue, "m_CollidesWithCategories", out toggle, out var collidesWith);
                position.y = position.yMax + EditorGUIUtility.standardVerticalSpacing;
                position.height = EditorGUIUtility.singleLineHeight;
                DisplayOverridableProperty(position, Content.CollidesWithLabel, toggle, collidesWith, templateAssigned);

                --EditorGUI.indentLevel;
            }

            // advanced group
            var advancedGroup = property.FindPropertyRelative(k_AdvancedGroupKey);
            position.y = position.yMax + EditorGUIUtility.standardVerticalSpacing;
            position.height = EditorGUIUtility.singleLineHeight;
            advancedGroup.isExpanded =
                EditorGUI.Foldout(position, advancedGroup.isExpanded, Content.AdvancedGroupFoldout, true);
            if (advancedGroup.isExpanded)
            {
                ++EditorGUI.indentLevel;

                FindToggleAndValueProperties(property, templateValue, "m_DetailedStaticMeshCollision", out toggle, out var detailedStaticMeshCollision);
                position.y = position.yMax + EditorGUIUtility.standardVerticalSpacing;
                position.height = EditorGUIUtility.singleLineHeight;
                DisplayOverridableProperty(position, Content.DetailedStaticMeshCollisionLabel, toggle, detailedStaticMeshCollision, templateAssigned);

                FindToggleAndValueProperties(property, templateValue, "m_CustomMaterialTags", out toggle, out var customFlags);
                position.y = position.yMax + EditorGUIUtility.standardVerticalSpacing;
                position.height = EditorGUIUtility.singleLineHeight;
                DisplayOverridableProperty(position, Content.CustomFlagsLabel, toggle, customFlags, templateAssigned);

                --EditorGUI.indentLevel;
            }
        }
    }
}
