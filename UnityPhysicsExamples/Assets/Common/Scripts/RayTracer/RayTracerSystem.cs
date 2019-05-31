using System.Collections.Generic;
using Unity.Physics.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.Physics.Extensions
{
    [UpdateAfter(typeof(BuildPhysicsWorld))]
    public class RayTracerSystem : JobComponentSystem
    {
        BuildPhysicsWorld m_BuildPhysicsWorldSystem;

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
            public BlockStream PixelData;
        }

        public RayResult AddRequest(RayRequest req)
        {
            int numWorkItems = 5;
            RayResult res = new RayResult { PixelData = new BlockStream(numWorkItems, 0xa1070b6d) };
            m_Requests.Add(req);
            m_Results.Add(res);
            return res;
        }

        List<RayRequest> m_Requests;
        List<RayResult> m_Results;

        public bool IsEnabled => m_Requests != null;

        [BurstCompile]
        protected struct RaycastJob : IJobParallelFor
        {
            public BlockStream.Writer Results;
            public RayRequest Request;
            [ReadOnly] public CollisionWorld World;
            public int NumDynamicBodies;

            public unsafe void Execute(int index)
            {
                Results.BeginForEachIndex(index);
                int numRows = (Request.ImageResolution + Results.ForEachCount - 1) / Results.ForEachCount;

                const float sphereRadius = 0.005f;
                BlobAssetReference<Collider> sphere;
                if (Request.CastSphere)
                {
                    sphere = SphereCollider.Create(float3.zero, sphereRadius, Request.CollisionFilter);
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
                            var input = new ColliderCastInput
                            {
                                Collider = (Collider*)sphere.GetUnsafePtr(),
                                Orientation = quaternion.identity,
                                Start = Request.PinHole,
                                End = Request.PinHole + rayDir
                            };
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
                                Collider* collider = World.Bodies[hit.RigidBodyIndex].Collider;
                                hit.ColliderKey.PopSubKey(collider->NumColliderKeyBits, out uint key);
                                if (key % 2 == 0)
                                {
                                    Color.RGBToHSV(hitColor, out float h, out float s, out float v);
                                    hitColor = Color.HSVToRGB(h, s, v + 0.25f);
                                }
                            }

                            if (Request.Shadows)
                            {
                                float3 hitPos = Request.PinHole + rayDir * hit.Fraction + hit.SurfaceNormal * 0.001f;
                                bool shadowHit = false;

                                if (Request.CastSphere)
                                {
                                    var start = hitPos + hit.SurfaceNormal * sphereRadius;
                                    var input = new ColliderCastInput
                                    {
                                        Collider = (Collider*)sphere.GetUnsafePtr(),
                                        Orientation = quaternion.identity,
                                        Start = start,
                                        End = start + (Request.LightDir * Request.RayLength),
                                    };
                                    ColliderCastHit colliderHit;
                                    shadowHit = World.CastCollider(input, out colliderHit);
                                }
                                else
                                {
                                    var rayCastInput = new RaycastInput
                                    {
                                        Start = hitPos,
                                        End = hitPos + (Request.LightDir * Request.RayLength),
                                        Filter = Request.CollisionFilter
                                    };
                                    RaycastHit shadowOutput;
                                    shadowHit = World.CastRay(rayCastInput, out shadowOutput);
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

                Results.EndForEachIndex();
            }
        }

        protected override void OnCreate()
        {
            m_BuildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
            m_Requests = new List<RayRequest>();
            m_Results = new List<RayResult>();
        }
        
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_Requests == null || m_Requests.Count == 0)
            {
                return inputDeps;
            }

            inputDeps = JobHandle.CombineDependencies(inputDeps, m_BuildPhysicsWorldSystem.FinalJobHandle);

            JobHandle combinedJobs = inputDeps;
            for (int i = 0; i < m_Requests.Count; i++)
            {
                JobHandle rcj = new RaycastJob
                {
                    Results = m_Results[0].PixelData,
                    Request = m_Requests[0],
                    World = m_BuildPhysicsWorldSystem.PhysicsWorld.CollisionWorld,
                    NumDynamicBodies = m_BuildPhysicsWorldSystem.PhysicsWorld.NumDynamicBodies
                }.Schedule(m_Results[0].PixelData.ForEachCount, 1, inputDeps);
                rcj.Complete(); //<todo.eoin How can we properly wait on this task when reading results?
                combinedJobs = JobHandle.CombineDependencies(combinedJobs, rcj);
            }

            m_Requests.Clear();
            m_Results.Clear();

            return combinedJobs;
        }
    }
}
