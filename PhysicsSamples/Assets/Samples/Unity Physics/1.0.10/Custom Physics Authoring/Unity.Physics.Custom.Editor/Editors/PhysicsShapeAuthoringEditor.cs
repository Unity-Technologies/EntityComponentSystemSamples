using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using Unity.Physics.Extensions;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Physics.Editor
{
    [CustomEditor(typeof(PhysicsShapeAuthoring))]
    [CanEditMultipleObjects]
    class PhysicsShapeAuthoringEditor : BaseEditor
    {
        static class Styles
        {
            const string k_Plural = "One or more selected objects";
            const string k_Singular = "This object";

            public static readonly string GenericUndoMessage = L10n.Tr("Change Shape");
            public static readonly string MultipleShapeTypesLabel =
                L10n.Tr("Multiple shape types in current selection.");
            public static readonly string PreviewGenerationNotification =
                L10n.Tr("Generating collision geometry preview...");

            static readonly GUIContent k_FitToRenderMeshesLabel =
                EditorGUIUtility.TrTextContent("Fit to Enabled Render Meshes");
            static readonly GUIContent k_FitToRenderMeshesWarningLabelSg = new GUIContent(
                k_FitToRenderMeshesLabel.text,
                EditorGUIUtility.Load("console.warnicon") as Texture,
                L10n.Tr($"{k_Singular} has non-uniform scale. Trying to fit the shape to render meshes might produce unexpected results.")
            );
            static readonly GUIContent k_FitToRenderMeshesWarningLabelPl = new GUIContent(
                k_FitToRenderMeshesLabel.text,
                EditorGUIUtility.Load("console.warnicon") as Texture,
                L10n.Tr($"{k_Plural} has non-uniform scale. Trying to fit the shape to render meshes might produce unexpected results.")
            );
            public static readonly GUIContent CenterLabel = EditorGUIUtility.TrTextContent("Center");
            public static readonly GUIContent SizeLabel = EditorGUIUtility.TrTextContent("Size");
            public static readonly GUIContent OrientationLabel = EditorGUIUtility.TrTextContent(
                "Orientation", "Euler orientation in the shape's local space (ZXY order)."
            );
            public static readonly GUIContent CylinderSideCountLabel = EditorGUIUtility.TrTextContent("Side Count");
            public static readonly GUIContent RadiusLabel = EditorGUIUtility.TrTextContent("Radius");
            public static readonly GUIContent ForceUniqueLabel = EditorGUIUtility.TrTextContent(
                "Force Unique",
                "If set to true, then this object will always produce a unique collider for run-time during conversion. " +
                "If set to false, then this object may share its collider data with other objects if they have the same inputs. " +
                "You should enable this option if you plan to modify this instance's collider at run-time."
            );
            public static readonly GUIContent MaterialLabel = EditorGUIUtility.TrTextContent("Material");
            public static readonly GUIContent SetRecommendedConvexValues = EditorGUIUtility.TrTextContent(
                "Set Recommended Default Values",
                "Set recommended values for convex hull generation parameters based on either render meshes or custom mesh."
            );

            public static GUIContent GetFitToRenderMeshesLabel(int numTargets, MessageType status) =>
                status >= MessageType.Warning
                ? numTargets == 1 ? k_FitToRenderMeshesWarningLabelSg : k_FitToRenderMeshesWarningLabelPl
                : k_FitToRenderMeshesLabel;

            static readonly string[] k_NoGeometryWarning =
            {
                L10n.Tr($"{k_Singular} has no enabled render meshes in its hierarchy and no custom mesh assigned."),
                L10n.Tr($"{k_Plural} has no enabled render meshes in their hierarchies and no custom mesh assigned.")
            };

            public static string GetNoGeometryWarning(int numTargets) =>
                numTargets == 1 ? k_NoGeometryWarning[0] : k_NoGeometryWarning[1];

            static readonly string[] k_NonReadableGeometryWarning =
            {
                L10n.Tr($"{k_Singular} has a non-readable mesh in its hierarchy. Move it into a sub-scene or assign a custom mesh with Read/Write enabled in its import settings if it needs to be converted at run-time."),
                L10n.Tr($"{k_Plural} has a non-readable mesh in its hierarchy. Move it into a sub-scene or assign a custom mesh with Read/Write enabled in its import settings if it needs to be converted at run-time.")
            };

            public static string GetNonReadableGeometryWarning(int numTargets) =>
                numTargets == 1 ? k_NonReadableGeometryWarning[0] : k_NonReadableGeometryWarning[1];

            static readonly string[] k_MeshWithSkinnedPointsWarning =
            {
                L10n.Tr($"{k_Singular} is a mesh based on its render geometry, but its render geometry includes skinned points. These points will be excluded from the automatically generated shape."),
                L10n.Tr($"{k_Plural} is a mesh based on its render geometry, but its render geometry includes skinned points. These points will be excluded from the automatically generated shape.")
            };

            public static string GetMeshWithSkinnedPointsWarning(int numTargets) =>
                numTargets == 1 ? k_MeshWithSkinnedPointsWarning[0] : k_MeshWithSkinnedPointsWarning[1];

            static readonly string[] k_StaticColliderStatusMessage =
            {
                L10n.Tr($"{k_Singular} will be considered static. Add a {ObjectNames.NicifyVariableName(typeof(PhysicsBodyAuthoring).Name)} component if you will move it at run-time."),
                L10n.Tr($"{k_Plural} will be considered static. Add a {ObjectNames.NicifyVariableName(typeof(PhysicsBodyAuthoring).Name)} component if you will move them at run-time.")
            };

            public static string GetStaticColliderStatusMessage(int numTargets) =>
                numTargets == 1 ? k_StaticColliderStatusMessage[0] : k_StaticColliderStatusMessage[1];

            public static readonly string BoxCapsuleSuggestion =
                L10n.Tr($"Target {ShapeType.Box} has uniform size on its two short axes and a large convex radius. Consider using a {ShapeType.Capsule} instead.");
            public static readonly string BoxPlaneSuggestion =
                L10n.Tr($"Target {ShapeType.Box} is flat. Consider using a {ShapeType.Plane} instead.");
            public static readonly string BoxSphereSuggestion =
                L10n.Tr($"Target {ShapeType.Box} has uniform size and large convex radius. Consider using a {ShapeType.Sphere} instead.");
            public static readonly string CapsuleSphereSuggestion =
                L10n.Tr($"Target {ShapeType.Capsule}'s diameter is equal to its height. Consider using a {ShapeType.Sphere} instead.");
            public static readonly string CylinderCapsuleSuggestion =
                L10n.Tr($"Target {ShapeType.Cylinder} has a large convex radius. Consider using a {ShapeType.Capsule} instead.");
            public static readonly string CylinderSphereSuggestion =
                L10n.Tr($"Target {ShapeType.Cylinder} has a large convex radius and its diameter is equal to its height. Consider using a {ShapeType.Sphere} instead.");

            public static readonly GUIStyle Button =
                new GUIStyle(EditorStyles.miniButton) { padding = new RectOffset() };
            public static readonly GUIStyle ButtonDropDown =
                new GUIStyle(EditorStyles.popup) { alignment = TextAnchor.MiddleCenter };
        }

        #pragma warning disable 649
        [AutoPopulate] SerializedProperty m_ShapeType;
        [AutoPopulate] SerializedProperty m_PrimitiveCenter;
        [AutoPopulate] SerializedProperty m_PrimitiveSize;
        [AutoPopulate] SerializedProperty m_PrimitiveOrientation;
        [AutoPopulate] SerializedProperty m_Capsule;
        [AutoPopulate] SerializedProperty m_Cylinder;
        [AutoPopulate] SerializedProperty m_CylinderSideCount;
        [AutoPopulate] SerializedProperty m_SphereRadius;
        [AutoPopulate] SerializedProperty m_ConvexHullGenerationParameters;
        [AutoPopulate(PropertyPath = "m_ConvexHullGenerationParameters.m_BevelRadius")] SerializedProperty m_BevelRadius;
        [AutoPopulate] SerializedProperty m_MinimumSkinnedVertexWeight;
        [AutoPopulate] SerializedProperty m_CustomMesh;
        [AutoPopulate] SerializedProperty m_Material;
        [AutoPopulate] SerializedProperty m_ForceUnique;
        #pragma warning restore 649

        [Flags]
        enum GeometryState
        {
            Okay = 0,
            NoGeometry = 1 << 0,
            NonReadableGeometry = 1 << 1,
            MeshWithSkinnedPoints = 1 << 2
        }

        GeometryState m_GeometryState;
        int m_NumImplicitStatic;
        // keep track of when the user is dragging some control to prevent continually rebuilding preview geometry
        [NonSerialized]
        int m_DraggingControlID;
        [NonSerialized]
        FitToRenderMeshesDropDown m_DropDown;

        protected override void OnEnable()
        {
            base.OnEnable();

            HashUtility.Initialize();

            m_NumImplicitStatic = targets.Cast<PhysicsShapeAuthoring>().Count(
                shape => shape.GetPrimaryBody() == shape.gameObject
                && shape.GetComponent<PhysicsBodyAuthoring>() == null
                && shape.GetComponent<Rigidbody>() == null
            );

            Undo.undoRedoPerformed += Repaint;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= Repaint;

            SceneViewUtility.ClearNotificationInSceneView();

            foreach (var preview in m_PreviewData.Values)
                preview.Dispose();

            if (m_DropDown != null)
                m_DropDown.CloseWithoutUndo();
        }

        class PreviewMeshData : IDisposable
        {
            [BurstCompile]
            struct CreateTempHullJob : IJob
            {
                public ConvexHullGenerationParameters GenerationParameters;
                [ReadOnly]
                [DeallocateOnJobCompletion]
                public NativeArray<float3> Points;
                public NativeArray<BlobAssetReference<Collider>> Output;

                public void Execute() => Output[0] = ConvexCollider.Create(Points, GenerationParameters, CollisionFilter.Default);
            }

            [BurstCompile]
            struct CreateTempMeshJob : IJob
            {
                [ReadOnly]
                [DeallocateOnJobCompletion]
                public NativeArray<float3> Points;
                [ReadOnly]
                [DeallocateOnJobCompletion]
                public NativeArray<int3> Triangles;
                public NativeArray<BlobAssetReference<Collider>> Output;

                public void Execute() => Output[0] = MeshCollider.Create(Points, Triangles);
            }

            static readonly List<Vector3> s_ReusableEdges = new List<Vector3>(1024);

            public Vector3[] Edges = Array.Empty<Vector3>();

            public Aabb Bounds = new Aabb();

            bool m_Disposed;
            uint m_InputHash;
            ConvexHullGenerationParameters m_HashedConvexParameters;
            NativeArray<float3> m_HashedPoints = new NativeArray<float3>(0, Allocator.Persistent);
            // multiple preview jobs might be running if user assigned a different mesh before previous job completed
            JobHandle m_MostRecentlyScheduledJob;
            Dictionary<JobHandle, NativeArray<BlobAssetReference<Collider>>> m_PreviewJobsOutput =
                new Dictionary<JobHandle, NativeArray<BlobAssetReference<Collider>>>();

            unsafe uint GetInputHash(
                PhysicsShapeAuthoring shape,
                NativeList<float3> currentPoints,
                NativeArray<float3> hashedPoints,
                ConvexHullGenerationParameters hashedConvexParameters,
                out ConvexHullGenerationParameters currentConvexProperties
            )
            {
                currentConvexProperties = default;
                switch (shape.ShapeType)
                {
                    case ShapeType.ConvexHull:
                        shape.GetBakedConvexProperties(currentPoints); // TODO: use HashableShapeInputs
                        currentConvexProperties = shape.ConvexHullGenerationParameters;

                        return math.hash(
                            new uint3(
                                (uint)shape.ShapeType,
                                currentConvexProperties.GetStableHash(hashedConvexParameters),
                                currentPoints.GetStableHash(hashedPoints)
                            )
                        );

                    case ShapeType.Mesh:
                        var triangles = new NativeList<int3>(1024, Allocator.Temp);
                        shape.GetBakedMeshProperties(currentPoints, triangles);  // TODO: use HashableShapeInputs

                        return math.hash(
                            new uint3(
                                (uint)shape.ShapeType,
                                currentPoints.GetStableHash(hashedPoints),
                                math.hash(triangles.GetUnsafePtr(), UnsafeUtility.SizeOf<int3>() * triangles.Length)
                            )
                        );

                    default:
                        return (uint)shape.ShapeType;
                }
            }

            public void SchedulePreviewIfChanged(PhysicsShapeAuthoring shape)
            {
                using (var currentPoints = new NativeList<float3>(65535, Allocator.Temp))
                {
                    var hash = GetInputHash(
                        shape, currentPoints, m_HashedPoints, m_HashedConvexParameters, out var currentConvexParameters
                    );
                    if (m_InputHash == hash)
                        return;

                    m_InputHash = hash;
                    m_HashedConvexParameters = currentConvexParameters;
                    m_HashedPoints.Dispose();
                    m_HashedPoints = new NativeArray<float3>(currentPoints.Length, Allocator.Persistent);
                    m_HashedPoints.CopyFrom(currentPoints.AsArray());
                }

                if (shape.ShapeType != ShapeType.ConvexHull && shape.ShapeType != ShapeType.Mesh)
                    return;

                // TODO: cache results per input data hash, and simply use existing data (e.g., to make undo/redo faster)
                var output = new NativeArray<BlobAssetReference<Collider>>(1, Allocator.Persistent);

                m_MostRecentlyScheduledJob = shape.ShapeType == ShapeType.Mesh
                    ? ScheduleMeshPreview(shape, output)
                    : ScheduleConvexHullPreview(shape, output);
                m_PreviewJobsOutput.Add(m_MostRecentlyScheduledJob, output);
                if (m_PreviewJobsOutput.Count == 1)
                {
                    CheckPreviewJobsForCompletion();
                    if (m_MostRecentlyScheduledJob.Equals(default(JobHandle)))
                        return;
                    EditorApplication.update += CheckPreviewJobsForCompletion;
                    EditorApplication.delayCall += () =>
                    {
                        SceneViewUtility.DisplayProgressNotification(
                            Styles.PreviewGenerationNotification, () => float.PositiveInfinity
                        );
                    };
                }
            }

            JobHandle ScheduleConvexHullPreview(PhysicsShapeAuthoring shape, NativeArray<BlobAssetReference<Collider>> output)
            {
                var pointCloud = new NativeList<float3>(65535, Allocator.Temp);
                shape.GetBakedConvexProperties(pointCloud);

                if (pointCloud.Length == 0)
                    return default;

                // copy to NativeArray because NativeList not yet compatible with DeallocateOnJobCompletion
                var pointsArray = new NativeArray<float3>(
                    pointCloud.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory
                );
                pointsArray.CopyFrom(pointCloud.AsArray());

                // TODO: if there is still an active job with the same input data hash, then just set it to be most recently scheduled job
                return new CreateTempHullJob
                {
                    GenerationParameters = shape.ConvexHullGenerationParameters.ToRunTime(),
                    Points = pointsArray,
                    Output = output
                }.Schedule();
            }

            JobHandle ScheduleMeshPreview(PhysicsShapeAuthoring shape, NativeArray<BlobAssetReference<Collider>> output)
            {
                var points = new NativeList<float3>(1024, Allocator.Temp);
                var triangles = new NativeList<int3>(1024, Allocator.Temp);
                shape.GetBakedMeshProperties(points, triangles);

                if (points.Length == 0 || triangles.Length == 0)
                    return default;

                // copy to NativeArray because NativeList not yet compatible with DeallocateOnJobCompletion
                var pointsArray = new NativeArray<float3>(
                    points.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory
                );
                pointsArray.CopyFrom(points.AsArray());
                var triangleArray = new NativeArray<int3>(
                    triangles.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory
                );
                triangleArray.CopyFrom(triangles.AsArray());

                // TODO: if there is still an active job with the same input data hash, then just set it to be most recently scheduled job
                return new CreateTempMeshJob
                {
                    Points = pointsArray,
                    Triangles = triangleArray,
                    Output = output
                }.Schedule();
            }

            unsafe void CheckPreviewJobsForCompletion()
            {
                var repaintSceneViews = false;

                foreach (var job in m_PreviewJobsOutput.Keys.ToArray()) // TODO: don't allocate on heap
                {
                    // repaint scene views to indicate progress if most recent preview job is still in the queue
                    var mostRecentlyScheduledJob = m_MostRecentlyScheduledJob.Equals(job);
                    repaintSceneViews |= mostRecentlyScheduledJob;

                    if (!job.IsCompleted)
                        continue;

                    var output = m_PreviewJobsOutput[job];
                    m_PreviewJobsOutput.Remove(job);
                    job.Complete();

                    // only populate preview edge data if not already disposed and this job was actually the most recent
                    if (!m_Disposed && mostRecentlyScheduledJob)
                    {
                        if (!output[0].IsCreated)
                        {
                            Edges = Array.Empty<Vector3>();
                            Bounds = new Aabb();
                        }
                        else
                        {
                            switch (output[0].Value.Type)
                            {
                                case ColliderType.Convex:
                                    ref var convex = ref output[0].As<ConvexCollider>();
                                    DrawingUtility.GetConvexColliderEdges(
                                        ref convex, s_ReusableEdges
                                    );
                                    Bounds = convex.CalculateAabb();
                                    break;
                                case ColliderType.Mesh:
                                    ref var mesh = ref output[0].As<MeshCollider>();
                                    DrawingUtility.GetMeshColliderEdges(
                                        ref mesh, s_ReusableEdges
                                    );
                                    Bounds = mesh.CalculateAabb();
                                    break;
                            }

                            Edges = s_ReusableEdges.ToArray();
                        }

                        EditorApplication.delayCall += SceneViewUtility.ClearNotificationInSceneView;
                    }

                    if (output.IsCreated)
                    {
                        if (output[0].IsCreated)
                            output[0].Dispose();
                        output.Dispose();
                    }
                }

                if (repaintSceneViews)
                    SceneView.RepaintAll();

                if (m_PreviewJobsOutput.Count == 0)
                    EditorApplication.update -= CheckPreviewJobsForCompletion;
            }

            public void Dispose()
            {
                m_Disposed = true;
                m_HashedPoints.Dispose();
            }
        }

        Dictionary<PhysicsShapeAuthoring, PreviewMeshData> m_PreviewData = new Dictionary<PhysicsShapeAuthoring, PreviewMeshData>();

        PreviewMeshData GetPreviewData(PhysicsShapeAuthoring shape)
        {
            if (shape.ShapeType != ShapeType.ConvexHull && shape.ShapeType != ShapeType.Mesh)
                return null;

            if (!m_PreviewData.TryGetValue(shape, out var preview))
            {
                preview = m_PreviewData[shape] = new PreviewMeshData();
                preview.SchedulePreviewIfChanged(shape);
            }

            // do not generate a new preview until the user has finished dragging a control handle (e.g., scale)
            if (m_DraggingControlID == 0 && !EditorGUIUtility.editingTextField)
                preview.SchedulePreviewIfChanged(shape);

            return preview;
        }

        void UpdateGeometryState()
        {
            m_GeometryState = GeometryState.Okay;
            var skinnedPoints = new NativeList<float3>(8192, Allocator.Temp);
            foreach (PhysicsShapeAuthoring shape in targets)
            {
                // if a custom mesh is assigned, only check it
                using (var so = new SerializedObject(shape))
                {
                    var customMesh = so.FindProperty(m_CustomMesh.propertyPath).objectReferenceValue as UnityEngine.Mesh;
					if (customMesh != null)
                    {
                        m_GeometryState |= GetGeometryState(customMesh, shape.gameObject);
                        continue;
                    }
                }

                // otherwise check all mesh filters in the hierarchy that might be included
                var geometryState = GeometryState.Okay;
                using (var scope = new GetActiveChildrenScope<MeshFilter>(shape, shape.transform))
                {
                    foreach (var meshFilter in scope.Buffer)
                    {
                        if (scope.IsChildActiveAndBelongsToShape(meshFilter, filterOutInvalid: false))
                            geometryState |= GetGeometryState(meshFilter.sharedMesh, shape.gameObject);
                    }
                }

                if (shape.ShapeType == ShapeType.Mesh)
                {
                    PhysicsShapeAuthoring.GetAllSkinnedPointsInHierarchyBelongingToShape(
                        shape, skinnedPoints, false, default, default, default
                    );
                    if (skinnedPoints.Length > 0)
                        geometryState |= GeometryState.MeshWithSkinnedPoints;
                }

                m_GeometryState |= geometryState;
            }
            skinnedPoints.Dispose();
        }

        static GeometryState GetGeometryState(UnityEngine.Mesh mesh, GameObject host)
        {
            if (mesh == null)
                return GeometryState.NoGeometry;
            if (!mesh.IsValidForConversion(host))
                return GeometryState.NonReadableGeometry;
            return GeometryState.Okay;
        }

        public override void OnInspectorGUI()
        {
            var hotControl = GUIUtility.hotControl;
            switch (Event.current.GetTypeForControl(hotControl))
            {
                case EventType.MouseDrag:
                    m_DraggingControlID = hotControl;
                    break;
                case EventType.MouseUp:
                    m_DraggingControlID = 0;
                    break;
            }

            UpdateGeometryState();

            serializedObject.Update();

            UpdateStatusMessages();

            EditorGUI.BeginChangeCheck();

            DisplayShapeSelector();

            ++EditorGUI.indentLevel;

            if (m_ShapeType.hasMultipleDifferentValues)
                EditorGUILayout.HelpBox(Styles.MultipleShapeTypesLabel, MessageType.None);
            else
            {
                switch ((ShapeType)m_ShapeType.intValue)
                {
                    case ShapeType.Box:
                        AutomaticPrimitiveControls();
                        DisplayBoxControls();
                        break;
                    case ShapeType.Capsule:
                        AutomaticPrimitiveControls();
                        DisplayCapsuleControls();
                        break;
                    case ShapeType.Sphere:
                        AutomaticPrimitiveControls();
                        DisplaySphereControls();
                        break;
                    case ShapeType.Cylinder:
                        AutomaticPrimitiveControls();
                        DisplayCylinderControls();
                        break;
                    case ShapeType.Plane:
                        AutomaticPrimitiveControls();
                        DisplayPlaneControls();
                        break;
                    case ShapeType.ConvexHull:
                        RecommendedConvexValuesButton();
                        EditorGUILayout.PropertyField(m_ConvexHullGenerationParameters);
                        EditorGUILayout.PropertyField(m_MinimumSkinnedVertexWeight);
                        DisplayMeshControls();
                        break;
                    case ShapeType.Mesh:
                        DisplayMeshControls();
                        break;
                    default:
                        throw new UnimplementedShapeException((ShapeType)m_ShapeType.intValue);
                }

                EditorGUILayout.PropertyField(m_ForceUnique, Styles.ForceUniqueLabel);
            }

            --EditorGUI.indentLevel;

            EditorGUILayout.LabelField(Styles.MaterialLabel);

            ++EditorGUI.indentLevel;

            EditorGUILayout.PropertyField(m_Material);

            --EditorGUI.indentLevel;

            if (m_StatusMessages.Count > 0)
                EditorGUILayout.HelpBox(string.Join("\n\n", m_StatusMessages), m_Status);

            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();
        }

        void RecommendedConvexValuesButton()
        {
            EditorGUI.BeginDisabledGroup(
                (m_GeometryState & GeometryState.NoGeometry) == GeometryState.NoGeometry ||
                EditorUtility.IsPersistent(target)
            );
            var rect = EditorGUI.IndentedRect(
                EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, EditorStyles.miniButton)
            );
            var buttonLabel = Styles.SetRecommendedConvexValues;
            if (GUI.Button(rect, buttonLabel, Styles.Button))
            {
                Undo.RecordObjects(targets, buttonLabel.text);
                foreach (PhysicsShapeAuthoring shape in targets)
                {
                    shape.InitializeConvexHullGenerationParameters();
                    EditorUtility.SetDirty(shape);
                }
            }

            EditorGUI.EndDisabledGroup();
        }

        MessageType m_GeometryStatus;
        List<string> m_GeometryStatusMessages = new List<string>();
        HashSet<string> m_ShapeSuggestions = new HashSet<string>();
        MessageType m_Status;
        List<string> m_StatusMessages = new List<string>(8);
        MessageType m_MatrixStatus;
        List<MatrixState> m_MatrixStates = new List<MatrixState>();

        void UpdateStatusMessages()
        {
            m_Status = MessageType.None;
            m_StatusMessages.Clear();

            if (m_NumImplicitStatic != 0)
                m_StatusMessages.Add(Styles.GetStaticColliderStatusMessage(targets.Length));

            m_ShapeSuggestions.Clear();
            foreach (PhysicsShapeAuthoring shape in targets)
            {
                const float k_Epsilon = HashableShapeInputs.k_DefaultLinearPrecision;
                switch (shape.ShapeType)
                {
                    case ShapeType.Box:
                        var box = shape.GetBakedBoxProperties();
                        var max = math.cmax(box.Size);
                        var min = math.cmin(box.Size);
                        if (min < k_Epsilon)
                            m_ShapeSuggestions.Add(Styles.BoxPlaneSuggestion);
                        else if (math.abs(box.BevelRadius - min * 0.5f) < k_Epsilon)
                        {
                            if (math.abs(max - min) < k_Epsilon)
                                m_ShapeSuggestions.Add(Styles.BoxSphereSuggestion);
                            else if (math.abs(math.lengthsq(box.Size - new float3(min)) - math.pow(max - min, 2f)) < k_Epsilon)
                                m_ShapeSuggestions.Add(Styles.BoxCapsuleSuggestion);
                        }
                        break;
                    case ShapeType.Capsule:
                        var capsule = shape.GetBakedCapsuleProperties();
                        if (math.abs(capsule.Height - 2f * capsule.Radius) < k_Epsilon)
                            m_ShapeSuggestions.Add(Styles.CapsuleSphereSuggestion);
                        break;
                    case ShapeType.Cylinder:
                        var cylinder = shape.GetBakedCylinderProperties();
                        if (math.abs(cylinder.BevelRadius - cylinder.Radius) < k_Epsilon)
                        {
                            m_ShapeSuggestions.Add(math.abs(cylinder.Height - 2f * cylinder.Radius) < k_Epsilon
                                ? Styles.CylinderSphereSuggestion
                                : Styles.CylinderCapsuleSuggestion);
                        }
                        break;
                }
            }
            foreach (var suggestion in m_ShapeSuggestions)
                m_StatusMessages.Add(suggestion);

            var hierarchyStatus = StatusMessageUtility.GetHierarchyStatusMessage(targets, out var hierarchyStatusMessage);
            if (!string.IsNullOrEmpty(hierarchyStatusMessage))
            {
                m_StatusMessages.Add(hierarchyStatusMessage);
                m_Status = (MessageType)math.max((int)m_Status, (int)hierarchyStatus);
            }

            m_MatrixStates.Clear();
            foreach (var t in targets)
            {
                var localToWorld = (float4x4)(t as Component).transform.localToWorldMatrix;
                m_MatrixStates.Add(ManipulatorUtility.GetMatrixState(ref localToWorld));
            }

            m_MatrixStatus = StatusMessageUtility.GetMatrixStatusMessage(m_MatrixStates, out var matrixStatusMessage);
            if (m_MatrixStatus != MessageType.None)
            {
                m_StatusMessages.Add(matrixStatusMessage);
                m_Status = (MessageType)math.max((int)m_Status, (int)m_MatrixStatus);
            }

            m_GeometryStatus = MessageType.None;
            m_GeometryStatusMessages.Clear();
            if ((m_GeometryState & GeometryState.NoGeometry) == GeometryState.NoGeometry)
            {
                m_GeometryStatusMessages.Add(Styles.GetNoGeometryWarning(targets.Length));
                m_GeometryStatus = (MessageType)math.max((int)m_GeometryStatus, (int)MessageType.Error);
            }
            if ((m_GeometryState & GeometryState.NonReadableGeometry) == GeometryState.NonReadableGeometry)
            {
                m_GeometryStatusMessages.Add(Styles.GetNonReadableGeometryWarning(targets.Length));
                m_GeometryStatus = (MessageType)math.max((int)m_GeometryStatus, (int)MessageType.Warning);
            }
            if ((m_GeometryState & GeometryState.MeshWithSkinnedPoints) == GeometryState.MeshWithSkinnedPoints)
            {
                m_GeometryStatusMessages.Add(Styles.GetMeshWithSkinnedPointsWarning(targets.Length));
                m_GeometryStatus = (MessageType)math.max((int)m_GeometryStatus, (int)MessageType.Warning);
            }
        }

        void DisplayShapeSelector()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_ShapeType);
            if (!EditorGUI.EndChangeCheck())
                return;

            Undo.RecordObjects(targets, Styles.GenericUndoMessage);
            foreach (PhysicsShapeAuthoring shape in targets)
            {
                switch ((ShapeType)m_ShapeType.intValue)
                {
                    case ShapeType.Box:
                        shape.SetBox(shape.GetBoxProperties(out var orientation), orientation);
                        break;
                    case ShapeType.Capsule:
                        shape.SetCapsule(shape.GetCapsuleProperties());
                        break;
                    case ShapeType.Sphere:
                        shape.SetSphere(shape.GetSphereProperties(out orientation), orientation);
                        break;
                    case ShapeType.Cylinder:
                        shape.SetCylinder(shape.GetCylinderProperties(out orientation), orientation);
                        break;
                    case ShapeType.Plane:
                        shape.GetPlaneProperties(out var center, out var size2D, out orientation);
                        shape.SetPlane(center, size2D, orientation);
                        break;
                    case ShapeType.ConvexHull:
                    case ShapeType.Mesh:
                        return;
                    default:
                        throw new UnimplementedShapeException((ShapeType)m_ShapeType.intValue);
                }
                EditorUtility.SetDirty(shape);
            }

            GUIUtility.ExitGUI();
        }

        void AutomaticPrimitiveControls()
        {
            EditorGUI.BeginDisabledGroup(
                (m_GeometryState & GeometryState.NoGeometry) == GeometryState.NoGeometry || EditorUtility.IsPersistent(target)
            );

            var buttonLabel = Styles.GetFitToRenderMeshesLabel(targets.Length, m_MatrixStatus);

            var rect = EditorGUI.IndentedRect(
                EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, EditorStyles.miniButton)
            );

            if (GUI.Button(rect, buttonLabel, Styles.ButtonDropDown))
                m_DropDown = FitToRenderMeshesDropDown.Show(rect, buttonLabel.text, m_MinimumSkinnedVertexWeight);

            EditorGUI.EndDisabledGroup();
        }

        class FitToRenderMeshesDropDown : EditorWindow
        {
            static class Styles
            {
                public const float WindowWidth = 400f;
                public const float LabelWidth = 200f;
                public static GUIStyle Button => PhysicsShapeAuthoringEditor.Styles.Button;
            }

            static class Content
            {
                public static readonly string ApplyLabel = L10n.Tr("Apply");
                public static readonly string CancelLabel = L10n.Tr("Cancel");
            }

            bool m_ApplyChanges;
            bool m_ClosedWithoutUndo;
            int m_UndoGroup;
            SerializedProperty m_MinimumSkinnedVertexWeight;

            public static FitToRenderMeshesDropDown Show(
                Rect buttonRect, string title, SerializedProperty minimumSkinnedVertexWeight
            )
            {
                var window = CreateInstance<FitToRenderMeshesDropDown>();
                window.titleContent = EditorGUIUtility.TrTextContent(title);
                window.m_UndoGroup = Undo.GetCurrentGroup();
                window.m_MinimumSkinnedVertexWeight = minimumSkinnedVertexWeight;
                var size = new Vector2(
                    math.max(buttonRect.width, Styles.WindowWidth),
                    (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 3f
                );
                window.maxSize = window.minSize = size;
                window.ShowAsDropDown(GUIUtility.GUIToScreenRect(buttonRect), size);
                return window;
            }

            void OnGUI()
            {
                var labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = Styles.LabelWidth;

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_MinimumSkinnedVertexWeight);
                if (EditorGUI.EndChangeCheck())
                    ApplyChanges();

                EditorGUIUtility.labelWidth = labelWidth;

                GUILayout.FlexibleSpace();

                var buttonRect = GUILayoutUtility.GetRect(0f, EditorGUIUtility.singleLineHeight);

                var buttonLeft = new Rect(buttonRect)
                {
                    width = 0.5f * (buttonRect.width - EditorGUIUtility.standardVerticalSpacing)
                };
                var buttonRight = new Rect(buttonLeft)
                {
                    x = buttonLeft.xMax + EditorGUIUtility.standardVerticalSpacing
                };

                var close = false;

                buttonRect = Application.platform == RuntimePlatform.OSXEditor ? buttonLeft : buttonRight;
                if (
                    GUI.Button(buttonRect, Content.CancelLabel, Styles.Button)
                    || Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape
                )
                {
                    close = true;
                }

                buttonRect = Application.platform == RuntimePlatform.OSXEditor ? buttonRight : buttonLeft;
                if (GUI.Button(buttonRect, Content.ApplyLabel, Styles.Button))
                {
                    close = true;
                    m_ApplyChanges = true;
                }

                if (close)
                {
                    Close();
                    EditorGUIUtility.ExitGUI();
                }
            }

            void ApplyChanges()
            {
                m_MinimumSkinnedVertexWeight.serializedObject.ApplyModifiedProperties();
                Undo.RecordObjects(m_MinimumSkinnedVertexWeight.serializedObject.targetObjects, titleContent.text);
                foreach (PhysicsShapeAuthoring shape in m_MinimumSkinnedVertexWeight.serializedObject.targetObjects)
                {
                    using (var so = new SerializedObject(shape))
                    {
                        shape.FitToEnabledRenderMeshes(
                            so.FindProperty(m_MinimumSkinnedVertexWeight.propertyPath).floatValue
                        );
                        EditorUtility.SetDirty(shape);
                    }
                }
                m_MinimumSkinnedVertexWeight.serializedObject.Update();
            }

            public void CloseWithoutUndo()
            {
                m_ApplyChanges = true;
                Close();
            }

            void OnDestroy()
            {
                if (m_ApplyChanges)
                    ApplyChanges();
                else
                    Undo.RevertAllDownToGroup(m_UndoGroup);
            }
        }

        void DisplayBoxControls()
        {
            EditorGUILayout.PropertyField(m_PrimitiveSize, Styles.SizeLabel, true);

            EditorGUILayout.PropertyField(m_PrimitiveCenter, Styles.CenterLabel, true);
            EditorGUILayout.PropertyField(m_PrimitiveOrientation, Styles.OrientationLabel, true);

            EditorGUILayout.PropertyField(m_BevelRadius);
        }

        void DisplayCapsuleControls()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_Capsule);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(targets, Styles.GenericUndoMessage);
                foreach (PhysicsShapeAuthoring shape in targets)
                {
                    shape.SetCapsule(shape.GetCapsuleProperties());
                    EditorUtility.SetDirty(shape);
                }
            }

            EditorGUILayout.PropertyField(m_PrimitiveCenter, Styles.CenterLabel, true);
            EditorGUILayout.PropertyField(m_PrimitiveOrientation, Styles.OrientationLabel, true);
        }

        void DisplaySphereControls()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_SphereRadius, Styles.RadiusLabel);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(targets, Styles.GenericUndoMessage);
                foreach (PhysicsShapeAuthoring shape in targets)
                {
                    shape.SetSphere(shape.GetSphereProperties(out EulerAngles orientation), orientation);
                    EditorUtility.SetDirty(shape);
                }
            }

            EditorGUILayout.PropertyField(m_PrimitiveCenter, Styles.CenterLabel, true);
            EditorGUILayout.PropertyField(m_PrimitiveOrientation, Styles.OrientationLabel, true);
        }

        void DisplayCylinderControls()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_Cylinder);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObjects(targets, Styles.GenericUndoMessage);
                foreach (PhysicsShapeAuthoring shape in targets)
                {
                    shape.SetCylinder(shape.GetCylinderProperties(out var orientation), orientation);
                    EditorUtility.SetDirty(shape);
                }
            }

            EditorGUILayout.PropertyField(m_PrimitiveCenter, Styles.CenterLabel, true);
            EditorGUILayout.PropertyField(m_PrimitiveOrientation, Styles.OrientationLabel, true);

            EditorGUILayout.PropertyField(m_CylinderSideCount, Styles.CylinderSideCountLabel);

            EditorGUILayout.PropertyField(m_BevelRadius);
        }

        void DisplayPlaneControls()
        {
            EditorGUILayout.PropertyField(m_PrimitiveSize, Styles.SizeLabel, true);

            EditorGUILayout.PropertyField(m_PrimitiveCenter, Styles.CenterLabel, true);
            EditorGUILayout.PropertyField(m_PrimitiveOrientation, Styles.OrientationLabel, true);
        }

        void DisplayMeshControls()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_CustomMesh);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
            }

            if (m_GeometryStatusMessages.Count > 0)
                EditorGUILayout.HelpBox(string.Join("\n\n", m_GeometryStatusMessages), m_GeometryStatus);
        }

        static readonly BeveledBoxBoundsHandle s_Box = new BeveledBoxBoundsHandle();
        static readonly PhysicsCapsuleBoundsHandle s_Capsule =
            new PhysicsCapsuleBoundsHandle { heightAxis = CapsuleBoundsHandle.HeightAxis.Z };
        static readonly BeveledCylinderBoundsHandle s_Cylinder = new BeveledCylinderBoundsHandle();
        static readonly PhysicsSphereBoundsHandle s_Sphere = new PhysicsSphereBoundsHandle();
        static readonly BoxBoundsHandle s_Plane =
            new BoxBoundsHandle { axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Z };

        static readonly Color k_ShapeHandleColor = new Color32(145, 244, 139, 210);
        static readonly Color k_ShapeHandleColorDisabled = new Color32(84, 200, 77, 140);

        void OnSceneGUI()
        {
            var hotControl = GUIUtility.hotControl;
            switch (Event.current.GetTypeForControl(hotControl))
            {
                case EventType.MouseDrag:
                    m_DraggingControlID = hotControl;
                    break;
                case EventType.MouseUp:
                    m_DraggingControlID = 0;
                    break;
            }

            var shape = target as PhysicsShapeAuthoring;

            var handleColor = shape.enabled ? k_ShapeHandleColor : k_ShapeHandleColorDisabled;
            var handleMatrix = shape.GetShapeToWorldMatrix();
            using (new Handles.DrawingScope(handleColor, handleMatrix))
            {
                switch (shape.ShapeType)
                {
                    case ShapeType.Box:
                        var boxGeometry = shape.GetBakedBoxProperties();
                        s_Box.bevelRadius = boxGeometry.BevelRadius;
                        s_Box.center = float3.zero;
                        s_Box.size = boxGeometry.Size;
                        EditorGUI.BeginChangeCheck();
                        {
                            using (new Handles.DrawingScope(math.mul(Handles.matrix, float4x4.TRS(boxGeometry.Center, boxGeometry.Orientation, 1f))))
                                s_Box.DrawHandle();
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(shape, Styles.GenericUndoMessage);
                            shape.SetBakedBoxSize(s_Box.size, s_Box.bevelRadius);
                        }
                        break;
                    case ShapeType.Capsule:
                        s_Capsule.center = float3.zero;
                        var capsuleGeometry = shape.GetBakedCapsuleProperties();
                        s_Capsule.height = capsuleGeometry.Height;
                        s_Capsule.radius = capsuleGeometry.Radius;
                        EditorGUI.BeginChangeCheck();
                        {
                            using (new Handles.DrawingScope(math.mul(Handles.matrix, float4x4.TRS(capsuleGeometry.Center, capsuleGeometry.Orientation, 1f))))
                                s_Capsule.DrawHandle();
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(shape, Styles.GenericUndoMessage);
                            shape.SetBakedCapsuleSize(s_Capsule.height, s_Capsule.radius);
                        }
                        break;
                    case ShapeType.Sphere:
                        var sphereGeometry = shape.GetBakedSphereProperties(out EulerAngles orientation);
                        s_Sphere.center = float3.zero;
                        s_Sphere.radius = sphereGeometry.Radius;
                        EditorGUI.BeginChangeCheck();
                        {
                            using (new Handles.DrawingScope(math.mul(Handles.matrix, float4x4.TRS(sphereGeometry.Center, orientation, 1f))))
                                s_Sphere.DrawHandle();
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(shape, Styles.GenericUndoMessage);
                            shape.SetBakedSphereRadius(s_Sphere.radius);
                        }
                        break;
                    case ShapeType.Cylinder:
                        var cylinderGeometry = shape.GetBakedCylinderProperties();
                        s_Cylinder.center = float3.zero;
                        s_Cylinder.height = cylinderGeometry.Height;
                        s_Cylinder.radius = cylinderGeometry.Radius;
                        s_Cylinder.sideCount = cylinderGeometry.SideCount;
                        s_Cylinder.bevelRadius = cylinderGeometry.BevelRadius;
                        EditorGUI.BeginChangeCheck();
                        {
                            using (new Handles.DrawingScope(math.mul(Handles.matrix, float4x4.TRS(cylinderGeometry.Center, cylinderGeometry.Orientation, 1f))))
                                s_Cylinder.DrawHandle();
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(shape, Styles.GenericUndoMessage);
                            shape.SetBakedCylinderSize(s_Cylinder.height, s_Cylinder.radius, s_Cylinder.bevelRadius);
                        }
                        break;
                    case ShapeType.Plane:
                        shape.GetPlaneProperties(out var center, out var size2, out orientation);
                        s_Plane.center = float3.zero;
                        s_Plane.size = new float3(size2.x, 0f, size2.y);
                        EditorGUI.BeginChangeCheck();
                        {
                            var m = math.mul(shape.transform.localToWorldMatrix, float4x4.TRS(center, orientation, 1f));
                            using (new Handles.DrawingScope(m))
                                s_Plane.DrawHandle();
                            var right = math.mul(m, new float4 { x = 1f }).xyz;
                            var forward = math.mul(m, new float4 { z = 1f }).xyz;
                            var normal = math.cross(math.normalizesafe(forward), math.normalizesafe(right))
                                * 0.5f * math.lerp(math.length(right) * size2.x, math.length(forward) * size2.y, 0.5f);

                            using (new Handles.DrawingScope(float4x4.identity))
                                Handles.DrawLine(m.c3.xyz, m.c3.xyz + normal);
                        }
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(shape, Styles.GenericUndoMessage);
                            shape.SetBakedPlaneSize(((float3)s_Plane.size).xz);
                        }
                        break;
                    case ShapeType.ConvexHull:
                        if (Event.current.type != EventType.Repaint)
                            break;
                        var points = GetPreviewData(shape).Edges;
                        // TODO: follow transformation until new preview is generated if e.g., user is dragging handles
                        if (points.Length > 0)
                            Handles.DrawLines(points);
                        break;
                    case ShapeType.Mesh:
                        if (Event.current.type != EventType.Repaint)
                            break;
                        points = GetPreviewData(shape).Edges;
                        if (points.Length > 0)
                            Handles.DrawLines(points);
                        break;
                    default:
                        throw new UnimplementedShapeException(shape.ShapeType);
                }
            }
        }

        // ReSharper disable once UnusedMember.Global - magic method called by unity inspector
        public bool HasFrameBounds()
        {
            return true;
        }

        static Bounds TransformBounds(Bounds localBounds, float4x4 matrix)
        {
            var center = new float4(localBounds.center, 1);
            Bounds bounds = new Bounds(math.mul(matrix, center).xyz, Vector3.zero);
            var extent = new float4(localBounds.extents, 0);
            for (int i = 0; i < 8; ++i)
            {
                extent.x = (i & 1) == 0 ? -extent.x : extent.x;
                extent.y = (i & 2) == 0 ? -extent.y : extent.y;
                extent.z = (i & 4) == 0 ? -extent.z : extent.z;
                var worldPoint = math.mul(matrix, center + extent).xyz;
                bounds.Encapsulate(worldPoint);
            }
            return bounds;
        }

        // ReSharper disable once UnusedMember.Global - magic method called by unity inspector
        public Bounds OnGetFrameBounds()
        {
            var shape = target as PhysicsShapeAuthoring;

            var shapeMatrix = shape.GetShapeToWorldMatrix();
            Bounds bounds = new Bounds();
            switch (shape.ShapeType)
            {
                case ShapeType.Box:
                    var boxGeometry = shape.GetBakedBoxProperties();
                    bounds = new Bounds(float3.zero, boxGeometry.Size);
                    bounds = TransformBounds(bounds, float4x4.TRS(boxGeometry.Center, boxGeometry.Orientation, 1f));
                    break;
                case ShapeType.Capsule:
                    var capsuleGeometry = shape.GetBakedCapsuleProperties();
                    var cd = capsuleGeometry.Radius * 2;
                    bounds = new Bounds(float3.zero, new float3(cd, cd, capsuleGeometry.Height));
                    bounds = TransformBounds(bounds, float4x4.TRS(capsuleGeometry.Center, capsuleGeometry.Orientation, 1f));
                    break;
                case ShapeType.Sphere:
                    var sphereGeometry = shape.GetBakedSphereProperties(out var orientation);
                    var sd = sphereGeometry.Radius * 2;
                    bounds = new Bounds(sphereGeometry.Center, new float3(sd, sd, sd));
                    break;
                case ShapeType.Cylinder:
                    var cylinderGeometry = shape.GetBakedCylinderProperties();
                    var cyld = cylinderGeometry.Radius * 2;
                    bounds = new Bounds(float3.zero, new float3(cyld, cyld, cylinderGeometry.Height));
                    bounds = TransformBounds(bounds, float4x4.TRS(cylinderGeometry.Center, cylinderGeometry.Orientation, 1f));
                    break;
                case ShapeType.Plane:
                    shape.GetPlaneProperties(out var center, out var size2, out orientation);
                    bounds = new Bounds(float3.zero, new float3(size2.x, 0, size2.y));
                    bounds = TransformBounds(bounds, float4x4.TRS(center, orientation, 1f));
                    break;
                case ShapeType.ConvexHull:
                case ShapeType.Mesh:
                    var previewData = GetPreviewData(shape);
                    if (previewData != null)
                        bounds = new Bounds(previewData.Bounds.Center, previewData.Bounds.Extents);
                    break;
                default:
                    throw new UnimplementedShapeException(shape.ShapeType);
            }

            return TransformBounds(bounds, shapeMatrix);
        }
    }
}
