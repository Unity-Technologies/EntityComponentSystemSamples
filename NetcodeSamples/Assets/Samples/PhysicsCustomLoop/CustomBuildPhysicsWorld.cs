using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Unity.NetCode
{
    /// <summary>
    /// A system which builds the physics world based on the entity world. The world will contain a
    /// rigid body for every entity which has a rigid body component, and a joint for every entity
    /// which has a joint component.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [BurstCompile]
    [UpdateInGroup(typeof(PhysicsBuildWorldGroup))]
    [CreateAfter(typeof(BuildPhysicsWorld))]
    public partial struct CustomBuildPhysicsWorld : ISystem
    {
        private SystemHandle buildPhysicSystemHandle;
        private int currentPhysicStep;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            buildPhysicSystemHandle = state.WorldUnmanaged.GetExistingUnmanagedSystem<BuildPhysicsWorld>();
            //It is necesary to create temp PhysicsWorldData here for sake of setting up the correct dependencies.
            //The PhysicsWorldData itself is then retrieved from the BuildPhysicsWorld all the time.
            var physicsData = new PhysicsWorldData(ref state, new PhysicsWorldIndex());
            physicsData.Dispose();
            state.EntityManager.AddComponentData(state.SystemHandle, new BuildPhysicsWorldSettings());
            //The sytem always start disabled and will be enabled/disabled based on the PhysicsCustomLoop component
            //settings by the ConfigureBuildPhysicsWorld.
            state.Enabled = false;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //First we need to need to complete any pending dependencies before building or updating the
            //physics world. In particular the BuildPhysicsWorldData may have some physics jobs that need to be
            //completed but for which the dependency cannot be tracked automatically. They are tracked by the internal
            // InputDepdency handled and it is possible to wait for them here using the exposed CompleteInputDependency method.
            ref var buildPhysicsData = ref state.EntityManager.GetComponentDataRW<BuildPhysicsWorldData>(buildPhysicSystemHandle).ValueRW;
            buildPhysicsData.CompleteInputDependency();

            float timeStep = SystemAPI.Time.DeltaTime;
            if (!SystemAPI.TryGetSingleton(out PhysicsStep stepComponent))
                stepComponent = PhysicsStep.Default;

            //The BuildPhysicsWorldSettings govern how the build system construct the physics world. In particular,
            //either a full rebuild or an "incremental" one, that just update the current brodphase by enlargin the AABB
            //by using the last calculated physics step PhysicsVelocity and gravity.
            //Notice that in case of prediction, the PhysicsVelocity may be actually a replicated value from the server
            //and not the last predicted value, being used as both input-output.
            var settings = state.EntityManager.GetComponentData<BuildPhysicsWorldSettings>(state.SystemHandle);

            // In case we just want to update the broadphase and motion data we check if any new
            // physics object has been created or destroyed (either, static, dynamic or joint) and we force a full rebuild
            // in that case.
            if (settings.UpdateBroadphaseAndMotion == 1)
            {
                var dynamicObject = buildPhysicsData.PhysicsData.DynamicEntityGroup.CalculateEntityCount();
                //+1 to account for the default static body object
                var staticObject = buildPhysicsData.PhysicsData.StaticEntityGroup.CalculateEntityCount() + 1;
                var joints = buildPhysicsData.PhysicsData.JointEntityGroup.CalculateEntityCount();
                //If entity count changed we forcibly rebuild (and we log this)
                if (buildPhysicsData.PhysicsData.PhysicsWorld.NumDynamicBodies != dynamicObject ||
                    buildPhysicsData.PhysicsData.PhysicsWorld.NumStaticBodies != staticObject ||
                    buildPhysicsData.PhysicsData.PhysicsWorld.NumJoints != joints)
                {
                    settings.UpdateBroadphaseAndMotion = 0;
                }
            }

            // The following code either does a full rebuild (BuildPhysicsWorld)
            // or does an partial update of the Motion and Broaphase.
            // The update of the motions and the broaphase are not executed
            // by the same method (i.e ScheduleUpdateBroadphase) because this way there is a little bit more
            // flexibility for checking if and what we actually need to update. Technically, only simulated physics entities can have LocalTransform,
            // and PhysicsVelocity expoerted, meaning that "kinematic-like" motion data in general does not change, and
            // can be potentially not need an update.

            //Immediate mode could be a good choice to use when the number of physics entities is small.
            //In this sammple, this is executed on the main thread but it is possible to also execute that
            //in a job.
            if (settings.UseImmediateMode != 0)
            {
                state.CompleteDependency();
                if (settings.UpdateBroadphaseAndMotion == 0)
                {
                    PhysicsWorldBuilder.BuildPhysicsWorldImmediate(ref state, ref buildPhysicsData.PhysicsData,
                        timeStep, stepComponent.Gravity, state.LastSystemVersion);
                }
                else
                {
                    buildPhysicsData.PhysicsData.Update(ref state);
                    PhysicsWorldBuilder.UpdateMotionDataImmediate(ref state, ref buildPhysicsData.PhysicsData);
                    //This method internally check if necessary to update also the static tree
                    PhysicsWorldBuilder.UpdateBroadphaseImmediate(ref buildPhysicsData.PhysicsData, timeStep, stepComponent.Gravity,
                        state.LastSystemVersion);
                }
            }
            else
            {
                //Full rebuild of the physics world.
                if (settings.UpdateBroadphaseAndMotion == 0)
                {
                    state.Dependency = PhysicsWorldBuilder.SchedulePhysicsWorldBuild(ref state, ref buildPhysicsData.PhysicsData,
                        state.Dependency, timeStep, stepComponent.MultiThreaded > 0, stepComponent.Gravity, state.LastSystemVersion);
                }
                else
                {
                    buildPhysicsData.PhysicsData.Update(ref state);
                    state.Dependency = PhysicsWorldBuilder.ScheduleUpdateMotionData(ref state, ref buildPhysicsData.PhysicsData, state.Dependency);
                    //This method internally check if necessary to update also the static tree
                    state.Dependency = PhysicsWorldBuilder.ScheduleUpdateBroadphase(
                        ref buildPhysicsData.PhysicsData, timeStep, stepComponent.Gravity, state.LastSystemVersion,
                        state.Dependency, stepComponent.MultiThreaded > 0);
                }
            }
            SystemAPI.SetSingleton(new PhysicsWorldSingleton
            {
                PhysicsWorld = buildPhysicsData.PhysicsData.PhysicsWorld,
                PhysicsWorldIndex = buildPhysicsData.WorldFilter
            });
        }
    }
}
