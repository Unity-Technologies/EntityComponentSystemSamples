using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Entities.Graphics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public struct SpawnParameters : IEquatable<SpawnParameters>
{
    internal float3 Origin;
    internal float3 Scale;
    public int EntityCount;
    public int MaterialCount;
    public int MeshCount;
    public float ObjectScale;
    public Material BaseMaterial;
    public bool SpreadMaterials;
    public bool SpreadMeshes;
    public bool InstancedColor;
    public bool Movement;

    public bool Equals(SpawnParameters other)
    {
        return Origin.Equals(other.Origin) && Scale.Equals(other.Scale) && EntityCount == other.EntityCount &&
               MaterialCount == other.MaterialCount && MeshCount == other.MeshCount &&
               ObjectScale.Equals(other.ObjectScale) && Equals(BaseMaterial, other.BaseMaterial) &&
               SpreadMaterials == other.SpreadMaterials && SpreadMeshes == other.SpreadMeshes &&
               InstancedColor == other.InstancedColor && Movement == other.Movement;
    }

    public override bool Equals(object obj)
    {
        return obj is SpawnParameters other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Origin.GetHashCode();
            hashCode = (hashCode * 397) ^ Scale.GetHashCode();
            hashCode = (hashCode * 397) ^ EntityCount;
            hashCode = (hashCode * 397) ^ MaterialCount;
            hashCode = (hashCode * 397) ^ MeshCount;
            hashCode = (hashCode * 397) ^ ObjectScale.GetHashCode();
            hashCode = (hashCode * 397) ^ (BaseMaterial != null ? BaseMaterial.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ SpreadMaterials.GetHashCode();
            hashCode = (hashCode * 397) ^ SpreadMeshes.GetHashCode();
            hashCode = (hashCode * 397) ^ InstancedColor.GetHashCode();
            hashCode = (hashCode * 397) ^ Movement.GetHashCode();
            return hashCode;
        }
    }

    public static bool operator ==(SpawnParameters left, SpawnParameters right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SpawnParameters left, SpawnParameters right)
    {
        return !left.Equals(right);
    }
}

public class BatchingBenchmark : MonoBehaviour
{
    public SpawnParameters SpawnParameters = new SpawnParameters
    {
        EntityCount = 100000,
        MaterialCount = 10,
        MeshCount = 10,
        ObjectScale = 0.5f,
        SpreadMaterials = false,
        SpreadMeshes = true,
        Movement = true,
    };
    public static SpawnParameters s_SpawnParameters;

    private void UpdateSpawnParameters()
    {
        SpawnParameters.Origin = transform.position;
        SpawnParameters.Scale = transform.localScale;
        s_SpawnParameters = SpawnParameters;
    }

    // Start is called before the first frame update
    void Start()
    {
        UpdateSpawnParameters();
    }

    private void Awake()
    {
        UpdateSpawnParameters();
    }

    // Update is called once per frame
    void Update()
    {
    }
}

public struct BatchingBenchmark_Tag : IComponentData {}

public struct StartingPosition : IComponentData
{
    public float3 Value;
}

[MaterialProperty("_BaseColor")]
public struct SRPBaseColor : IComponentData
{
    public float4 Value;
}

public partial class BatchingBenchmarkSystem : SystemBase
{
    [BurstCompile]
    public struct SpawnUtilities
    {
        public static float4 ComputeColor(int index, int maxCount, float saturation = 1)
        {
            float t = (float) index / math.max(1, maxCount - 1);
            var color = Color.HSVToRGB(t, saturation, 1);
            return new float4(color.r, color.g, color.b, 1);
        }

        public static float3 ComputePosition(int index, int2 dim, float3 origin, float3 scale)
        {
            int x = index % dim.y;
            int y = index / dim.y;

            float2 uv = new float2(
                (float) x / (dim.x - 1),
                (float) y / (dim.y - 1));

            float3 extent = new float3(scale.x, 0, scale.z) / 2.0f;
            float3 min = origin - extent;
            float3 max = origin + extent;
            float3 pos = new float3(
                math.lerp(min.x, max.x, uv.x),
                origin.y,
                math.lerp(min.z, max.z, uv.y));
            return pos;
        }

