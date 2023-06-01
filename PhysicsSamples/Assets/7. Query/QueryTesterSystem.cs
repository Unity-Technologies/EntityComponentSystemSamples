using System;
using System.Collections.Generic;
using Common.Scripts;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Authoring;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Random = Unity.Mathematics.Random;

namespace Unity.Physics.Extensions
{
    [UpdateInGroup(typeof(PhysicsSimulationGroup))]
    public partial class QueryTesterSystem : SystemBase
    {
#if UNITY_EDITOR
        private static string k_MeshDisplayMaterialPath = "Assets/7. Query/MeshDisplayMaterial.mat";
        private static UnityEngine.Material k_MeshDisplayMaterial = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(k_MeshDisplayMaterialPath);
#endif

        private MeshTrsList m_MeshTrsList;

        protected override void OnCreate()
        {
            m_MeshTrsList = new MeshTrsList();
            RequireForUpdate<QueryData>();
        }

        protected override void OnUpdate()
        {
            // Properly chain up dependencies
            {
                if (!SystemAPI.TryGetSingleton<PhysicsDebugDisplayData>(out _))
                {
                    var singletonEntity = EntityManager.CreateEntity();
                    EntityManager.AddComponentData(singletonEntity, new PhysicsDebugDisplayData());
                }
                SystemAPI.GetSingletonRW<PhysicsDebugDisplayData>();
            }

            var meshTrsList = m_MeshTrsList;
            var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

            NativeList<RaycastHit> raycastHits = new NativeList<RaycastHit>(Allocator.TempJob);
            NativeList<ColliderCastHit> colliderCastHits = new NativeList<ColliderCastHit>(Allocator.TempJob);
            NativeList<DistanceHit> distanceHits = new NativeList<DistanceHit>(Allocator.TempJob);

            // The generated code doesn't automatically complete the dependency on PhysicsWorldSingleton
            EntityManager.CompleteDependencyBeforeRO<PhysicsWorldSingleton>();

            foreach (var(qd, localToWorld) in SystemAPI.Query<QueryData, RefRO<LocalToWorld>>())
            {
                if (qd.ColliderQuery && !qd.ColliderDataInitialized)
                {
                    CreateCollider(qd);
                    qd.ColliderDataInitialized = true;
                }

                raycastHits.Clear();
                colliderCastHits.Clear();
                distanceHits.Clear();

                RaycastInput raycastInput = default;
                ColliderCastInput colliderCastInput = default;
                PointDistanceInput pointDistanceInput = default;
                ColliderDistanceInput colliderDistanceInput = default;

                RunQueries(physicsWorld, localToWorld.ValueRO.Position, localToWorld.ValueRO.Rotation,
                    qd, ref raycastInput, ref colliderCastInput,
                    ref pointDistanceInput, ref colliderDistanceInput, ref raycastHits,
                    ref colliderCastHits, ref distanceHits);

                DisplayResults(physicsWorld, qd, raycastInput,
                    colliderCastInput, pointDistanceInput, colliderDistanceInput,
                    raycastHits, colliderCastHits, distanceHits,
                    ref meshTrsList);
            }

            raycastHits.Dispose();
            colliderCastHits.Dispose();
            distanceHits.Dispose();

            meshTrsList.DrawAndReset();
        }

        protected override void OnDestroy()
        {
            EntityQuery query = GetEntityQuery(ComponentType.ReadWrite<QueryData>());
            QueryData[] qdArr = query.ToComponentArray<QueryData>();

            if (qdArr != null)
            {
                for (int i = 0; i < qdArr.Length; i++)
                {
                    QueryData qd = qdArr[i];
                    if (qd.ColliderQuery && qd.ColliderDataInitialized)
                    {
                        qd.Collider.Dispose();

                        if (qd.ChildrenColliders != null)
                        {
                            for (int j = 0; j < qd.ChildrenColliders.Length; j++)
                            {
                                qd.ChildrenColliders[j].Dispose();
                            }
                        }
                    }
                }
            }
        }

        public class MeshTrsList
        {
            internal List<MeshTrs> m_MeshTrsList;

            public MeshTrsList() => m_MeshTrsList = new List<MeshTrs>();

            public void AddMeshTrs(UnityEngine.Mesh mesh, float3 t, quaternion q, float s)
            {
                Matrix4x4 trs = default;
                trs.SetTRS(t, q, new float3(s));

                MeshTrs meshTrs = new MeshTrs
                {
                    m_Mesh = mesh,
                    m_Trs = trs
                };

                m_MeshTrsList.Add(meshTrs);
            }

