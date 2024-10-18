using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
partial struct ColliderBakeTransformSystem : ISystem
{
    private NativeQueue<BlobAssetReference<Unity.Physics.Collider>> m_ColliderBlobsToDisposeNow;

    [BurstCompile]
    public partial struct BakeTransformJob : IJobEntity
    {
        public float TimeStep;
        public NativeQueue<BlobAssetReference<Unity.Physics.Collider>>.ParallelWriter ColliderBlobsToDisposeNow;

        void Execute(ref ColliderBakeTransform transformData, ref SaveColliderBlobForDisposal saveCollider, ref PhysicsCollider collider, ref PhysicsMass mass,
            ref PostTransformMatrix postTransformMatrix, Entity entity, [ChunkIndexInQuery] int chunkIndex)
        {
            // Collider should have been made unique before this job runs
            if (!collider.IsUnique)
                return;

            // Skip if the transformation has already been applied to the collider previously and no animation is requested
            if (transformData.FrameCount > 0 && transformData.AnimationDuration <= 0)
                return;

            // If both drift prevention and the collider baking animation is enabled,
            // store some data for later collider reset if the drift threshold has been reached.
            if (transformData.DriftPrevention && transformData.AnimationDuration > 0)
            {
                // store original geometry data for later reset if geometry drifts when the animation is reset, and
                // replace baked collider with guaranteed unique clone right away.
                if (transformData.FrameCount == 0 && !transformData.OriginalCollider.IsCreated)
                {
                    transformData.OriginalCollider = collider.Value;
                    collider.Value = transformData.OriginalCollider.Value.Clone();
                    saveCollider.Collider = collider.Value;

                    transformData.OriginalPostTransformMatrix = postTransformMatrix;
                }
            }

            var animationFactor = 1f;
            if (transformData.AnimationDuration > 0)
            {
                var animationFrames = math.ceil(transformData.AnimationDuration / TimeStep);
                if (transformData.FrameCount >= animationFrames)
                {
                    transformData.FrameCount = 0;
                    if (transformData.DriftPrevention)
                    {
                        var lengthSq = math.lengthsq(postTransformMatrix.Value.c0)
                            + math.lengthsq(postTransformMatrix.Value.c1)
                            + math.lengthsq(postTransformMatrix.Value.c2)
                            + math.lengthsq(postTransformMatrix.Value.c3);

                        var lengthSqOrig = math.lengthsq(transformData.OriginalPostTransformMatrix.Value.c0)
                            + math.lengthsq(transformData.OriginalPostTransformMatrix.Value.c1)
                            + math.lengthsq(transformData.OriginalPostTransformMatrix.Value.c2)
                            + math.lengthsq(transformData.OriginalPostTransformMatrix.Value.c3);

                        if (math.abs(lengthSq - lengthSqOrig) > transformData.DriftErrorThreshold)
                        {
                            var driftedCollider = collider.Value;

                            //clone and store the blob in save for disposal, in order to dispose at the end of the frame
                            collider.Value = transformData.OriginalCollider.Value.Clone();
                            saveCollider.Collider = collider.Value;

                            // We can't dispose the blob in Collider.value yet because it may be needed by the
                            // DebugDraw system. Instead, add it to a queue to be disposed of next frame.
                            ColliderBlobsToDisposeNow.Enqueue(driftedCollider);

                            postTransformMatrix = transformData.OriginalPostTransformMatrix;
                        }
                    }
                }

                // Normalize animation factor considering the sum of the animation function weights over the animation duration.
                // Here, we are using an identity of the discrete sum of sines.
                var N = math.ceil(animationFrames / 2);
                var d = math.PI / N;
                var s = math.sin(0.5f * d);
                var o_t = 1f;
                if (math.abs(s) > math.EPSILON)
                {
                    var R = math.sin(N * 0.5f * d) / s;
                    o_t = R * math.sin((N - 1) * 0.5f * d);
                }

                float o = math.sin(2f * math.PI * (transformData.FrameCount / animationFrames));
                animationFactor = o / math.abs(o_t);
            }

            ++transformData.FrameCount;

            if (math.abs(animationFactor) < math.EPSILON)
            {
                return;
            }

            var deltaScale = transformData.Scale - 1f;

            // Compute the affine transformation from the translation, rotation, scale and shear provided in the baking data.
            var bakeTransform = new AffineTransform(
                animationFactor * transformData.Translation,
                math.slerp(math.conjugate(transformData.Rotation), transformData.Rotation,
                    (animationFactor + 1f) / 2f),
                1 + animationFactor * deltaScale);

            float3x3 shearXZ, shearYZ;
            var shearXY = shearXZ = shearYZ = float3x3.identity;

            shearXY[2][0] = animationFactor * transformData.ShearXY.x;
            shearXY[2][1] = animationFactor * transformData.ShearXY.y;
            shearXZ[1][0] = animationFactor * transformData.ShearXZ.x;
            shearXZ[1][2] = animationFactor * transformData.ShearXZ.y;
            shearYZ[0][1] = animationFactor * transformData.ShearYZ.x;
            shearYZ[0][2] = animationFactor * transformData.ShearYZ.y;

            bakeTransform = math.mul(bakeTransform, math.mul(shearXY, math.mul(shearXZ, shearYZ)));

            // Apply the affine transformation to the collider geometry.
            collider.Value.Value.BakeTransform(bakeTransform);

            // Update the rigid body's mass properties for if available and dynamic by copying the
            // new, modified collider's mass properties into the PhysicsMass component.
            if (!mass.IsKinematic)
            {
                var massProperties = collider.MassProperties;
                mass.Transform = massProperties.MassDistribution.Transform;
                mass.InverseInertia = math.rcp(massProperties.MassDistribution.InertiaTensor);
                mass.AngularExpansionFactor = massProperties.AngularExpansionFactor;
            }

            // Apply bake transform also to the PostTransformMatrix to affect the visuals.
            postTransformMatrix = new PostTransformMatrix
            {
                Value = math.mul((float4x4)bakeTransform, postTransformMatrix.Value)
            };
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ColliderBakeTransform>();
        m_ColliderBlobsToDisposeNow = new NativeQueue<BlobAssetReference<Unity.Physics.Collider>>(Allocator.Persistent);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        using var ecb = new EntityCommandBuffer(Allocator.Temp);
        var dt = SystemAPI.Time.DeltaTime;

        // Make sure all colliders we want to apply transformations to have a PostTransformMatrix component, so that
        // we can also affect their visuals.
        foreach (var(scaleAndShearData, collider, entity) in SystemAPI
                 .Query<ColliderBakeTransform, RefRW<PhysicsCollider>>()
                 .WithNone<PostTransformMatrix>()
                 .WithEntityAccess())
        {
            ecb.AddComponent(entity, new PostTransformMatrix { Value = float4x4.identity });
            ecb.AddComponent(entity, new SaveColliderBlobForDisposal
            {
                Collider = BlobAssetReference<Unity.Physics.Collider>.Null
            });

            if (!collider.ValueRO.IsUnique)
            {
                collider.ValueRW.MakeUnique(entity, ecb);
            }
        }

        ecb.Playback(state.EntityManager);

        var disposeJobHandle = new DisposeJob()
        {
            DisposeNow = m_ColliderBlobsToDisposeNow
        }.Schedule(state.Dependency);

        // Perform collider transform baking on NON-STATIC bodies with unique colliders
        state.Dependency = new BakeTransformJob()
        {
            TimeStep = dt,
            ColliderBlobsToDisposeNow = m_ColliderBlobsToDisposeNow.AsParallelWriter()
        }.ScheduleParallel(disposeJobHandle);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        using var ecb = new EntityCommandBuffer(Allocator.Temp);
        // Clean up any saved collider blobs that haven't been disposed of yet via the m_ColliderBlobsToDisposeNow queue
        foreach (var(saveCollider, entity) in
                 SystemAPI.Query<RefRW<SaveColliderBlobForDisposal>>().WithEntityAccess())
        {
            //dispose the latest collider clone inside our bake data component
            if (saveCollider.ValueRO.Collider.IsCreated)
            {
                saveCollider.ValueRW.Collider.Dispose();
                ecb.RemoveComponent<SaveColliderBlobForDisposal>(entity);
            }
        }
        ecb.Playback(state.EntityManager);

        // Disposes blobs from clone of transformData.OriginalCollider that are used in the animation drift reset
        while (!m_ColliderBlobsToDisposeNow.IsEmpty())
        {
            m_ColliderBlobsToDisposeNow.Dequeue().Dispose();
        }

        m_ColliderBlobsToDisposeNow.Dispose();
    }

    private struct DisposeJob : IJob
    {
        public NativeQueue<BlobAssetReference<Unity.Physics.Collider>> DisposeNow;
        public void Execute()
        {
            while (!DisposeNow.IsEmpty())
            {
                DisposeNow.Dequeue().Dispose();
            }
        }
    }
}
