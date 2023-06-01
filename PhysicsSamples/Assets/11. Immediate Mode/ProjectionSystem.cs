using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace ImmediateMode
{
    // Project ball paths into the future given current cue ball shot angle and power.
    [RequireMatchingQueriesForUpdate]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial struct ProjectionSystem : ISystem
    {
        public NativeArray<float3> positions;
        public PhysicsWorld physicsWorld;
        public ImmediatePhysicsWorldStepper immediatePhysicsStepper;

        // An ISystem cannot have managed fields, but we can work around this by
        // storing managed fields in a class component.
        public class Managed : IComponentData
        {
            public RenderMeshArray ProjectionMaterial;
        }

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Config>();
            state.RequireForUpdate<CueBall>();
            state.RequireForUpdate<Shot>();
            state.RequireForUpdate<BuildPhysicsWorldData>();
            state.RequireForUpdate<PhysicsWorldSingleton>();

            state.EntityManager.AddComponentObject(state.SystemHandle, new Managed());
        }

        public void OnDestroy(ref SystemState state)
        {
            state.CompleteDependency();

            if (positions.IsCreated)
            {
                positions.Dispose();
            }

            if (physicsWorld.NumBodies != 0)
            {
                physicsWorld.Dispose();
            }

            if (immediatePhysicsStepper.Created)
            {
                immediatePhysicsStepper.Dispose();
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            // Make PhysicsWorld safe to read
            // Complete the projections from the previous step.
            state.Dependency.Complete();

            var configQuery = SystemAPI.QueryBuilder().WithAll<Config>().Build();
            var config = configQuery.GetSingleton<Config>();
            var managed = state.EntityManager.GetComponentObject<Managed>(state.SystemHandle);
            var cueBallEntity = SystemAPI.GetSingletonEntity<CueBall>();
            var physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

            if (!immediatePhysicsStepper.Created)
            {
                physicsWorld = new PhysicsWorld();
                managed.ProjectionMaterial = new RenderMeshArray(new[] { config.Material }, new[] { config.Mesh });
                immediatePhysicsStepper = ImmediatePhysicsWorldStepper.Create();
                CreateProjectionEntities(ref state, config, managed.ProjectionMaterial, physicsWorldSingleton.NumDynamicBodies,
                    cueBallEntity);
            }

            for (int i = 0; i < positions.Length; i++)
            {
                // Moves the lines out of view inside the table
                positions[i] = new float3(0, -1, 0);
            }

            if (physicsWorld.NumBodies > 0)
            {
                physicsWorld.Dispose();
            }

            physicsWorld = physicsWorldSingleton.Clone();

            PhysicsStep physicsStep = PhysicsStep.Default;
            if (SystemAPI.HasSingleton<PhysicsStep>())
            {
                physicsStep = SystemAPI.GetSingleton<PhysicsStep>();
            }

            var shot = SystemAPI.GetSingletonRW<Shot>();
            if (shot.ValueRO.TakeShot)
            {
                var velocity = SystemAPI.GetComponentRW<PhysicsVelocity>(cueBallEntity);
                velocity.ValueRW.Linear = shot.ValueRW.Velocity;
                shot.ValueRW.TakeShot = false;
            }

            physicsWorld.SetLinearVelocity(physicsWorld.GetRigidBodyIndex(cueBallEntity), shot.ValueRW.Velocity);

            var stepInput = new SimulationStepInput
            {
                World = physicsWorld,
                TimeStep = SystemAPI.Time.DeltaTime,
                NumSolverIterations = physicsStep.SolverIterationCount,
                SolverStabilizationHeuristicSettings = physicsStep.SolverStabilizationHeuristicSettings,
                Gravity = physicsStep.Gravity,
                SynchronizeCollisionWorld = true,
                HaveStaticBodiesChanged = SystemAPI.GetSingleton<BuildPhysicsWorldData>().HaveStaticBodiesChanged
            };

            // Sync the CollisionWorld before the initial step.
            // As stepInput.SynchronizeCollisionWorld is true the simulation will
            // automatically sync the CollisionWorld on subsequent steps.
            // This is only needed as we have modified the cue ball velocity.
            physicsWorld.CollisionWorld.ScheduleUpdateDynamicTree(
                ref physicsWorld, stepInput.TimeStep, stepInput.Gravity, default, false)
                .Complete();

            // Step the local world
            for (int i = 0; i < config.NumSteps; i++)
            {
                if (physicsStep.SimulationType == SimulationType.UnityPhysics)
                {
                    // TODO: look into a public version of SimulationContext.ScheduleReset
                    // so that we can chain multiple StepLJob instances.

                    // Dispose and reallocate input velocity buffer, if dynamic body count has increased.
                    // Dispose previous collision and trigger event streams and allocator new streams.
                    immediatePhysicsStepper.SimulationContext.Reset(stepInput);

                    new StepJob()
                    {
                        StepInput = stepInput,
                        SimulationContext = immediatePhysicsStepper.SimulationContext,
                        StepIndex = i,
                        NumSteps = config.NumSteps,
                        ProjectionPositions = positions
                    }.Run();
                }
#if HAVOK_PHYSICS_EXISTS
                else
                {
                    immediatePhysicsStepper.HavokSimulationContext.Reset(ref physicsWorld);
                    new StepLocalWorldHavokJob()
                    {
                        StepInput = stepInput,
                        SimulationContext = immediatePhysicsStepper.HavokSimulationContext,
                        StepIndex = i,
                        NumSteps = config.NumSteps,
                        ProjectionPositions = positions
                    }.Run();
                }
#endif
            }

            new ProjectionJob
            {
                Positions = positions,
                ProjectionScale = config.ProjectionScale,
                NumSteps = config.NumSteps
            }.Schedule();
        }

        void CreateProjectionEntities(ref SystemState state, Config config, RenderMeshArray projectionMaterial,
            int numDynamicBodies, Entity cueBallEntity)
        {
            var em = state.EntityManager;

            int numEntities = config.NumSteps * numDynamicBodies;
            positions = new NativeArray<float3>(numEntities, Allocator.Persistent);

            for (int i = 0; i < numEntities; i++)
            {
                var ballProjection = em.Instantiate(cueBallEntity);

                em.RemoveComponent<PhysicsCollider>(ballProjection);
                em.RemoveComponent<PhysicsVelocity>(ballProjection);
                em.RemoveComponent<CueBall>(ballProjection);

                em.AddComponentData(ballProjection, new Projection());
                em.AddSharedComponentManaged(ballProjection, projectionMaterial);
                em.SetComponentData(ballProjection, MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));

                em.AddComponentData(ballProjection, new PostTransformMatrix
                {
                    Value = float4x4.Scale(config.ProjectionScale)
                });
            }
        }

        [BurstCompile]
        [WithAll(typeof(Projection))]
        private partial struct ProjectionJob : IJobEntity
        {
            public NativeArray<float3> Positions;
            public float ProjectionScale;
            public int NumSteps;

            public void Execute([EntityIndexInQuery] int entityInQueryIndex, ref LocalTransform localTransform,
                ref PostTransformMatrix postTransformMatrix)
            {
                var posT0 = Positions[entityInQueryIndex];

                // Return if we are on the last step
                if ((entityInQueryIndex % NumSteps) == (NumSteps - 1))
                {
                    localTransform.Position = posT0;
                    postTransformMatrix.Value = float4x4.Scale(ProjectionScale);
                    return;
                }

                // Get the next position
                var posT1 = Positions[entityInQueryIndex + 1];

                // Return if we haven't moved
                var haveMovement = !posT0.Equals(posT1);
                if (!haveMovement)
                {
                    localTransform.Position = posT0; // Comment this out to leave the trails after shot.
                    postTransformMatrix.Value = float4x4.Scale(ProjectionScale);
                    return;
                }

                // Position the projection ball half way between T0 and T1
                localTransform.Position = math.lerp(posT0, posT1, 0.5f);
                // Orient the ball along the direction between T0 and T1
                // and stretch the ball between those 2 positions.
                var forward = posT1 - posT0;
                var scaleValue = math.length(forward);
                var rotationValue = quaternion.LookRotationSafe(forward, new float3(0, 1, 0));

                localTransform.Rotation = rotationValue;
                postTransformMatrix.Value.c2.z = scaleValue;
            }
        }

        [BurstCompile]
        struct StepJob : IJob
        {
            public SimulationStepInput StepInput;
            public SimulationContext SimulationContext;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> ProjectionPositions;

            public int NumSteps;
            public int StepIndex;

            public void Execute()
            {
                // Update the trails
                for (int b = 0; b < StepInput.World.DynamicBodies.Length; b++)
                {
                    ProjectionPositions[b * NumSteps + StepIndex] = StepInput.World.DynamicBodies[b].WorldFromBody.pos;
                }

                // Step the local world
                ImmediatePhysicsWorldStepper.StepUnityPhysicsSimulationImmediate(StepInput, ref SimulationContext);
            }
        }

#if HAVOK_PHYSICS_EXISTS
        [BurstCompile]
        struct StepLocalWorldHavokJob : IJob
        {
            public SimulationStepInput StepInput;
            public Havok.Physics.SimulationContext SimulationContext;

            [NativeDisableContainerSafetyRestriction]
            public NativeArray<float3> ProjectionPositions;

            public int NumSteps;
            public int StepIndex;

            public void Execute()
            {
                // Update the trails
                for (int b = 0; b < StepInput.World.DynamicBodies.Length; b++)
                {
                    ProjectionPositions[b * NumSteps + StepIndex] = StepInput.World.DynamicBodies[b].WorldFromBody.pos;
                }

                // Step the local world
                ImmediatePhysicsWorldStepper.StepHavokPhysicsSimulationImmediate(StepInput, ref SimulationContext);
            }
        }
#endif
    }
}