        public static float4x4 ComputeTransform(float3 pos, float scale)
        {
            float4x4 transform = float4x4.TRS(pos, quaternion.identity, scale);
            return transform;
        }
    }

    [BurstCompile]
    public struct SpawnJob : IJobParallelFor
    {
        public Entity Prototype;
        public int EntityCount;
        public int2 Dim;

        public float3 Origin;
        public float3 Scale;
        public float ObjectScale;

        public bool SetColor;
        public bool SetStartingPosition;

        public int NumMaterials;
        public int NumMeshes;
        public bool SpreadMaterials;
        public bool SpreadMeshes;

        public EntityCommandBuffer.ParallelWriter Ecb;

        [Unity.Collections.ReadOnly]
        public NativeArray<RenderBounds> MeshBounds;

        public void Execute(int index)
        {
            var e = Ecb.Instantiate(index, Prototype);

            float3 position = SpawnUtilities.ComputePosition(index, Dim, Origin, Scale);
            float4x4 transform = SpawnUtilities.ComputeTransform(position, ObjectScale);

            // Prototype has all correct components up front, can use SetComponent
            Ecb.SetComponent(index, e, new LocalToWorld {Value = transform});

            if (SetColor)
                Ecb.SetComponent(index, e, new SRPBaseColor {Value = SpawnUtilities.ComputeColor(index, EntityCount, 1f)});

            if (SetStartingPosition)
                Ecb.SetComponent(index, e, new StartingPosition {Value = position});

            int materialIndex = math.min(NumMaterials - 1,
                SpreadMaterials
                    ? (index % NumMaterials)
                    : (index * NumMaterials / EntityCount));

            int meshIndex = math.min(NumMeshes - 1,
                SpreadMeshes
                    ? (index % NumMeshes)
                    : (index * NumMeshes / EntityCount));

            // MeshBounds must be set according to the actual mesh for culling to work.
            Ecb.SetComponent(index, e, MaterialMeshInfo.FromRenderMeshArrayIndices(materialIndex, meshIndex));
            Ecb.SetComponent(index, e, MeshBounds[meshIndex]);
        }
    }

    private SpawnParameters m_SpawnParameters;

    private bool UpdateSpawnParameters()
    {
        var newParameters = BatchingBenchmark.s_SpawnParameters;

        if (newParameters != m_SpawnParameters)
        {
            m_SpawnParameters = newParameters;
            return true;
        }
        else
        {
            return false;
        }
    }

    protected override void OnUpdate()
    {
        Dependency.Complete();

        if (UpdateSpawnParameters())
        {
            Spawn();
        }

        if (m_SpawnParameters.Movement)
            MoveEntities();
    }

    public void Despawn()
    {
        EntityManager.DestroyEntity(GetEntityQuery(ComponentType.ReadWrite<BatchingBenchmark_Tag>()));
    }

