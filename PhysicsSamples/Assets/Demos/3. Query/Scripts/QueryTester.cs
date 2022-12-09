using System;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.NotBurstCompatible;
using static Unity.Physics.CompoundCollider;
using Random = Unity.Mathematics.Random;
using static Unity.Physics.Math;

namespace Unity.Physics.Extensions
{
    /// Simple Behaviour for testing broadphase raycasts. Provides a
    /// gizmo which can be manipulated to cast a ray during simulation.
    /// Displays hit positions or the tested line segment.
    public unsafe class QueryTester : MonoBehaviour
    {
        [BurstCompile]
        public struct RaycastJob : IJob
        {
            public RaycastInput RaycastInput;
            public NativeList<RaycastHit> RaycastHits;
            public bool CollectAllHits;
            [ReadOnly] public PhysicsWorld World;

            public void Execute()
            {
                if (CollectAllHits)
                {
                    World.CastRay(RaycastInput, ref RaycastHits);
                }
                else if (World.CastRay(RaycastInput, out RaycastHit hit))
                {
                    RaycastHits.Add(hit);
                }
            }
        }

        [BurstCompile]
        public struct ColliderCastJob : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            public ColliderCastInput Input;
            public NativeList<ColliderCastHit> ColliderCastHits;
            public bool CollectAllHits;
            [ReadOnly] public PhysicsWorld World;

            public void Execute()
            {
                if (CollectAllHits)
                {
                    World.CastCollider(Input, ref ColliderCastHits);
                }
                else if (World.CastCollider(Input, out ColliderCastHit hit))
                {
                    ColliderCastHits.Add(hit);
                }
            }
        }

        [BurstCompile]
        public struct PointDistanceJob : IJob
        {
            public PointDistanceInput PointDistanceInput;
            public NativeList<DistanceHit> DistanceHits;
            public bool CollectAllHits;
            [ReadOnly] public PhysicsWorld World;

            public void Execute()
            {
                if (CollectAllHits)
                {
                    World.CalculateDistance(PointDistanceInput, ref DistanceHits);
                }
                else if (World.CalculateDistance(PointDistanceInput, out DistanceHit hit))
                {
                    DistanceHits.Add(hit);
                }
            }
        }

        [BurstCompile]
        public struct ColliderDistanceJob : IJob
        {
            [NativeDisableUnsafePtrRestriction]
            public ColliderDistanceInput Input;
            public NativeList<DistanceHit> DistanceHits;
            public bool CollectAllHits;
            [ReadOnly] public PhysicsWorld World;

            public void Execute()
            {
                if (CollectAllHits)
                {
                    World.CalculateDistance(Input, ref DistanceHits);
                }
                else if (World.CalculateDistance(Input, out DistanceHit hit))
                {
                    DistanceHits.Add(hit);
                }
            }
        }

        #region ColliderConstructionParams

        private static readonly float3[] k_TetraherdonVertices =
        {
            new float3(-1, 0, 0),
            new float3(0, 1.0f, 0),
            new float3(1.0f, -1.0f, -1.0f),
            new float3(-1.0f, 0, 0),
            new float3(0, -1.0f, 1.0f),
            new float3(0, 1.0f, 0),
            new float3(-1.0f, 0, 0),
            new float3(1.0f, -1.0f, -1.0f),
            new float3(0, -1.0f, 1.0f),
            new float3(0, 1.0f, 0),
            new float3(0, -1.0f, 1.0f),
            new float3(1.0f, -1.0f, -1.0f)
        };

        private static readonly int3[] k_TetrahedronMeshTriangles =
        {
            new int3(0, 1, 2),
            new int3(3, 4, 5),
            new int3(6, 7, 8),
            new int3(9, 10, 11)
        };

        private static readonly float3[] k_TriangleVertices =
        {
            new float3(0, 1.0f, 0),
            new float3(-1.0f, 0, 0),
            new float3(1.0f, 0, 0)
        };

        private static readonly float3[] k_QuadVertices =
        {
            new float3(0.5f, 0.5f, 0),
            new float3(0.5f, -0.5f, 0),
            new float3(-0.5f, -0.5f, 0),
            new float3(-0.5f, 0.5f, 0)
        };

