using System.Collections.Generic;
using Unity.Physics.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Assertions;

namespace Unity.Physics.Extensions
{
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateAfter(typeof(PhysicsInitializeGroup))]
    public partial class RayTracerSystem : SystemBase
    {
        internal JobHandle FinalJobHandle => Dependency;
        internal bool HasRequests => m_Requests.Count > 0;

        public struct RayRequest
        {
            public float3 PinHole;
            public float3 ImageCenter;
            public float3 Up;
            public float3 Right;
            public float3 LightDir;
            public float RayLength;
            public float PlaneHalfExtents;
            public float AmbientLight;
            public int ImageResolution;
            public bool AlternateKeys;
            public bool CastSphere;
            public bool Shadows;
            public CollisionFilter CollisionFilter;
        }

        public struct RayResult
        {
            public NativeStream PixelData;
            internal bool m_Assigned;
            internal JobHandle m_JobHandle;

            public void Dispose()
            {
                if (PixelData.IsCreated)
                    PixelData.Dispose();

                //reset isCreated
                PixelData = default;
                m_Assigned = false;
                m_JobHandle = default;
            }
        }

        public int AddRequest(RayRequest req)
        {
            int index = -1;
            // Find an empty slot?
            for (int i = 0; i < m_Results.Count; i++)
            {
                if (!m_Results[i].PixelData.IsCreated)
                {
                    index = i;
                }
            }
            // None found so add a new one.
            if (index == -1)
            {
                m_Requests.Add(default);
                m_Results.Add(default);
                index = m_Results.Count - 1;
            }
            m_Requests[index] = req;
            m_Results[index] = new RayResult { PixelData = new NativeStream(5, Allocator.TempJob) };
            return index;
        }

        public bool DisposeRequest(int index)
        {
            Assert.IsTrue(0 <= index && index < m_Results.Count);

            if (m_Results[index].PixelData.IsCreated)
            {
                // Mark slot as unused
                m_Results[index].m_JobHandle.Complete();
                m_Results[index].Dispose();
                m_Results[index] = default;
                return true;
            }

            return false;
        }

        public bool TryGetResults(int index, out NativeStream.Reader results)
        {
            Assert.IsTrue(0 <= index && index < m_Results.Count);

            var res = m_Results[index];
            if (res.m_Assigned && res.m_JobHandle.IsCompleted && res.PixelData.IsCreated)
            {
                results = m_Results[index].PixelData.AsReader();
                return true;
            }

            results = default;
            return false;
        }

        List<RayRequest> m_Requests;
        List<RayResult> m_Results;

        public bool IsEnabled => m_Requests != null;

        [BurstCompile]
        protected struct RaycastJob : IJobParallelFor
        {
            public NativeStream.Writer Results;
            public RayRequest Request;
            [ReadOnly] public CollisionWorld World;
            public int NumDynamicBodies;

            public void Execute(int index)
            {
                Results.BeginForEachIndex(index);
                int numRows = (Request.ImageResolution + Results.ForEachCount - 1) / Results.ForEachCount;

                const float sphereRadius = 0.005f;
                BlobAssetReference<Collider> sphere = default;
                if (Request.CastSphere)
                {
                    sphere = SphereCollider.Create(new SphereGeometry
                    {
                        Center = float3.zero,
                        Radius = sphereRadius
                    }, Request.CollisionFilter);
                }

                for (int yCoord = index * numRows; yCoord < math.min(Request.ImageResolution, (index + 1) * numRows); yCoord++)
                {
                    for (int xCoord = 0; xCoord < Request.ImageResolution; xCoord++)
                    {
                        float xFrac = 2.0f * ((xCoord / (float)Request.ImageResolution) - 0.5f);
                        float yFrac = 2.0f * ((yCoord / (float)Request.ImageResolution) - 0.5f);

                        float3 targetImagePlane = Request.ImageCenter + Request.Up * Request.PlaneHalfExtents * yFrac + Request.Right * Request.PlaneHalfExtents * xFrac;
                        float3 rayDir = Request.RayLength * (Request.PinHole - targetImagePlane);

                        RaycastHit hit;
                        bool hasHit;
                        if (Request.CastSphere)
                        {
                            var input = new ColliderCastInput(sphere, Request.PinHole, Request.PinHole + rayDir);
                            hasHit = World.CastCollider(input, out ColliderCastHit colliderHit);
                            hit = new RaycastHit
                            {
                                Fraction = colliderHit.Fraction,
                                Position = colliderHit.Position,
                                SurfaceNormal = colliderHit.SurfaceNormal,
                                RigidBodyIndex = colliderHit.RigidBodyIndex,
                                ColliderKey = colliderHit.ColliderKey
                            };
                        }
                        else
                        {
                            var rayCastInput = new RaycastInput
                            {
                                Start = Request.PinHole,
                                End = Request.PinHole + rayDir,
                                Filter = Request.CollisionFilter
                            };
                            hasHit = World.CastRay(rayCastInput, out hit);
                        }

                        Color hitColor = Color.black;
                        if (hasHit)
                        {
                            if (hit.RigidBodyIndex < NumDynamicBodies)
                            {
                                hitColor = Color.yellow;
                            }
                            else
                            {
                                hitColor = Color.grey;
                            }

                            // Lighten alternate keys
                            if (Request.AlternateKeys && !hit.ColliderKey.Equals(ColliderKey.Empty))
                            {
                                hit.ColliderKey.PopSubKey(World.Bodies[hit.RigidBodyIndex].Collider.Value.NumColliderKeyBits, out uint key);
                                if (key % 2 == 0)
                                {
                                    Color.RGBToHSV(hitColor, out float h, out float s, out float v);
                                    hitColor = Color.HSVToRGB(h, s, v + 0.25f);
                                }
                            }

                            if (Request.Shadows)
                            {
                                float3 hitPos = Request.PinHole + rayDir * hit.Fraction + hit.SurfaceNormal * 0.001f;
                                bool shadowHit;

                                if (Request.CastSphere)
                                {
                                    var start = hitPos + hit.SurfaceNormal * sphereRadius;
                                    var input = new ColliderCastInput(sphere, start, start + (Request.LightDir * Request.RayLength));
                                    shadowHit = World.CastCollider(input);
                                }
                                else
                                {
                                    var rayCastInput = new RaycastInput
                                    {
                                        Start = hitPos,
                                        End = hitPos + (Request.LightDir * Request.RayLength),
                                        Filter = Request.CollisionFilter
                                    };
                                    shadowHit = World.CastRay(rayCastInput);
                                }

                                if (shadowHit)
                                {
                                    hitColor *= 0.4f;
                                }
                            }
                        }

                        float lighting = math.min(1.0f, math.max(Request.AmbientLight, Vector3.Dot(hit.SurfaceNormal, Request.LightDir)));

                        Results.Write(xCoord);
                        Results.Write(yCoord);
                        Results.Write(hitColor * lighting);
                    }
                }

                if (sphere.IsCreated)
                    sphere.Dispose();

                Results.EndForEachIndex();
            }
        }

        protected override void OnCreate()
        {
            m_Requests = new List<RayRequest>();
            m_Results = new List<RayResult>();
        }

        protected override void OnDestroy()
        {
            for (int i = 0; i < m_Results.Count; ++i)
                m_Results[i].Dispose();

            m_Requests.Clear();
            m_Results.Clear();
        }

        protected override void OnUpdate()
        {
            if (m_Requests == null || m_Requests.Count == 0) return;

            var world = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
            JobHandle combinedJobs = Dependency;
            for (int i = 0; i < m_Requests.Count; i++)
            {
                if (!m_Results[i].m_Assigned)
                {
                    JobHandle rcj = new RaycastJob
                    {
                        Results = m_Results[i].PixelData.AsWriter(),
                        Request = m_Requests[i],
                        World = world.CollisionWorld,
                        NumDynamicBodies = world.NumDynamicBodies
                    }.Schedule(m_Results[i].PixelData.ForEachCount, 1, Dependency);

                    combinedJobs = JobHandle.CombineDependencies(combinedJobs, rcj);

                    var res = m_Results[i];
                    res.m_JobHandle = rcj;
                    res.m_Assigned = true;
                    m_Results[i] = res;
                }
            }

            Dependency = combinedJobs;
        }
    }
}