    public void Spawn()
    {
        Despawn();

        Debug.Log($"Spawning {m_SpawnParameters.EntityCount} entities using base material {m_SpawnParameters.BaseMaterial}");

        if (m_SpawnParameters.EntityCount <= 0 || m_SpawnParameters.BaseMaterial == null)
            return;

        int numMaterials = math.max(1, m_SpawnParameters.MaterialCount);
        int numMeshes = math.max(1, m_SpawnParameters.MeshCount);

        var materials = CreateMaterials(numMaterials);
        var meshes = CreateMeshes(numMeshes);

        var renderBounds = new NativeArray<RenderBounds>(numMeshes, Allocator.TempJob);
        for (int i = 0; i < renderBounds.Length; ++i)
            renderBounds[i] = new RenderBounds {Value = meshes[i].bounds.ToAABB()};

        var renderMeshArray = new RenderMeshArray(materials.ToArray(), meshes.ToArray());
        var renderMeshDescription = new RenderMeshDescription
        {
            FilterSettings = new RenderFilterSettings
            {
                Layer = 0,
                RenderingLayerMask = 0xffffffff,
                MotionMode = m_SpawnParameters.Movement
                    ? MotionVectorGenerationMode.Object
                    : MotionVectorGenerationMode.Camera,
                ReceiveShadows = true,
                ShadowCastingMode = ShadowCastingMode.On,
                StaticShadowCaster = false,
            },
            LightProbeUsage = LightProbeUsage.Off,
        };

        var prototype = EntityManager.CreateEntity();
        RenderMeshUtility.AddComponents(
            prototype,
            EntityManager,
            renderMeshDescription,
            renderMeshArray,
            MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
        EntityManager.AddComponent<BatchingBenchmark_Tag>(prototype);

        if (m_SpawnParameters.Movement)
            EntityManager.AddComponent<StartingPosition>(prototype);

        if (m_SpawnParameters.InstancedColor)
            EntityManager.AddComponent<SRPBaseColor>(prototype);

        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        int dim = (int) math.ceil(math.sqrt( m_SpawnParameters.EntityCount));

        var spawnJobHandle = new SpawnJob
        {
            Prototype = prototype,
            Ecb = ecb.AsParallelWriter(),
            EntityCount = m_SpawnParameters.EntityCount,
            Dim = new int2(dim),
            Origin = m_SpawnParameters.Origin,
            Scale = m_SpawnParameters.Scale,
            ObjectScale = m_SpawnParameters.ObjectScale,
            NumMaterials = numMaterials,
            NumMeshes = numMeshes,
            SpreadMaterials = m_SpawnParameters.SpreadMaterials,
            SpreadMeshes = m_SpawnParameters.SpreadMeshes,
            SetColor = m_SpawnParameters.InstancedColor,
            SetStartingPosition = m_SpawnParameters.Movement,
            MeshBounds = renderBounds,
        }.Schedule(m_SpawnParameters.EntityCount, 100);
        renderBounds.Dispose(spawnJobHandle);
        spawnJobHandle.Complete();

        ecb.Playback(EntityManager);
        ecb.Dispose();
        EntityManager.DestroyEntity(prototype);
    }

    private List<Material> CreateMaterials(int numMaterials)
    {
        var materials = new List<Material>(numMaterials);

        for (int i = 0; i < numMaterials; ++i)
        {
            var mat = new Material(m_SpawnParameters.BaseMaterial);
            var colorF4 = SpawnUtilities.ComputeColor(i, numMaterials, 0.5f);
            var color = new Color(colorF4.x, colorF4.y, colorF4.z, 1);
            mat.SetColor("_BaseColor", color);
            materials.Add(mat);
        }

        return materials;
    }

    private List<Mesh> CreateMeshes(int numMeshes)
    {
        var meshes = new List<Mesh>(numMeshes);

        for (int i = 0; i < numMeshes; ++i)
        {
            int meshType = i % 4;
            PrimitiveType primitive;
            switch (meshType)
            {
                default:
                case 0: primitive = PrimitiveType.Cube; break;
                case 1: primitive = PrimitiveType.Sphere; break;
                case 2: primitive = PrimitiveType.Capsule; break;
                case 3: primitive = PrimitiveType.Cylinder; break;
            }

            var go = GameObject.CreatePrimitive(primitive);
            meshes.Add(go.GetComponent<MeshFilter>().mesh);
            UnityEngine.Object.DestroyImmediate(go);
        }

        return meshes;
    }

    private void MoveEntities()
    {
        var t = SystemAPI.Time.ElapsedTime;
        var origin = m_SpawnParameters.Origin;
        var scale = m_SpawnParameters.ObjectScale;
        const float speed = 10;

        Entities
            .WithName("MoveEntities")
            .ForEach((ref LocalToWorld transform, in StartingPosition start) =>
        {
            var relativeStart = start.Value - origin;

            var p0 = relativeStart.xz;

            var R = math.length(p0);

            var angle0 = math.atan2(p0.y, p0.x);
            var angularSpeed = speed / R;

            var angle = (float) math.fmod(angle0 + angularSpeed * t, math.PI_DBL * 2);

            var p = new float2(math.cos(angle), math.sin(angle)) * R;

            transform.Value = SpawnUtilities.ComputeTransform(
                new float3(p.x, origin.y, p.y),
                scale);
        }).ScheduleParallel();
    }
}