            public void DrawAndReset()
            {
#if UNITY_EDITOR
                for (int i = 0; i < m_MeshTrsList.Count; i++)
                {
                    MeshTrs meshTrs = m_MeshTrsList[i];

                    // Using this as DrawMeshInstanced is the only thing that works
                    Matrix4x4[] trs = new[] { meshTrs.m_Trs };
                    UnityEngine.Graphics.DrawMeshInstanced(meshTrs.m_Mesh, 0, k_MeshDisplayMaterial, trs);
                }
#endif
                m_MeshTrsList.Clear();
            }

            internal class MeshTrs
            {
                internal UnityEngine.Mesh m_Mesh;
                internal Matrix4x4 m_Trs;
            }
        }

        #region Queries and display

        void RunQueries(in PhysicsWorld world, in float3 pos, in quaternion rot,
            in QueryData queryData, ref RaycastInput raycastInput, ref ColliderCastInput colliderCastInput,
            ref PointDistanceInput pointDistanceInput, ref ColliderDistanceInput colliderDistanceInput, ref NativeList<RaycastHit> raycastHits,
            ref NativeList<ColliderCastHit> colliderCastHits, ref NativeList<DistanceHit> distanceHits)
        {
            float3 origin = pos;
            float3 direction = math.rotate(rot, queryData.Direction) * queryData.Distance;

            if (!queryData.ColliderQuery)
            {
                if (math.any(new float3(queryData.Direction) != float3.zero))
                {
                    raycastInput = new RaycastInput
                    {
                        Start = origin,
                        End = origin + direction,
                        Filter = CollisionFilter.Default
                    };

                    new RaycastJob
                    {
                        RaycastInput = raycastInput,
                        RaycastHits = raycastHits,
                        CollectAllHits = queryData.CollectAllHits,
                        World = world
                    }.Schedule().Complete();
                }
                else
                {
                    pointDistanceInput = new PointDistanceInput
                    {
                        Position = origin,
                        MaxDistance = queryData.Distance,
                        Filter = CollisionFilter.Default
                    };

                    new PointDistanceJob
                    {
                        PointDistanceInput = pointDistanceInput,
                        DistanceHits = distanceHits,
                        CollectAllHits = queryData.CollectAllHits,
                        World = world
                    }.Schedule().Complete();
                }
            }
            else
            {
                if (math.any(new float3(queryData.Direction) != float3.zero))
                {
                    unsafe
                    {
                        colliderCastInput = new ColliderCastInput
                        {
                            Collider = queryData.Collider.AsPtr(),
                            Orientation = rot,
                            Start = origin,
                            End = origin + direction,
                            QueryColliderScale = queryData.InputColliderScale
                        };

                        new ColliderCastJob
                        {
                            Input = colliderCastInput,
                            ColliderCastHits = colliderCastHits,
                            CollectAllHits = queryData.CollectAllHits,
                            World = world
                        }.Schedule().Complete();
                    }
                }
                else
                {
                    unsafe
                    {
                        colliderDistanceInput = new ColliderDistanceInput
                        {
                            Collider = queryData.Collider.AsPtr(),
                            Transform = new RigidTransform(rot, origin),
                            MaxDistance = queryData.Distance,
                            Scale = queryData.InputColliderScale
                        };

                        new ColliderDistanceJob
                        {
                            Input = colliderDistanceInput,
                            DistanceHits = distanceHits,
                            CollectAllHits = queryData.CollectAllHits,
                            World = world
                        }.Schedule().Complete();
                    }
                }
            }
        }

