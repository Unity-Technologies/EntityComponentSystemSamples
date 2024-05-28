using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.Physics.Authoring
{
    public sealed class UnimplementedShapeException : NotImplementedException
    {
        public UnimplementedShapeException(ShapeType shapeType)
            : base($"Unknown shape type {shapeType} requires explicit implementation") {}
    }

#if UNITY_2021_2_OR_NEWER
    [Icon(k_IconPath)]
#endif
    [AddComponentMenu("Entities/Physics/Physics Shape")]
    public sealed class PhysicsShapeAuthoring : MonoBehaviour, IInheritPhysicsMaterialProperties, ISerializationCallbackReceiver
    {
        const string k_IconPath = "Packages/com.unity.physics/Unity.Physics.Editor/Editor Default Resources/Icons/d_BoxCollider@64.png";

        PhysicsShapeAuthoring() {}

        [Serializable]
        struct CylindricalProperties
        {
            public float Height;
            public float Radius;
            [HideInInspector]
            public int Axis;
        }

        static readonly int[] k_NextAxis = { 1, 2, 0 };

        public ShapeType ShapeType => m_ShapeType;
        [SerializeField]
        ShapeType m_ShapeType = ShapeType.Box;

        [SerializeField]
        float3 m_PrimitiveCenter;

        [SerializeField]
        float3 m_PrimitiveSize = new float3(1f, 1f, 1f);

        [SerializeField]
        EulerAngles m_PrimitiveOrientation = EulerAngles.Default;

        [SerializeField]
        [ExpandChildren]
        CylindricalProperties m_Capsule = new CylindricalProperties { Height = 1f, Radius = 0.5f, Axis = 2 };

        [SerializeField]
        [ExpandChildren]
        CylindricalProperties m_Cylinder = new CylindricalProperties { Height = 1f, Radius = 0.5f, Axis = 2 };

        [SerializeField]
        [Tooltip("How many sides the convex cylinder shape should have.")]
        [Range(CylinderGeometry.MinSideCount, CylinderGeometry.MaxSideCount)]
        int m_CylinderSideCount = 20;

        [SerializeField]
        float m_SphereRadius = 0.5f;

        public ConvexHullGenerationParameters ConvexHullGenerationParameters => m_ConvexHullGenerationParameters;

        [SerializeField]
        [Tooltip(
            "Specifies the minimum weight of a skinned vertex assigned to this shape and/or its transform children required for it to be included for automatic detection. " +
            "A value of 0 will include all points with any weight assigned to this shape's hierarchy."
         )]
        [Range(0f, 1f)]
        float m_MinimumSkinnedVertexWeight = 0.1f;

        [SerializeField]
        [ExpandChildren]
        ConvexHullGenerationParameters m_ConvexHullGenerationParameters = ConvexHullGenerationParameters.Default.ToAuthoring();

        // TODO: remove this accessor in favor of GetRawVertices() when blob data is serializable
        internal UnityEngine.Mesh CustomMesh => m_CustomMesh;
        [SerializeField]
        [Tooltip("If no custom mesh is specified, then one will be generated using this body's rendered meshes.")]
        UnityEngine.Mesh m_CustomMesh;

        public bool ForceUnique { get => m_ForceUnique; set => m_ForceUnique = value; }
        [SerializeField]
        bool m_ForceUnique;

        public PhysicsMaterialTemplate MaterialTemplate { get => m_Material.Template; set => m_Material.Template = value; }
        PhysicsMaterialTemplate IInheritPhysicsMaterialProperties.Template
        {
            get => m_Material.Template;
            set => m_Material.Template = value;
        }

        public bool OverrideCollisionResponse { get => m_Material.OverrideCollisionResponse; set => m_Material.OverrideCollisionResponse = value; }

        public CollisionResponsePolicy CollisionResponse { get => m_Material.CollisionResponse; set => m_Material.CollisionResponse = value; }

        public bool OverrideFriction { get => m_Material.OverrideFriction; set => m_Material.OverrideFriction = value; }

        public PhysicsMaterialCoefficient Friction { get => m_Material.Friction; set => m_Material.Friction = value; }

        public bool OverrideRestitution
        {
            get => m_Material.OverrideRestitution;
            set => m_Material.OverrideRestitution = value;
        }

        public PhysicsMaterialCoefficient Restitution
        {
            get => m_Material.Restitution;
            set => m_Material.Restitution = value;
        }

        public bool OverrideBelongsTo
        {
            get => m_Material.OverrideBelongsTo;
            set => m_Material.OverrideBelongsTo = value;
        }

        public PhysicsCategoryTags BelongsTo
        {
            get => m_Material.BelongsTo;
            set => m_Material.BelongsTo = value;
        }

        public bool OverrideCollidesWith
        {
            get => m_Material.OverrideCollidesWith;
            set => m_Material.OverrideCollidesWith = value;
        }

        public PhysicsCategoryTags CollidesWith
        {
            get => m_Material.CollidesWith;
            set => m_Material.CollidesWith = value;
        }

        public bool OverrideCustomTags
        {
            get => m_Material.OverrideCustomTags;
            set => m_Material.OverrideCustomTags = value;
        }

        public CustomPhysicsMaterialTags CustomTags { get => m_Material.CustomTags; set => m_Material.CustomTags = value; }

        [SerializeField]
        PhysicsMaterialProperties m_Material = new PhysicsMaterialProperties(true);

        public BoxGeometry GetBoxProperties() => GetBoxProperties(out _);

        internal BoxGeometry GetBoxProperties(out EulerAngles orientation)
        {
            orientation = m_PrimitiveOrientation;
            return new BoxGeometry
            {
                Center = m_PrimitiveCenter,
                Size = m_PrimitiveSize,
                Orientation = m_PrimitiveOrientation,
                BevelRadius = m_ConvexHullGenerationParameters.BevelRadius
            };
        }

        void GetCylindricalProperties(
            CylindricalProperties props,
            out float3 center, out float height, out float radius, out EulerAngles orientation,
            bool rebuildOrientation
        )
        {
            center = m_PrimitiveCenter;
            var lookVector = math.mul(m_PrimitiveOrientation, new float3 { [props.Axis] = 1f });
            // use previous axis so forward will prefer up
            var upVector = math.mul(m_PrimitiveOrientation, new float3 { [k_NextAxis[k_NextAxis[props.Axis]]] = 1f });
            orientation = m_PrimitiveOrientation;
            if (rebuildOrientation && props.Axis != 2)
                orientation.SetValue(quaternion.LookRotation(lookVector, upVector));
            radius = props.Radius;
            height = props.Height;
        }

        public CapsuleGeometryAuthoring GetCapsuleProperties()
        {
            GetCylindricalProperties(
                m_Capsule, out var center, out var height, out var radius, out var orientationEuler, m_ShapeType != ShapeType.Capsule
            );
            return new CapsuleGeometryAuthoring
            {
                OrientationEuler = orientationEuler,
                Center = center,
                Height = height,
                Radius = radius
            };
        }

        public CylinderGeometry GetCylinderProperties() => GetCylinderProperties(out _);

        internal CylinderGeometry GetCylinderProperties(out EulerAngles orientation)
        {
            GetCylindricalProperties(
                m_Cylinder, out var center, out var height, out var radius, out orientation, m_ShapeType != ShapeType.Cylinder
            );
            return new CylinderGeometry
            {
                Center = center,
                Height = height,
                Radius = radius,
                Orientation = orientation,
                BevelRadius = m_ConvexHullGenerationParameters.BevelRadius,
                SideCount = m_CylinderSideCount
            };
        }

        public SphereGeometry GetSphereProperties(out quaternion orientation)
        {
            var result = GetSphereProperties(out EulerAngles euler);
            orientation = euler;
            return result;
        }

        internal SphereGeometry GetSphereProperties(out EulerAngles orientation)
        {
            orientation = m_PrimitiveOrientation;
            return new SphereGeometry
            {
                Center = m_PrimitiveCenter,
                Radius = m_SphereRadius
            };
        }

        public void GetPlaneProperties(out float3 center, out float2 size, out quaternion orientation)
        {
            GetPlaneProperties(out center, out size, out EulerAngles euler);
            orientation = euler;
        }

        internal void GetPlaneProperties(out float3 center, out float2 size, out EulerAngles orientation)
        {
            center = m_PrimitiveCenter;
            orientation = m_PrimitiveOrientation;

            if (m_ShapeType == ShapeType.Plane)
            {
                size = m_PrimitiveSize.xz;
                return;
            }

            UpdateCapsuleAxis();
            var look = m_Capsule.Axis;
            var nextAx = k_NextAxis[look];
            var prevAx = k_NextAxis[k_NextAxis[look]];
            var ax2 = m_PrimitiveSize[nextAx] > m_PrimitiveSize[prevAx] ? nextAx : prevAx;
            size = new float2(m_PrimitiveSize[ax2], m_PrimitiveSize[look]);

            var up = k_NextAxis[ax2] == look ? k_NextAxis[look] : k_NextAxis[ax2];
            var offset = quaternion.LookRotation(new float3 { [look] = 1f }, new float3 { [up] = 1f });

            orientation.SetValue(math.mul(m_PrimitiveOrientation, offset));
        }

        static readonly HashSet<int> s_BoneIDs = new HashSet<int>();
        static readonly HashSet<Transform> s_BonesInHierarchy = new HashSet<Transform>();
        static readonly List<Vector3> s_Vertices = new List<Vector3>(65535);
        static readonly List<int> s_Indices = new List<int>(65535);

        static UnityEngine.Mesh ReusableBakeMesh =>
            s_ReusableBakeMesh ??
            (s_ReusableBakeMesh = new UnityEngine.Mesh { hideFlags = HideFlags.HideAndDontSave });
        static UnityEngine.Mesh s_ReusableBakeMesh;

        public void GetConvexHullProperties(NativeList<float3> pointCloud) =>
            GetConvexHullProperties(pointCloud, true, default, default, default, default);

        internal void GetConvexHullProperties(
            NativeList<float3> pointCloud, bool validate,
            NativeList<HashableShapeInputs> inputs, NativeList<int> allSkinIndices, NativeList<float> allBlendShapeWeights,
            HashSet<UnityEngine.Mesh> meshAssets
        )
        {
            if (pointCloud.IsCreated)
                pointCloud.Clear();
            if (inputs.IsCreated)
                inputs.Clear();
            if (allSkinIndices.IsCreated)
                allSkinIndices.Clear();
            if (allBlendShapeWeights.IsCreated)
                allBlendShapeWeights.Clear();
            meshAssets?.Clear();

            if (m_CustomMesh != null)
            {
                if (validate && !m_CustomMesh.IsValidForConversion(gameObject))
                    return;

                AppendMeshPropertiesToNativeBuffers(
                    transform.localToWorldMatrix, m_CustomMesh, pointCloud, default, validate, inputs, meshAssets
                );
            }
            else
            {
                using (var scope = new GetActiveChildrenScope<MeshFilter>(this, transform))
                {
                    foreach (var meshFilter in scope.Buffer)
                    {
                        if (scope.IsChildActiveAndBelongsToShape(meshFilter, validate))
                        {
                            AppendMeshPropertiesToNativeBuffers(
                                meshFilter.transform.localToWorldMatrix, meshFilter.sharedMesh, pointCloud, default, validate, inputs, meshAssets
                            );
                        }
                    }
                }

                using (var skinnedPoints = new NativeList<float3>(8192, Allocator.Temp))
                using (var skinnedInputs = new NativeList<HashableShapeInputs>(8, Allocator.Temp))
                {
                    GetAllSkinnedPointsInHierarchyBelongingToShape(
                        this, skinnedPoints, validate, skinnedInputs, allSkinIndices, allBlendShapeWeights
                    );
                    if (pointCloud.IsCreated)
                        pointCloud.AddRange(skinnedPoints.AsArray());
                    if (inputs.IsCreated)
                        inputs.AddRange(skinnedInputs.AsArray());
                }
            }
        }

        internal static void GetAllSkinnedPointsInHierarchyBelongingToShape(
            PhysicsShapeAuthoring shape, NativeList<float3> pointCloud, bool validate,
            NativeList<HashableShapeInputs> inputs, NativeList<int> allIncludedIndices, NativeList<float> allBlendShapeWeights
        )
        {
            if (pointCloud.IsCreated)
                pointCloud.Clear();

            if (inputs.IsCreated)
                inputs.Clear();

            // get all the transforms that belong to this shape
            s_BonesInHierarchy.Clear();
            using (var scope = new GetActiveChildrenScope<Transform>(shape, shape.transform))
            {
                foreach (var bone in scope.Buffer)
                {
                    if (scope.IsChildActiveAndBelongsToShape(bone))
                        s_BonesInHierarchy.Add(bone);
                }
            }

            // find all skinned mesh renderers in which this shape's transform might be a bone
            using (var scope = new GetActiveChildrenScope<SkinnedMeshRenderer>(shape, shape.transform.root))
            {
                foreach (var skin in scope.Buffer)
                {
                    var mesh = skin.sharedMesh;
                    if (
                        !skin.enabled
                        || mesh == null
                        || validate && !mesh.IsValidForConversion(shape.gameObject)
                        || !scope.IsChildActiveAndBelongsToShape(skin)
                    )
                        continue;

                    // get indices of this shape's transform hierarchy in skinned mesh's bone array
                    s_BoneIDs.Clear();
                    var bones = skin.bones;
                    for (int i = 0, count = bones.Length; i < count; ++i)
                    {
                        if (s_BonesInHierarchy.Contains(bones[i]))
                            s_BoneIDs.Add(i);
                    }

                    if (s_BoneIDs.Count == 0)
                        continue;

                    // sample the vertices
                    if (pointCloud.IsCreated)
                    {
                        skin.BakeMesh(ReusableBakeMesh);
                        ReusableBakeMesh.GetVertices(s_Vertices);
                    }

                    // add all vertices weighted to at least one bone in this shape's transform hierarchy
                    var bonesPerVertex = mesh.GetBonesPerVertex(); // Allocator.None
                    var weights = mesh.GetAllBoneWeights(); // Allocator.None
                    var vertexIndex = 0;
                    var weightsOffset = 0;
                    var shapeFromSkin = math.mul(shape.transform.worldToLocalMatrix, skin.transform.localToWorldMatrix);
                    var includedIndices = new NativeList<int>(mesh.vertexCount, Allocator.Temp);
                    foreach (var weightCount in bonesPerVertex)
                    {
                        var totalWeight = 0f;
                        for (var i = 0; i < weightCount; ++i)
                        {
                            var weight = weights[weightsOffset + i];
                            if (s_BoneIDs.Contains(weight.boneIndex))
                                totalWeight += weight.weight;
                        }

                        if (totalWeight > shape.m_MinimumSkinnedVertexWeight)
                        {
                            if (pointCloud.IsCreated)
                                pointCloud.Add(math.mul(shapeFromSkin, new float4(s_Vertices[vertexIndex], 1f)).xyz);
                            includedIndices.Add(vertexIndex);
                        }

                        weightsOffset += weightCount;
                        ++vertexIndex;
                    }

                    if (!inputs.IsCreated || !allIncludedIndices.IsCreated || !allBlendShapeWeights.IsCreated)
                        continue;

                    var blendShapeWeights = new NativeArray<float>(mesh.blendShapeCount, Allocator.Temp);
                    for (var i = 0; i < blendShapeWeights.Length; ++i)
                        blendShapeWeights[i] = skin.GetBlendShapeWeight(i);

                    var data = HashableShapeInputs.FromSkinnedMesh(
                        mesh, shapeFromSkin, includedIndices.AsArray(), allIncludedIndices, blendShapeWeights, allBlendShapeWeights
                    );
                    inputs.Add(data);
                }
            }

            s_BonesInHierarchy.Clear();
        }

        public void GetMeshProperties(NativeList<float3> vertices, NativeList<int3> triangles) =>
            GetMeshProperties(vertices, triangles, true, default);

        internal void GetMeshProperties(
            NativeList<float3> vertices, NativeList<int3> triangles, bool validate, NativeList<HashableShapeInputs> inputs, HashSet<UnityEngine.Mesh> meshAssets = null
        )
        {
            if (vertices.IsCreated)
                vertices.Clear();
            if (triangles.IsCreated)
                triangles.Clear();
            if (inputs.IsCreated)
                inputs.Clear();
            meshAssets?.Clear();

            if (m_CustomMesh != null)
            {
                if (validate && !m_CustomMesh.IsValidForConversion(gameObject))
                    return;

                AppendMeshPropertiesToNativeBuffers(
                    transform.localToWorldMatrix, m_CustomMesh, vertices, triangles, validate, inputs, meshAssets
                );
            }
            else
            {
                using (var scope = new GetActiveChildrenScope<MeshFilter>(this, transform))
                {
                    foreach (var meshFilter in scope.Buffer)
                    {
                        if (scope.IsChildActiveAndBelongsToShape(meshFilter, validate))
                        {
                            AppendMeshPropertiesToNativeBuffers(
                                meshFilter.transform.localToWorldMatrix, meshFilter.sharedMesh, vertices, triangles, validate, inputs, meshAssets
                            );
                        }
                    }
                }
            }
        }

        void AppendMeshPropertiesToNativeBuffers(
            float4x4 localToWorld, UnityEngine.Mesh mesh, NativeList<float3> vertices, NativeList<int3> triangles, bool validate,
            NativeList<HashableShapeInputs> inputs, HashSet<UnityEngine.Mesh> meshAssets
        )
        {
            if (mesh == null || validate && !mesh.IsValidForConversion(gameObject))
                return;

            var childToShape = math.mul(transform.worldToLocalMatrix, localToWorld);

            AppendMeshPropertiesToNativeBuffers(childToShape, mesh, vertices, triangles, inputs, meshAssets);
        }

        internal static void AppendMeshPropertiesToNativeBuffers(
            float4x4 childToShape, UnityEngine.Mesh mesh, NativeList<float3> vertices, NativeList<int3> triangles,
            NativeList<HashableShapeInputs> inputs, HashSet<UnityEngine.Mesh> meshAssets
        )
        {
            var offset = 0u;
#if UNITY_EDITOR
            // TODO: when min spec is 2020.1, collect all meshes and their data via single Burst job rather than one at a time
            using (var meshData = UnityEditor.MeshUtility.AcquireReadOnlyMeshData(mesh))
#else
            using (var meshData = UnityEngine.Mesh.AcquireReadOnlyMeshData(mesh))
#endif
            {
                if (vertices.IsCreated)
                {
                    offset = (uint)vertices.Length;
                    var tmpVertices = new NativeArray<Vector3>(meshData[0].vertexCount, Allocator.Temp);
                    meshData[0].GetVertices(tmpVertices);
                    if (vertices.Capacity < vertices.Length + tmpVertices.Length)
                        vertices.Capacity = vertices.Length + tmpVertices.Length;
                    foreach (var v in tmpVertices)
                        vertices.Add(math.mul(childToShape, new float4(v, 1f)).xyz);
                }

                if (triangles.IsCreated)
                {
                    switch (meshData[0].indexFormat)
                    {
                        case IndexFormat.UInt16:
                            var indices16 = meshData[0].GetIndexData<ushort>();
                            var numTriangles = indices16.Length / 3;
                            if (triangles.Capacity < triangles.Length + numTriangles)
                                triangles.Capacity = triangles.Length + numTriangles;
                            for (var sm = 0; sm < meshData[0].subMeshCount; ++sm)
                            {
                                var subMesh = meshData[0].GetSubMesh(sm);
                                for (int i = subMesh.indexStart, count = 0; count < subMesh.indexCount; i += 3, count += 3)
                                    triangles.Add((int3) new uint3(offset + indices16[i], offset + indices16[i + 1], offset + indices16[i + 2]));
                            }
                            break;
                        case IndexFormat.UInt32:
                            var indices32 = meshData[0].GetIndexData<uint>();
                            numTriangles = indices32.Length / 3;
                            if (triangles.Capacity < triangles.Length + numTriangles)
                                triangles.Capacity = triangles.Length + numTriangles;
                            for (var sm = 0; sm < meshData[0].subMeshCount; ++sm)
                            {
                                var subMesh = meshData[0].GetSubMesh(sm);
                                for (int i = subMesh.indexStart, count = 0; count < subMesh.indexCount; i += 3, count += 3)
                                    triangles.Add((int3) new uint3(offset + indices32[i], offset + indices32[i + 1], offset + indices32[i + 2]));
                            }
                            break;
                    }
                }
            }

            if (inputs.IsCreated)
                inputs.Add(HashableShapeInputs.FromMesh(mesh, childToShape));

            meshAssets?.Add(mesh);
        }

        void UpdateCapsuleAxis()
        {
            var cmax = math.cmax(m_PrimitiveSize);
            var cmin = math.cmin(m_PrimitiveSize);
            if (cmin == cmax)
                return;
            m_Capsule.Axis = m_PrimitiveSize.GetMaxAxis();
        }

        void UpdateCylinderAxis() => m_Cylinder.Axis = m_PrimitiveSize.GetDeviantAxis();

        void Sync(ref CylindricalProperties props)
        {
            props.Height = m_PrimitiveSize[props.Axis];
            props.Radius = 0.5f * math.max(
                m_PrimitiveSize[k_NextAxis[props.Axis]],
                m_PrimitiveSize[k_NextAxis[k_NextAxis[props.Axis]]]
            );
        }

        void SyncCapsuleProperties()
        {
            if (m_ShapeType == ShapeType.Box || m_ShapeType == ShapeType.Plane)
                UpdateCapsuleAxis();
            else
                m_Capsule.Axis = 2;
            Sync(ref m_Capsule);
        }

        void SyncCylinderProperties()
        {
            if (m_ShapeType == ShapeType.Box || m_ShapeType == ShapeType.Plane)
                UpdateCylinderAxis();
            else
                m_Cylinder.Axis = 2;
            Sync(ref m_Cylinder);
        }

        void SyncSphereProperties()
        {
            m_SphereRadius = 0.5f * math.cmax(m_PrimitiveSize);
        }

        public void SetBox(BoxGeometry geometry)
        {
            var euler = m_PrimitiveOrientation;
            euler.SetValue(geometry.Orientation);
            SetBox(geometry, euler);
        }

        internal void SetBox(BoxGeometry geometry, EulerAngles orientation)
        {
            m_ShapeType = ShapeType.Box;
            m_PrimitiveCenter = geometry.Center;
            m_PrimitiveSize = math.max(geometry.Size, new float3());
            m_PrimitiveOrientation = orientation;
            m_ConvexHullGenerationParameters.BevelRadius = geometry.BevelRadius;

            SyncCapsuleProperties();
            SyncCylinderProperties();
            SyncSphereProperties();
        }

        public void SetCapsule(CapsuleGeometryAuthoring geometry)
        {
            m_ShapeType = ShapeType.Capsule;
            m_PrimitiveCenter = geometry.Center;
            m_PrimitiveOrientation = geometry.OrientationEuler;

            var radius = math.max(0f, geometry.Radius);
            var height = math.max(0f, geometry.Height);
            m_PrimitiveSize = new float3(radius * 2f, radius * 2f, height);

            SyncCapsuleProperties();
            SyncCylinderProperties();
            SyncSphereProperties();
        }

        public void SetCylinder(CylinderGeometry geometry)
        {
            var euler = m_PrimitiveOrientation;
            euler.SetValue(geometry.Orientation);
            SetCylinder(geometry, euler);
        }

        internal void SetCylinder(CylinderGeometry geometry, EulerAngles orientation)
        {
            m_ShapeType = ShapeType.Cylinder;
            m_PrimitiveCenter = geometry.Center;
            m_PrimitiveOrientation = orientation;

            geometry.Radius = math.max(0f, geometry.Radius);
            geometry.Height = math.max(0f, geometry.Height);
            m_PrimitiveSize = new float3(geometry.Radius * 2f, geometry.Radius * 2f, geometry.Height);

            m_ConvexHullGenerationParameters.BevelRadius = geometry.BevelRadius;

            SyncCapsuleProperties();
            SyncCylinderProperties();
            SyncSphereProperties();
        }

        public void SetSphere(SphereGeometry geometry, quaternion orientation)
        {
            var euler = m_PrimitiveOrientation;
            euler.SetValue(orientation);
            SetSphere(geometry, euler);
        }

        internal void SetSphere(SphereGeometry geometry)
        {
            SetSphere(geometry, m_PrimitiveOrientation);
        }

        internal void SetSphere(SphereGeometry geometry, EulerAngles orientation)
        {
            m_ShapeType = ShapeType.Sphere;
            m_PrimitiveCenter = geometry.Center;

            var radius = math.max(0f, geometry.Radius);
            m_PrimitiveSize = new float3(2f * radius, 2f * radius, 2f * radius);

            m_PrimitiveOrientation = orientation;

            SyncCapsuleProperties();
            SyncCylinderProperties();
            SyncSphereProperties();
        }

        public void SetPlane(float3 center, float2 size, quaternion orientation)
        {
            var euler = m_PrimitiveOrientation;
            euler.SetValue(orientation);
            SetPlane(center, size, euler);
        }

        internal void SetPlane(float3 center, float2 size, EulerAngles orientation)
        {
            m_ShapeType = ShapeType.Plane;
            m_PrimitiveCenter = center;
            m_PrimitiveOrientation = orientation;
            m_PrimitiveSize = new float3(size.x, 0f, size.y);

            SyncCapsuleProperties();
            SyncCylinderProperties();
            SyncSphereProperties();
        }

        public void SetConvexHull(
            ConvexHullGenerationParameters hullGenerationParameters, float minimumSkinnedVertexWeight
        )
        {
            m_MinimumSkinnedVertexWeight = minimumSkinnedVertexWeight;
            SetConvexHull(hullGenerationParameters);
        }

        public void SetConvexHull(ConvexHullGenerationParameters hullGenerationParameters, UnityEngine.Mesh customMesh = null)
        {
            m_ShapeType = ShapeType.ConvexHull;
            m_CustomMesh = customMesh;
            hullGenerationParameters.OnValidate();
            m_ConvexHullGenerationParameters = hullGenerationParameters;
        }

        public void SetMesh(UnityEngine.Mesh mesh = null)
        {
            m_ShapeType = ShapeType.Mesh;
            m_CustomMesh = mesh;
        }

        void OnEnable()
        {
            // included so tick box appears in Editor
        }

        const int k_LatestVersion = 1;

        [SerializeField]
        int m_SerializedVersion = 0;

        void ISerializationCallbackReceiver.OnBeforeSerialize() {}

        void ISerializationCallbackReceiver.OnAfterDeserialize() => UpgradeVersionIfNecessary();

#pragma warning disable 618
        void UpgradeVersionIfNecessary()
        {
            if (m_SerializedVersion < k_LatestVersion)
            {
                // old data from version < 1 have been removed
                if (m_SerializedVersion < 1)
                    m_SerializedVersion = 1;
            }
        }

#pragma warning restore 618

        static void Validate(ref CylindricalProperties props)
        {
            props.Height = math.max(0f, props.Height);
            props.Radius = math.max(0f, props.Radius);
        }

        void OnValidate()
        {
            UpgradeVersionIfNecessary();

            m_PrimitiveSize = math.max(m_PrimitiveSize, new float3());
            Validate(ref m_Capsule);
            Validate(ref m_Cylinder);
            switch (m_ShapeType)
            {
                case ShapeType.Box:
                    SetBox(GetBoxProperties(out var orientation), orientation);
                    break;
                case ShapeType.Capsule:
                    SetCapsule(GetCapsuleProperties());
                    break;
                case ShapeType.Cylinder:
                    SetCylinder(GetCylinderProperties(out orientation), orientation);
                    break;
                case ShapeType.Sphere:
                    SetSphere(GetSphereProperties(out orientation), orientation);
                    break;
                case ShapeType.Plane:
                    GetPlaneProperties(out var center, out var size2D, out orientation);
                    SetPlane(center, size2D, orientation);
                    break;
                case ShapeType.ConvexHull:
                case ShapeType.Mesh:
                    break;
                default:
                    throw new UnimplementedShapeException(m_ShapeType);
            }
            SyncCapsuleProperties();
            SyncCylinderProperties();
            SyncSphereProperties();
            m_CylinderSideCount =
                math.clamp(m_CylinderSideCount, CylinderGeometry.MinSideCount, CylinderGeometry.MaxSideCount);
            m_ConvexHullGenerationParameters.OnValidate();

            PhysicsMaterialProperties.OnValidate(ref m_Material, true);
        }

        // matrix to transform point from shape space into world space
        public float4x4 GetShapeToWorldMatrix() =>
            new float4x4(Math.DecomposeRigidBodyTransform(transform.localToWorldMatrix));

        // matrix to transform point from object's local transform matrix into shape space
        internal float4x4 GetLocalToShapeMatrix() =>
            math.mul(math.inverse(GetShapeToWorldMatrix()), transform.localToWorldMatrix);

        internal unsafe void BakePoints(NativeArray<float3> points)
        {
            var localToShapeQuantized = GetLocalToShapeMatrix();
            using (var aabb = new NativeArray<Aabb>(1, Allocator.TempJob))
            {
                new PhysicsShapeExtensions.GetAabbJob { Points = points, Aabb = aabb }.Run();
                HashableShapeInputs.GetQuantizedTransformations(localToShapeQuantized, aabb[0], out localToShapeQuantized);
            }
            using (var bakedPoints = new NativeArray<float3>(points.Length, Allocator.TempJob, NativeArrayOptions.UninitializedMemory))
            {
                new BakePointsJob
                {
                    Points = points,
                    LocalToShape = localToShapeQuantized,
                    Output = bakedPoints
                }.Schedule(points.Length, 16).Complete();

                UnsafeUtility.MemCpy(points.GetUnsafePtr(), bakedPoints.GetUnsafePtr(), points.Length * UnsafeUtility.SizeOf<float3>());
            }
        }

        [BurstCompile]
        struct BakePointsJob : IJobParallelFor
        {
            [Collections.ReadOnly]
            public NativeArray<float3> Points;
            public float4x4 LocalToShape;
            public NativeArray<float3> Output;

            public void Execute(int index) => Output[index] = math.mul(LocalToShape, new float4(Points[index], 1f)).xyz;
        }

        /// <summary>
        /// Fit this shape to render geometry in its GameObject hierarchy. Children in the hierarchy will
        /// influence the result if they have enabled MeshRenderer components or have vertices bound to
        /// them on a SkinnedMeshRenderer. Children will only count as influences if this shape is the
        /// first ancestor shape in their hierarchy. As such, you should add shape components to all
        /// GameObjects that should have them before you call this method on any of them.
        /// </summary>
        ///
        /// <exception cref="UnimplementedShapeException"> Thrown when an Unimplemented Shape error
        /// condition occurs. </exception>
        ///
        /// <param name="minimumSkinnedVertexWeight"> (Optional)
        /// The minimum total weight that a vertex in a skinned mesh must have assigned to this object
        /// and/or any of its influencing children. </param>

        public void FitToEnabledRenderMeshes(float minimumSkinnedVertexWeight = 0f)
        {
            var shapeType = m_ShapeType;
            m_MinimumSkinnedVertexWeight = minimumSkinnedVertexWeight;

            using (var points = new NativeList<float3>(65535, Allocator.Persistent))
            {
                // temporarily un-assign custom mesh and assume this shape is a convex hull
                var customMesh = m_CustomMesh;
                m_CustomMesh = null;
                GetConvexHullProperties(points, Application.isPlaying, default, default, default, default);
                m_CustomMesh = customMesh;
                if (points.Length == 0)
                    return;

                // TODO: find best rotation, particularly if any points came from skinned mesh
                var orientation = quaternion.identity;
                var bounds = new Bounds(points[0], float3.zero);
                for (int i = 1, count = points.Length; i < count; ++i)
                    bounds.Encapsulate(points[i]);

                SetBox(
                    new BoxGeometry
                    {
                        Center = bounds.center,
                        Size = bounds.size,
                        Orientation = orientation,
                        BevelRadius = m_ConvexHullGenerationParameters.BevelRadius
                    }
                );
            }

            switch (shapeType)
            {
                case ShapeType.Capsule:
                    SetCapsule(GetCapsuleProperties());
                    break;
                case ShapeType.Cylinder:
                    SetCylinder(GetCylinderProperties(out var orientation), orientation);
                    break;
                case ShapeType.Sphere:
                    SetSphere(GetSphereProperties(out orientation), orientation);
                    break;
                case ShapeType.Plane:
                    // force recalculation of plane orientation by making it think shape type is out of date
                    m_ShapeType = ShapeType.Box;
                    GetPlaneProperties(out var center, out var size2D, out orientation);
                    SetPlane(center, size2D, orientation);
                    break;
                case ShapeType.Box:
                case ShapeType.ConvexHull:
                case ShapeType.Mesh:
                    m_ShapeType = shapeType;
                    break;
                default:
                    throw new UnimplementedShapeException(shapeType);
            }
            SyncCapsuleProperties();
            SyncCylinderProperties();
            SyncSphereProperties();
        }

        [Conditional("UNITY_EDITOR")]
        void Reset()
        {
#if UNITY_EDITOR
            InitializeConvexHullGenerationParameters();
            FitToEnabledRenderMeshes(m_MinimumSkinnedVertexWeight);
            // TODO: also pick best primitive shape
            UnityEditor.SceneView.RepaintAll();
#endif
        }

        public void InitializeConvexHullGenerationParameters()
        {
            var pointCloud = new NativeList<float3>(65535, Allocator.Temp);
            GetConvexHullProperties(pointCloud, false, default, default, default, default);
            m_ConvexHullGenerationParameters.InitializeToRecommendedAuthoringValues(pointCloud.AsArray());
        }
    }
}