        #endregion

        private BlobAssetReference<Collider> CreateCollider(ColliderType type)
        {
            int numMeshes = type == ColliderType.Compound ? 2 : 1;
            ColliderMeshes = new Mesh[numMeshes];
            BlobAssetReference<Collider> collider = default;

            switch (type)
            {
                case ColliderType.Sphere:
                    collider = SphereCollider.Create(new SphereGeometry
                    {
                        Center = float3.zero,
                        Radius = 0.5f
                    });
                    break;
                case ColliderType.Triangle:
                    collider = PolygonCollider.CreateTriangle(k_TriangleVertices[0], k_TriangleVertices[1], k_TriangleVertices[2]);
                    break;
                case ColliderType.Quad:
                    collider = PolygonCollider.CreateQuad(k_QuadVertices[0], k_QuadVertices[1], k_QuadVertices[2], k_QuadVertices[3]);
                    break;
                case ColliderType.Box:
                    collider = BoxCollider.Create(new BoxGeometry
                    {
                        Center = float3.zero,
                        Orientation = quaternion.identity,
                        Size = new float3(1.0f),
                        BevelRadius = 0.0f
                    });
                    break;
                case ColliderType.Capsule:
                    collider = CapsuleCollider.Create(new CapsuleGeometry
                    {
                        Vertex0 = new float3(0, -0.5f, 0),
                        Vertex1 = new float3(0, 0.5f, 0),
                        Radius = 0.5f
                    });
                    break;
                case ColliderType.Cylinder:
                    // TODO: need someone to add
                    throw new NotImplementedException();
                case ColliderType.Convex:
                    // Tetrahedron
                    NativeArray<float3> points = new NativeArray<float3>(k_TetraherdonVertices, Allocator.TempJob);
                    collider = ConvexCollider.Create(points, ConvexHullGenerationParameters.Default, CollisionFilter.Default);
                    points.Dispose();
                    break;
                case ColliderType.Compound:

                    var child1 = SphereCollider.Create(new SphereGeometry
                    {
                        Center = float3.zero,
                        Radius = 0.5f
                    });
                    ChildrenColliders.Add(child1);

                    var child2 = BoxCollider.Create(new BoxGeometry
                    {
                        Center = float3.zero,
                        Orientation = quaternion.identity,
                        Size = new float3(1.0f),
                        BevelRadius = 0.0f
                    });
                    ChildrenColliders.Add(child2);

                    NativeArray<ColliderBlobInstance> childrenBlobs = new NativeArray<ColliderBlobInstance>(2, Allocator.TempJob);
                    childrenBlobs[0] = new ColliderBlobInstance
                    {
                        Collider = child1,
                        CompoundFromChild = new RigidTransform
                        {
                            pos = new float3(0.5f, 0, 0),
                            rot = quaternion.identity
                        }
                    };

                    childrenBlobs[1] = new ColliderBlobInstance
                    {
                        Collider = child2,
                        CompoundFromChild = new RigidTransform
                        {
                            pos = new float3(-0.5f, 0, 0),
                            rot = quaternion.identity
                        }
                    };

                    ColliderMeshes[0] = SceneCreationUtilities.CreateMeshFromCollider(child1);
                    ColliderMeshes[1] = SceneCreationUtilities.CreateMeshFromCollider(child2);

                    collider = CompoundCollider.Create(childrenBlobs);
                    childrenBlobs.Dispose();
                    break;
                case ColliderType.Mesh:
                    // Tetrahedron mesh
                    NativeArray<float3> meshVertices = new NativeArray<float3>(k_TetraherdonVertices, Allocator.TempJob);
                    NativeArray<int3> meshTriangles = new NativeArray<int3>(k_TetrahedronMeshTriangles, Allocator.TempJob);

                    collider = MeshCollider.Create(meshVertices, meshTriangles);
                    meshVertices.Dispose();
                    meshTriangles.Dispose();
                    break;
                case ColliderType.Terrain:
                    int2 size = 2;
                    float3 scale = 1;
                    Random rand = new Random(0x9739);

                    int numSamples = size.x * size.y;
                    var heights = new NativeArray<float>(numSamples, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
                    for (int i = 0; i < numSamples; i++)
                    {
                        heights[i] = rand.NextFloat(0, 1);
                    }
                    collider = TerrainCollider.Create(heights, size, scale, TerrainCollider.CollisionMethod.VertexSamples);
                    heights.Dispose();
                    break;
                default:
                    throw new System.NotImplementedException();
            }

            if (ColliderType != ColliderType.Compound)
            {
                ColliderMeshes[0] = SceneCreationUtilities.CreateMeshFromCollider(collider);
            }

            return collider;
        }

        void Start()
        {
            Simulating = true;
            ChildrenColliders = new NativeList<BlobAssetReference<Collider>>(Allocator.Persistent);

            if (ColliderQuery)
            {
                Collider = CreateCollider(ColliderType);
            }

            RaycastHits = new NativeList<RaycastHit>(Allocator.Persistent);
            ColliderCastHits = new NativeList<ColliderCastHit>(Allocator.Persistent);
            DistanceHits = new NativeList<DistanceHit>(Allocator.Persistent);
        }

        void OnDestroy()
        {
            if (RaycastHits.IsCreated)
            {
                RaycastHits.Dispose();
            }
            if (ColliderCastHits.IsCreated)
            {
                ColliderCastHits.Dispose();
            }
            if (DistanceHits.IsCreated)
            {
                DistanceHits.Dispose();
            }
            if (Collider.IsCreated)
            {
                Collider.Dispose();
            }
            if (ChildrenColliders.IsCreated)
            {
                for (int i = 0; i < ChildrenColliders.Length; i++)
                {
                    ChildrenColliders[i].Dispose();
                }
                ChildrenColliders.Dispose();
            }
        }

        void RunQueries()
        {
            EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<PhysicsWorldSingleton>();
            EntityQuery singletonQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(builder);
            PhysicsWorld world = singletonQuery.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
            float3 origin = transform.position;
            float3 direction = (transform.rotation * Direction) * Distance;

            RaycastHits.Clear();
            ColliderCastHits.Clear();
            DistanceHits.Clear();
            singletonQuery.Dispose();

            if (!ColliderQuery)
            {
                if (math.any(new float3(Direction) != float3.zero))
                {
                    RaycastInput = new RaycastInput
                    {
                        Start = origin,
                        End = origin + direction,
                        Filter = CollisionFilter.Default
                    };

                    new RaycastJob
                    {
                        RaycastInput = RaycastInput,
                        RaycastHits = RaycastHits,
                        CollectAllHits = CollectAllHits,
                        World = world,
                    }.Schedule().Complete();
                }
                else
                {
                    PointDistanceInput = new PointDistanceInput
                    {
                        Position = origin,
                        MaxDistance = Distance,
                        Filter = CollisionFilter.Default
                    };

                    new PointDistanceJob
                    {
                        PointDistanceInput = PointDistanceInput,
                        DistanceHits = DistanceHits,
                        CollectAllHits = CollectAllHits,
                        World = world,
                    }.Schedule().Complete();
                }
            }
            else
            {
                if (math.any(new float3(Direction) != float3.zero))
                {
                    unsafe
                    {
                        ColliderCastInput = new ColliderCastInput
                        {
                            Collider = Collider.AsPtr(),
                            Orientation = transform.rotation,
                            Start = origin,
                            End = origin + direction,
                            QueryColliderScale = InputColliderScale
                        };

                        new ColliderCastJob
                        {
                            Input = ColliderCastInput,
                            ColliderCastHits = ColliderCastHits,
                            CollectAllHits = CollectAllHits,
                            World = world
                        }.Schedule().Complete();
                    }
                }
                else
                {
                    unsafe
                    {
                        ColliderDistanceInput = new ColliderDistanceInput
                        {
                            Collider = Collider.AsPtr(),
                            Transform = new RigidTransform(transform.rotation, origin),
                            MaxDistance = Distance,
                            Scale = InputColliderScale
                        };

                        new ColliderDistanceJob
                        {
                            Input = ColliderDistanceInput,
                            DistanceHits = DistanceHits,
                            CollectAllHits = CollectAllHits,
                            World = world,
                        }.Schedule().Complete();
                    }
                }
            }
        }

        public float Distance = 10.0f;
        public Vector3 Direction = new Vector3(1, 0, 0);
        public bool CollectAllHits = false;
        public bool DrawSurfaceNormal = true;
        public bool HighlightLeafCollider = true;
        public ColliderType ColliderType;
        public bool ColliderQuery;
        [Tooltip("Applied only if ColliderQuery == true")]
        public float InputColliderScale = 1.0f;

        protected bool Simulating;
        protected RaycastInput RaycastInput;
        protected NativeList<RaycastHit> RaycastHits;
        protected ColliderCastInput ColliderCastInput;
        protected NativeList<ColliderCastHit> ColliderCastHits;
        protected PointDistanceInput PointDistanceInput;
        protected ColliderDistanceInput ColliderDistanceInput;
        protected NativeList<DistanceHit> DistanceHits;
        protected BlobAssetReference<Collider> Collider;
        protected NativeList<BlobAssetReference<Collider>> ChildrenColliders;
        protected UnityEngine.Mesh[] ColliderMeshes = null;

        void OnDrawGizmos()
        {
            if (Simulating)
            {
                RunQueries();

                EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp)
                    .WithAll<PhysicsWorldSingleton>();
                EntityQuery singletonQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(builder);
                PhysicsWorld world = singletonQuery.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

                // Draw the query
                Gizmos.color = new Color(0.94f, 0.35f, 0.15f, 0.75f);
                bool colliderCast = math.any(new float3(Direction) != float3.zero);
                if (ColliderQuery)
                {
                    ScaledMTransform worldFromCollider = colliderCast ?
                        new ScaledMTransform(new RigidTransform(ColliderCastInput.Orientation, ColliderCastInput.Start), ColliderCastInput.QueryColliderScale) :
                        new ScaledMTransform(ColliderDistanceInput.Transform, ColliderDistanceInput.Scale);

                    if (colliderCast)
                    {
                        Gizmos.DrawRay(worldFromCollider.Translation, ColliderCastInput.End - ColliderCastInput.Start);
                    }

                    if (ColliderType != ColliderType.Compound)
                    {
                        Gizmos.DrawWireMesh(ColliderMeshes[0], worldFromCollider.Translation, new quaternion(worldFromCollider.Rotation), new float3(InputColliderScale));
                    }
                    else
                    {
                        unsafe
                        {
                            CompoundCollider* compoundCollider = colliderCast ? (CompoundCollider*)ColliderCastInput.Collider : (CompoundCollider*)ColliderDistanceInput.Collider;
                            for (int i = 0; i < compoundCollider->NumChildren; i++)
                            {
                                ref Child child = ref compoundCollider->Children[i];
                                ScaledMTransform worldFromChild = ScaledMTransform.Mul(worldFromCollider, new MTransform(child.CompoundFromChild));

                                Gizmos.DrawWireMesh(ColliderMeshes[i], worldFromChild.Translation, new quaternion(worldFromChild.Rotation), new float3(InputColliderScale));
                            }
                        }
                    }
                }
                else
                {
                    if (math.any(new float3(Direction) != float3.zero))
                    {
                        Gizmos.DrawRay(RaycastInput.Start, RaycastInput.End - RaycastInput.Start);
                    }
                    else
                    {
                        Gizmos.DrawSphere(PointDistanceInput.Position, 0.05f);
                    }
                }

                // Draw ray hits
                if (RaycastHits.IsCreated)
                {
                    foreach (RaycastHit hit in RaycastHits.ToArrayNBC())
                    {
                        Assert.IsTrue(hit.RigidBodyIndex >= 0 && hit.RigidBodyIndex < world.NumBodies);
                        Assert.IsTrue(math.abs(math.lengthsq(hit.SurfaceNormal) - 1.0f) < 0.01f);

                        Gizmos.color = Color.magenta;
                        Gizmos.DrawRay(RaycastInput.Start, hit.Position - RaycastInput.Start);
                        Gizmos.DrawSphere(hit.Position, 0.02f);

                        if (DrawSurfaceNormal)
                        {
                            Gizmos.color = Color.green;
                            Gizmos.DrawRay(hit.Position, hit.SurfaceNormal);
                        }

                        if (HighlightLeafCollider && !hit.ColliderKey.Equals(ColliderKey.Empty))
                        {
                            Gizmos.color = Color.yellow;
                            DrawLeafCollider(world.Bodies[hit.RigidBodyIndex], hit.ColliderKey);
#if UNITY_EDITOR
                            GUIStyle style = new GUIStyle();
                            style.normal.textColor = Color.yellow;
                            Handles.Label(hit.Position, hit.ColliderKey.Value.ToString("X8"), style);
#endif
                        }
                    }
                }

                // Draw collider hits
                if (ColliderCastHits.IsCreated)
                {
                    foreach (ColliderCastHit hit in ColliderCastHits.ToArrayNBC())
                    {
                        Assert.IsTrue(hit.RigidBodyIndex >= 0 && hit.RigidBodyIndex < world.NumBodies);
                        Assert.IsTrue(math.abs(math.lengthsq(hit.SurfaceNormal) - 1.0f) < 0.01f);

                        Gizmos.color = Color.magenta;
                        Gizmos.DrawSphere(hit.Position, 0.02f);
                        Gizmos.DrawSphere(hit.Position - (ColliderCastInput.End - ColliderCastInput.Start) * hit.Fraction, 0.02f);

                        if (Collider.Value.Type == ColliderType.Compound)
                        {
                            var colliderkey = hit.QueryColliderKey;
                            Collider.Value.GetChild(ref colliderkey, out ChildCollider child);

                            unsafe
                            {
                                CompoundCollider* compound = Collider.AsPtr<CompoundCollider>();
                                for (int i = 0; i < compound->NumChildren; i++)
                                {
                                    if (child.Collider->Type == compound->Children[i].Collider->Type)
                                    {
                                        MTransform compoundFromChild = new MTransform(child.TransformFromChild);

                                        ScaledMTransform worldFromCompoundCastStart = new ScaledMTransform(new RigidTransform(ColliderCastInput.Orientation, ColliderCastInput.Start), InputColliderScale);
                                        ScaledMTransform worldFromCompoundCastEnd = new ScaledMTransform(new RigidTransform(ColliderCastInput.Orientation, ColliderCastInput.End), InputColliderScale);

                                        var worldFromChildCastStart = ScaledMTransform.Mul(worldFromCompoundCastStart, compoundFromChild);
                                        var worldFromChildCastEnd = ScaledMTransform.Mul(worldFromCompoundCastEnd, compoundFromChild);

                                        Gizmos.DrawWireMesh(ColliderMeshes[i], math.lerp(worldFromChildCastStart.Translation, worldFromChildCastEnd.Translation, hit.Fraction), new quaternion(worldFromChildCastStart.Rotation), new float3(InputColliderScale));
                                        break;
                                    }
                                }
                            }
                        }
                        else
                        {
                            Gizmos.DrawWireMesh(ColliderMeshes[0], math.lerp(ColliderCastInput.Start, ColliderCastInput.End, hit.Fraction), ColliderCastInput.Orientation, new float3(InputColliderScale));
                        }

                        if (DrawSurfaceNormal)
                        {
                            Gizmos.color = Color.green;
                            Gizmos.DrawRay(hit.Position, hit.SurfaceNormal);
                        }

                        if (HighlightLeafCollider && !hit.ColliderKey.Equals(ColliderKey.Empty))
                        {
                            Gizmos.color = Color.yellow;
                            DrawLeafCollider(world.Bodies[hit.RigidBodyIndex], hit.ColliderKey);
#if UNITY_EDITOR
                            GUIStyle style = new GUIStyle();
                            style.normal.textColor = Color.yellow;
                            Handles.Label(hit.Position, hit.ColliderKey.Value.ToString("X8"), style);
#endif
                        }

#if UNITY_EDITOR
                        if (HighlightLeafCollider && Collider.Value.CollisionType != CollisionType.Convex && !hit.QueryColliderKey.Equals(ColliderKey.Empty))
                        {
                            float3 flippedPosition = hit.Position - hit.Fraction * (ColliderCastInput.End - ColliderCastInput.Start);

                            GUIStyle style = new GUIStyle();
                            style.normal.textColor = Color.yellow;
                            Handles.Label(flippedPosition, hit.QueryColliderKey.Value.ToString("X8"), style);
                        }
#endif
                    }
                }

                // Draw distance hits
                if (DistanceHits.IsCreated)
                {
                    foreach (DistanceHit hit in DistanceHits.ToArrayNBC())
                    {
                        Assert.IsTrue(hit.RigidBodyIndex >= 0 && hit.RigidBodyIndex < world.NumBodies);
                        Assert.IsTrue(math.abs(math.lengthsq(hit.SurfaceNormal) - 1.0f) < 0.01f);


                        float maxDistance = ColliderQuery ? ColliderDistanceInput.MaxDistance : PointDistanceInput.MaxDistance;
                        Assert.IsTrue(hit.Fraction <= maxDistance);
                        float3 queryPoint = hit.Position + hit.SurfaceNormal * hit.Distance;

                        Gizmos.color = Color.magenta;
                        Gizmos.DrawSphere(hit.Position, 0.02f);
                        Gizmos.DrawSphere(queryPoint, 0.02f);
                        Gizmos.DrawLine(hit.Position, queryPoint);

                        if (DrawSurfaceNormal)
                        {
                            Gizmos.color = Color.green;
                            Gizmos.DrawRay(hit.Position, hit.SurfaceNormal);
                        }

                        if (HighlightLeafCollider && !hit.ColliderKey.Equals(ColliderKey.Empty))
                        {
                            Gizmos.color = Color.yellow;
                            DrawLeafCollider(world.Bodies[hit.RigidBodyIndex], hit.ColliderKey);
#if UNITY_EDITOR
                            GUIStyle style = new GUIStyle();
                            style.normal.textColor = Color.yellow;
                            Handles.Label(hit.Position, hit.ColliderKey.Value.ToString("X8"), style);
#endif
                        }

#if UNITY_EDITOR
                        if (ColliderQuery && HighlightLeafCollider && Collider.Value.CollisionType != CollisionType.Convex && !hit.QueryColliderKey.Equals(ColliderKey.Empty))
                        {
                            float3 flippedPosition = hit.Position + hit.SurfaceNormal * hit.Fraction;

                            GUIStyle style = new GUIStyle();
                            style.normal.textColor = Color.yellow;
                            Handles.Label(flippedPosition, hit.QueryColliderKey.Value.ToString("X8"), style);
                        }
#endif
                    }
                }
            }
            else
            {
                Gizmos.color = Color.red;
                Transform t = transform;

                Vector3 dir = (t.rotation * Direction) * Distance;

                Gizmos.DrawRay(t.position, dir);
            }
        }