        void DisplayResults(in PhysicsWorld world, in QueryData queryData, in RaycastInput raycastInput,
            in ColliderCastInput colliderCastInput, in PointDistanceInput pointDistanceInput, in ColliderDistanceInput colliderDistanceInput,
            in NativeList<RaycastHit> raycastHits, in NativeList<ColliderCastHit> colliderCastHits, in NativeList<DistanceHit> distanceHits,
            ref MeshTrsList meshTrsList)
        {
            // Draw the query
            bool colliderCast = math.any(new float3(queryData.Direction) != float3.zero);
            if (queryData.ColliderQuery)
            {
                Math.ScaledMTransform worldFromCollider = colliderCast ?
                    new Math.ScaledMTransform(new RigidTransform(colliderCastInput.Orientation, colliderCastInput.Start), colliderCastInput.QueryColliderScale) :
                    new Math.ScaledMTransform(colliderDistanceInput.Transform, colliderDistanceInput.Scale);

                if (colliderCast)
                {
                    PhysicsDebugDisplaySystem.Line(worldFromCollider.Translation, colliderCastInput.End, Unity.DebugDisplay.ColorIndex.Red);
                }

                if (queryData.ColliderType != ColliderType.Compound)
                {
                    meshTrsList.AddMeshTrs(queryData.ColliderMeshes[0], worldFromCollider.Translation, new quaternion(worldFromCollider.Rotation), queryData.InputColliderScale);
                }
                else
                {
                    unsafe
                    {
                        CompoundCollider* compoundCollider = colliderCast ? (CompoundCollider*)colliderCastInput.Collider : (CompoundCollider*)colliderDistanceInput.Collider;
                        for (int i = 0; i < compoundCollider->NumChildren; i++)
                        {
                            ref Unity.Physics.CompoundCollider.Child child = ref compoundCollider->Children[i];
                            Math.ScaledMTransform worldFromChild = Math.ScaledMTransform.Mul(worldFromCollider, new Math.MTransform(child.CompoundFromChild));

                            meshTrsList.AddMeshTrs(queryData.ColliderMeshes[i], worldFromChild.Translation, new quaternion(worldFromChild.Rotation), queryData.InputColliderScale);
                        }
                    }
                }
            }
            else
            {
                if (math.any(new float3(queryData.Direction) != float3.zero))
                {
                    PhysicsDebugDisplaySystem.Line(raycastInput.Start, raycastInput.End, Unity.DebugDisplay.ColorIndex.Red);
                }
                else
                {
                    PhysicsDebugDisplaySystem.Point(pointDistanceInput.Position, 0.05f, Unity.DebugDisplay.ColorIndex.Red);
                }
            }

            // Draw ray hits
            if (raycastHits.IsCreated)
            {
                foreach (RaycastHit hit in raycastHits)
                {
                    Assert.IsTrue(hit.RigidBodyIndex >= 0 && hit.RigidBodyIndex < world.NumBodies);
                    Assert.IsTrue(math.abs(math.lengthsq(hit.SurfaceNormal) - 1.0f) < 0.01f);

                    PhysicsDebugDisplaySystem.Line(raycastInput.Start, hit.Position, Unity.DebugDisplay.ColorIndex.Magenta);
                    PhysicsDebugDisplaySystem.Point(hit.Position, 0.02f, Unity.DebugDisplay.ColorIndex.White);

                    if (queryData.DrawSurfaceNormal)
                    {
                        PhysicsDebugDisplaySystem.Line(hit.Position, hit.Position + hit.SurfaceNormal, Unity.DebugDisplay.ColorIndex.Green);
                    }

                    if (queryData.HighlightLeafCollider && !hit.ColliderKey.Equals(ColliderKey.Empty))
                    {
                        DrawLeafCollider(world.Bodies[hit.RigidBodyIndex], hit.ColliderKey);

                        // Need to fix this once Unity.DebugDisplay.Label starts working and is exposed in PhysicsDebugDisplaySystem API [Havok-275]
                        //GUIStyle style = new GUIStyle();
                        //style.normal.textColor = Color.yellow;
                        //Handles.Label(hit.Position, hit.ColliderKey.Value.ToString("X8"), style);
                    }
                }
            }

            // Draw collider hits
            if (colliderCastHits.IsCreated)
            {
                foreach (ColliderCastHit hit in colliderCastHits)
                {
                    Assert.IsTrue(hit.RigidBodyIndex >= 0 && hit.RigidBodyIndex < world.NumBodies);
                    Assert.IsTrue(math.abs(math.lengthsq(hit.SurfaceNormal) - 1.0f) < 0.01f);

                    Gizmos.color = Color.magenta;
                    PhysicsDebugDisplaySystem.Point(hit.Position, 0.02f, Unity.DebugDisplay.ColorIndex.White);
                    PhysicsDebugDisplaySystem.Point(hit.Position - (colliderCastInput.End - colliderCastInput.Start) * hit.Fraction, 0.02f, Unity.DebugDisplay.ColorIndex.White);

                    if (queryData.Collider.Value.Type == ColliderType.Compound)
                    {
                        var colliderkey = hit.QueryColliderKey;
                        queryData.Collider.Value.GetChild(ref colliderkey, out ChildCollider child);

                        unsafe
                        {
                            CompoundCollider* compound = queryData.Collider.AsPtr<CompoundCollider>();
                            for (int i = 0; i < compound->NumChildren; i++)
                            {
                                if (child.Collider->Type == compound->Children[i].Collider->Type)
                                {
                                    Math.MTransform compoundFromChild = new Math.MTransform(child.TransformFromChild);

                                    Math.ScaledMTransform worldFromCompoundCastStart = new Math.ScaledMTransform(new RigidTransform(colliderCastInput.Orientation, colliderCastInput.Start), queryData.InputColliderScale);
                                    Math.ScaledMTransform worldFromCompoundCastEnd = new Math.ScaledMTransform(new RigidTransform(colliderCastInput.Orientation, colliderCastInput.End), queryData.InputColliderScale);

                                    var worldFromChildCastStart = Math.ScaledMTransform.Mul(worldFromCompoundCastStart, compoundFromChild);
                                    var worldFromChildCastEnd = Math.ScaledMTransform.Mul(worldFromCompoundCastEnd, compoundFromChild);

                                    meshTrsList.AddMeshTrs(queryData.ColliderMeshes[i], math.lerp(worldFromChildCastStart.Translation, worldFromChildCastEnd.Translation, hit.Fraction),
                                        new quaternion(worldFromChildCastStart.Rotation), queryData.InputColliderScale);
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        meshTrsList.AddMeshTrs(queryData.ColliderMeshes[0], math.lerp(colliderCastInput.Start, colliderCastInput.End, hit.Fraction), colliderCastInput.Orientation, queryData.InputColliderScale);
                    }

                    if (queryData.DrawSurfaceNormal)
                    {
                        PhysicsDebugDisplaySystem.Line(hit.Position, hit.Position + hit.SurfaceNormal, Unity.DebugDisplay.ColorIndex.Green);
                    }

                    if (queryData.HighlightLeafCollider && !hit.ColliderKey.Equals(ColliderKey.Empty))
                    {
                        DrawLeafCollider(world.Bodies[hit.RigidBodyIndex], hit.ColliderKey);

                        // Need to fix this once Unity.DebugDisplay.Label starts working and is exposed in PhysicsDebugDisplaySystem API [Havok-275]
                        //GUIStyle style = new GUIStyle();
                        //style.normal.textColor = Color.yellow;
                        //Handles.Label(hit.Position, hit.ColliderKey.Value.ToString("X8"), style);
                    }

                    // Need to fix this once Unity.DebugDisplay.Label starts working and is exposed in PhysicsDebugDisplaySystem API [Havok-275]
//#if UNITY_EDITOR
//                    if (queryData.HighlightLeafCollider && queryData.Collider.Value.CollisionType != CollisionType.Convex && !hit.QueryColliderKey.Equals(ColliderKey.Empty))
//                    {
//                        float3 flippedPosition = hit.Position - hit.Fraction * (colliderCastInput.End - colliderCastInput.Start);

//                        GUIStyle style = new GUIStyle();
//                        style.normal.textColor = Color.yellow;
//                        Handles.Label(flippedPosition, hit.QueryColliderKey.Value.ToString("X8"), style);
//                    }
//#endif
                }
            }

            // Draw distance hits
            if (distanceHits.IsCreated)
            {
                foreach (DistanceHit hit in distanceHits)
                {
                    Assert.IsTrue(hit.RigidBodyIndex >= 0 && hit.RigidBodyIndex < world.NumBodies);
                    Assert.IsTrue(math.abs(math.lengthsq(hit.SurfaceNormal) - 1.0f) < 0.01f);

                    float maxDistance = queryData.ColliderQuery ? colliderDistanceInput.MaxDistance : pointDistanceInput.MaxDistance;
                    Assert.IsTrue(hit.Fraction <= maxDistance);
                    float3 queryPoint = hit.Position + hit.SurfaceNormal * hit.Distance;

                    PhysicsDebugDisplaySystem.Point(hit.Position, 0.02f, Unity.DebugDisplay.ColorIndex.White);
                    PhysicsDebugDisplaySystem.Point(queryPoint, 0.02f, Unity.DebugDisplay.ColorIndex.White);
                    PhysicsDebugDisplaySystem.Line(hit.Position, queryPoint, Unity.DebugDisplay.ColorIndex.Magenta);

                    if (queryData.DrawSurfaceNormal)
                    {
                        PhysicsDebugDisplaySystem.Line(hit.Position, hit.Position + hit.SurfaceNormal, Unity.DebugDisplay.ColorIndex.Green);
                    }

                    if (queryData.HighlightLeafCollider && !hit.ColliderKey.Equals(ColliderKey.Empty))
                    {
                        DrawLeafCollider(world.Bodies[hit.RigidBodyIndex], hit.ColliderKey);

                        // Need to fix this once Unity.DebugDisplay.Label starts working and is exposed in PhysicsDebugDisplaySystem API [Havok-275]
                        //GUIStyle style = new GUIStyle();
                        //style.normal.textColor = Color.yellow;
                        //Handles.Label(hit.Position, hit.ColliderKey.Value.ToString("X8"), style);
                    }

                    // Need to fix this once Unity.DebugDisplay.Label starts working and is exposed in PhysicsDebugDisplaySystem API [Havok-275]
//#if UNITY_EDITOR
//                    if (queryData.ColliderQuery && queryData.HighlightLeafCollider && queryData.Collider.Value.CollisionType != CollisionType.Convex && !hit.QueryColliderKey.Equals(ColliderKey.Empty))
//                    {
//                        float3 flippedPosition = hit.Position + hit.SurfaceNormal * hit.Fraction;

//                        GUIStyle style = new GUIStyle();
//                        style.normal.textColor = Color.yellow;
//                        Handles.Label(flippedPosition, hit.QueryColliderKey.Value.ToString("X8"), style);
//                    }
//#endif
                }
            }
        }

        void DrawLeafCollider(RigidBody body, ColliderKey key)
        {
            unsafe
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

                        PhysicsDebugDisplaySystem.Line(v0, v1, Unity.DebugDisplay.ColorIndex.Yellow);
                        PhysicsDebugDisplaySystem.Line(v1, v2, Unity.DebugDisplay.ColorIndex.Yellow);

                        if (polygon->IsTriangle)
                        {
                            PhysicsDebugDisplaySystem.Line(v2, v0, Unity.DebugDisplay.ColorIndex.Yellow);
                        }
                        else
                        {
                            PhysicsDebugDisplaySystem.Line(v2, v3, Unity.DebugDisplay.ColorIndex.Yellow);
                            PhysicsDebugDisplaySystem.Line(v3, v0, Unity.DebugDisplay.ColorIndex.Yellow);
                        }
                    }
                }
            }
        }

