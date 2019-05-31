using System;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Physics.Authoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Physics.Extensions
{
    /// Simple Behaviour for testing broadphase raycasts. Provides a 
    /// gizmo which can be manipulated to cast a ray during simulation.
    /// Displays hit positions or the tested line segment.
    public unsafe class QueryTester : MonoBehaviour
    {
        private BlobAssetReference<Collider> CreateCollider(UnityEngine.Mesh mesh, ColliderType type)
        {
            switch (type)
            {
                case ColliderType.Sphere:
                {
                    Bounds bounds = mesh.bounds;
                    return SphereCollider.Create(bounds.center, math.cmax(bounds.extents));
                }
                case ColliderType.Triangle:
                {
                    return PolygonCollider.CreateTriangle(mesh.vertices[0], mesh.vertices[1], mesh.vertices[2]);
                }
                case ColliderType.Quad:
                {
                    // We assume the first 2 triangles of the mesh are a quad with a shared edge
                    // Work out a correct ordering for the triangle
                    int[] orderedIndices = new int[4];

                    // Find the vertex in first triangle that is not on the shared edge
                    for (int i = 0; i < 3; i++)
                    {
                        if ((mesh.triangles[i] != mesh.triangles[3]) &&
                            (mesh.triangles[i] != mesh.triangles[4]) &&
                            (mesh.triangles[i] != mesh.triangles[5]))
                        {
                            // Push in order or prev, unique, next
                            orderedIndices[0] = mesh.triangles[(i - 1 + 3) % 3];
                            orderedIndices[1] = mesh.triangles[i];
                            orderedIndices[2] = mesh.triangles[(i + 1) % 3];
                            break;
                        }
                    }

                    // Find the vertex in second triangle that is not on a shared edge
                    for (int i = 3; i < 6; i++)
                    {
                        if ((mesh.triangles[i] != orderedIndices[0]) &&
                            (mesh.triangles[i] != orderedIndices[1]) &&
                            (mesh.triangles[i] != orderedIndices[2]))
                        {
                            orderedIndices[3] = mesh.triangles[i];
                            break;
                        }
                    }

                    return PolygonCollider.CreateQuad(
                        mesh.vertices[orderedIndices[0]],
                        mesh.vertices[orderedIndices[1]],
                        mesh.vertices[orderedIndices[2]],
                        mesh.vertices[orderedIndices[3]]);
                }
                case ColliderType.Box:
                {
                    Bounds bounds = mesh.bounds;
                    return BoxCollider.Create(bounds.center, quaternion.identity, 2.0f * bounds.extents, 0.0f);
                }
                case ColliderType.Capsule:
                {
                    Bounds bounds = mesh.bounds;
                    float min = math.cmin(bounds.extents);
                    float max = math.cmax(bounds.extents);
                    int x = math.select(math.select(2, 1, min == bounds.extents.y), 0, min == bounds.extents.x);
                    int z = math.select(math.select(2, 1, max == bounds.extents.y), 0, max == bounds.extents.x);
                    int y = math.select(math.select(2, 1, (1 != x) && (1 != z)), 0, (0 != x) && (0 != z));
                    float radius = bounds.extents[y];
                    float3 vertex0 = bounds.center; vertex0[z] = -(max - radius);
                    float3 vertex1 = bounds.center; vertex1[z] = (max - radius);
                    return CapsuleCollider.Create(vertex0, vertex1, radius);
                }
                case ColliderType.Cylinder:
                    // TODO: need someone to add
                    throw new NotImplementedException();
                case ColliderType.Convex:
                {
                    NativeArray<float3> points = new NativeArray<float3>(mesh.vertices.Length, Allocator.Temp);
                    for (int i = 0; i < mesh.vertices.Length; i++)
                    {
                        points[i] = mesh.vertices[i];
                    }
                    return ConvexCollider.Create(points, 0.0f);
                }
                default:
                    throw new System.NotImplementedException();
            }
        }

        void Start()
        {
            Simulating = true;

            if (ColliderMesh != null)
            {
                Collider = CreateCollider(ColliderMesh, ColliderType);
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
        }

        void RunQueries()
        {
            ref PhysicsWorld world = ref Entities.World.Active.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;

            float3 origin = transform.position;
            float3 direction = (transform.rotation * Direction) * Distance;

            RaycastHits.Clear();
            ColliderCastHits.Clear();
            DistanceHits.Clear();

            if (!ColliderMesh)
            {
                if (math.any(new float3(Direction) != float3.zero))
                {
                    RaycastInput = new RaycastInput
                    {
                        Start = origin,
                        End = origin + direction,
                        Filter = CollisionFilter.Default
                    };

                    if (CollectAllHits)
                    {
                        world.CastRay(RaycastInput, ref RaycastHits);
                    }
                    else if (world.CastRay(RaycastInput, out RaycastHit hit))
                    {
                        RaycastHits.Add(hit);
                    }
                }
                else
                {
                    PointDistanceInput = new PointDistanceInput
                    {
                        Position = origin,
                        MaxDistance = Distance,
                        Filter = CollisionFilter.Default
                    };

                    if (CollectAllHits)
                    {
                        world.CalculateDistance(PointDistanceInput, ref DistanceHits);
                    }
                    else if (world.CalculateDistance(PointDistanceInput, out DistanceHit hit))
                    {
                        DistanceHits.Add(hit);
                    }
                }
            }
            else //(ColliderMesh)
            {
                if (math.any(new float3(Direction) != float3.zero))
                {
                    ColliderCastInput = new ColliderCastInput
                    {
                        Collider = (Collider*)(Collider.GetUnsafePtr()),
                        Orientation = transform.rotation,
                        Start = origin,
                        End = origin + direction,
                    };

                    if (CollectAllHits)
                    {
                        world.CastCollider(ColliderCastInput, ref ColliderCastHits);
                    }
                    else if (world.CastCollider(ColliderCastInput, out ColliderCastHit hit))
                    {
                        ColliderCastHits.Add(hit);
                    }
                }
                else
                {
                    ColliderDistanceInput = new ColliderDistanceInput
                    {
                        Collider = (Collider*)(Collider.GetUnsafePtr()),
                        Transform = new RigidTransform(transform.rotation, origin),
                        MaxDistance = Distance
                    };

                    if (CollectAllHits)
                    {
                        world.CalculateDistance(ColliderDistanceInput, ref DistanceHits);
                    }
                    else if (world.CalculateDistance(ColliderDistanceInput, out DistanceHit hit))
                    {
                        DistanceHits.Add(hit);
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
        public UnityEngine.Mesh ColliderMesh = null;

        protected bool Simulating;
        protected RaycastInput RaycastInput;
        protected NativeList<RaycastHit> RaycastHits;
        protected ColliderCastInput ColliderCastInput;
        protected NativeList<ColliderCastHit> ColliderCastHits;
        protected PointDistanceInput PointDistanceInput;
        protected ColliderDistanceInput ColliderDistanceInput;
        protected NativeList<DistanceHit> DistanceHits;
        protected BlobAssetReference<Collider> Collider;

        void OnDrawGizmos()
        {
            if (Simulating)
            {
                RunQueries();

                ref PhysicsWorld world = ref Entities.World.Active.GetExistingSystem<BuildPhysicsWorld>().PhysicsWorld;

                // Draw the query
                Gizmos.color = new Color(0.94f, 0.35f, 0.15f, 0.75f);
                if (ColliderMesh)
                {
                    if (math.any(new float3(Direction) != float3.zero))
                    {
                        Gizmos.DrawRay(ColliderCastInput.Start, ColliderCastInput.End - ColliderCastInput.Start);
                        Gizmos.DrawWireMesh(ColliderMesh, ColliderCastInput.Start, ColliderCastInput.Orientation);
                    }
                    else
                    {
                        Gizmos.DrawWireMesh(ColliderMesh, ColliderDistanceInput.Transform.pos, ColliderDistanceInput.Transform.rot);
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
                    foreach (RaycastHit hit in RaycastHits.ToArray())
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
                    foreach (ColliderCastHit hit in ColliderCastHits.ToArray())
                    {
                        Assert.IsTrue(hit.RigidBodyIndex >= 0 && hit.RigidBodyIndex < world.NumBodies);
                        Assert.IsTrue(math.abs(math.lengthsq(hit.SurfaceNormal) - 1.0f) < 0.01f);

                        Gizmos.color = Color.magenta;
                        Gizmos.DrawSphere(hit.Position, 0.02f);
                        Gizmos.DrawWireMesh(ColliderMesh, math.lerp(ColliderCastInput.Start, ColliderCastInput.End, hit.Fraction), ColliderCastInput.Orientation);

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

                // Draw distance hits
                if (DistanceHits.IsCreated)
                {
                    foreach (DistanceHit hit in DistanceHits.ToArray())
                    {
                        Assert.IsTrue(hit.RigidBodyIndex >= 0 && hit.RigidBodyIndex < world.NumBodies);
                        Assert.IsTrue(math.abs(math.lengthsq(hit.SurfaceNormal) - 1.0f) < 0.01f);

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
            if (body.Collider->GetLeaf(key, out ChildCollider leaf) && (leaf.Collider == null))
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