        private void DrawLeafCollider(RigidBody body, ColliderKey key)
        {
            if (body.Collider.Value.GetLeaf(key, out ChildCollider leaf) && (leaf.Collider == null))
            {
                RigidTransform worldFromLeaf = math.mul(body.WorldFromBody, leaf.TransformFromChild);
                if (leaf.Collider->Type == ColliderType.Triangle || leaf.Collider->Type == ColliderType.Quad)
                {
                    PolygonCollider* polygon = (PolygonCollider*)leaf.Collider;
                    float3 v0 = math.transform(worldFromLeaf, polygon->Vertices[0]);
                    float3 v1 = math.transform(worldFromLeaf, polygon->Vertices[1]);
                    float3 v2 = math.transform(worldFromLeaf, polygon->Vertices[2]);
                    float3 v3 = float3.zero;
                    if (polygon->IsQuad)
                    {
                        v3 = math.transform(worldFromLeaf, polygon->Vertices[3]);
                    }

                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(v0, v1);
                    Gizmos.DrawLine(v1, v2);
                    if (polygon->IsTriangle)
                    {
                        Gizmos.DrawLine(v2, v0);
                    }
                    else
                    {
                        Gizmos.DrawLine(v2, v3);
                        Gizmos.DrawLine(v3, v0);
                    }
                }
            }
        }
    }
}