        #endregion

        #region Jobs

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

        #endregion

        #region Creation
        private void CreateCollider(QueryData queryData)
        {
            int numMeshes = 1;

            if (queryData.ColliderType == ColliderType.Compound)
            {
                numMeshes = 2;
                queryData.ChildrenColliders = new BlobAssetReference<Collider>[2];
            }

            queryData.ColliderMeshes = new UnityEngine.Mesh[numMeshes];

            BlobAssetReference<Collider> collider = default;

            switch (queryData.ColliderType)
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
                    queryData.ChildrenColliders[0] = child1;

                    var child2 = BoxCollider.Create(new BoxGeometry
                    {
                        Center = float3.zero,
                        Orientation = quaternion.identity,
                        Size = new float3(1.0f),
                        BevelRadius = 0.0f
                    });
                    queryData.ChildrenColliders[1] = child2;

                    NativeArray<CompoundCollider.ColliderBlobInstance> childrenBlobs = new NativeArray<CompoundCollider.ColliderBlobInstance>(2, Allocator.TempJob);
                    childrenBlobs[0] = new CompoundCollider.ColliderBlobInstance
                    {
                        Collider = child1,
                        CompoundFromChild = new RigidTransform
                        {
                            pos = new float3(0.5f, 0, 0),
                            rot = quaternion.identity
                        }
                    };

                    childrenBlobs[1] = new CompoundCollider.ColliderBlobInstance
                    {
                        Collider = child2,
                        CompoundFromChild = new RigidTransform
                        {
                            pos = new float3(-0.5f, 0, 0),
                            rot = quaternion.identity
                        }
                    };

                    queryData.ColliderMeshes[0] = SceneCreationUtilities.CreateMeshFromCollider(child1);
                    queryData.ColliderMeshes[1] = SceneCreationUtilities.CreateMeshFromCollider(child2);

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

            if (queryData.ColliderType != ColliderType.Compound)
            {
                queryData.ColliderMeshes[0] = SceneCreationUtilities.CreateMeshFromCollider(collider);
            }

            queryData.Collider = collider;
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
        #endregion
    }
}
