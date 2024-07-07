using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics.GraphicsIntegration;
using Unity.Physics.Systems;

namespace Unity.NetCode
{
    //We want this system to no run into any custom physics world. So we don't target directly BeforePhysicsSytemGroup
    //This system is responbile to instrument the CustomBuildPhysicsWorld if rebuild or update the physics world
    //based on the PhysicsLoopConfig settings and the current simulated tick.
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(PhysicsSystemGroup))]
    public partial struct ConfigureBuildPhysicsWorld : ISystem
    {
        private SystemHandle physicsBuildWorld;
        private NetworkTick lastFullBuildTick;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            physicsBuildWorld = state.WorldUnmanaged.GetExistingUnmanagedSystem<CustomBuildPhysicsWorld>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            SystemAPI.TryGetSingleton<PhysicsLoopConfig>(out var loopConfig);
            var settings = state.EntityManager.GetComponentDataRW<BuildPhysicsWorldSettings>(physicsBuildWorld);
            settings.ValueRW.UseImmediateMode = loopConfig.UseImmediateMode;
            settings.ValueRW.StepImmediateMode = loopConfig.StepImmediateMode;
            //The first prediction tick we always rebuild the physics world no matter what to
            //start from a clean state.
            //We do the same also for the final full tick and partial ticks, so the very last step also use a
            //good starting point.
            //In case physics tick rate > simulation tick rate, the build should be done only the first physic step
            if (networkTime.IsFirstPredictionTick || networkTime.IsFinalFullPredictionTick)
            {
                if (!lastFullBuildTick.IsValid || lastFullBuildTick != networkTime.ServerTick)
                {
                    settings.ValueRW.UpdateBroadphaseAndMotion = 0;
                    lastFullBuildTick = networkTime.ServerTick;
                }
                else
                {
                    settings.ValueRW.UpdateBroadphaseAndMotion = 1;
                }
            }
            else
            {
                settings.ValueRW.UpdateBroadphaseAndMotion = 1;
            }
        }
    }

    /// <summary>
    /// System that enable/disable which physics system need to update for a given predicted tick.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(PredictedFixedStepSimulationSystemGroup))]
    partial class EnableDisablePhysicsSystems : SystemBase
    {
        private SystemHandle BuildPhysicWorldHandle;
        private SystemHandle SyncCustomPhysicsProxySystemHandle;
        private SystemHandle BufferInterpolatedRigidBodiesMotionHandle;
        private SystemHandle CopyPhysicsVelocityToSmoothingHandle;
        private ComponentSystemGroup PhysicsCreateBodyPairsGroup;
        private ComponentSystemGroup PhysicsCreateContactsGroup;
        private ComponentSystemGroup PhysicsCreateJacobiansGroup;
        private ComponentSystemGroup PhysicsSolveAndIntegrateGroup;
        //Custom physics systems.
        private SystemHandle CustomBuildPhysicsWorldHandle;
        private SystemHandle ImmediatePhysicsStepHandle;

        protected override void OnCreate()
        {
            BuildPhysicWorldHandle = World.Unmanaged.GetExistingUnmanagedSystem<BuildPhysicsWorld>();
            SyncCustomPhysicsProxySystemHandle = World.Unmanaged.GetExistingUnmanagedSystem<SyncCustomPhysicsProxySystem>();
            BufferInterpolatedRigidBodiesMotionHandle = World.Unmanaged.GetExistingUnmanagedSystem<BufferInterpolatedRigidBodiesMotion>();
            CopyPhysicsVelocityToSmoothingHandle = World.Unmanaged.GetExistingUnmanagedSystem<CopyPhysicsVelocityToSmoothing>();
            PhysicsCreateBodyPairsGroup = World.GetExistingSystemManaged<PhysicsCreateBodyPairsGroup>();
            PhysicsCreateContactsGroup = World.GetExistingSystemManaged<PhysicsCreateContactsGroup>();
            PhysicsCreateJacobiansGroup = World.GetExistingSystemManaged<PhysicsCreateJacobiansGroup>();
            PhysicsSolveAndIntegrateGroup = World.GetExistingSystemManaged<PhysicsSolveAndIntegrateGroup>();
            CustomBuildPhysicsWorldHandle = World.Unmanaged.GetExistingUnmanagedSystem<CustomBuildPhysicsWorld>();
            ImmediatePhysicsStepHandle = World.Unmanaged.GetExistingUnmanagedSystem<ImmediatePhysicsStep>();
            RequireForUpdate<PhysicsLoopConfig>();
        }

        protected override void OnUpdate()
        {
            var loopConfig = SystemAPI.GetSingleton<PhysicsLoopConfig>();
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            //Setup this once per prediction update, becaue the loopConfig does not change in between prediction steps.
            if (networkTime.IsFirstPredictionTick)
            {
                World.Unmanaged.ResolveSystemStateRef(CustomBuildPhysicsWorldHandle).Enabled = true;
                World.Unmanaged.ResolveSystemStateRef(ImmediatePhysicsStepHandle).Enabled = loopConfig.StepImmediateMode != 0;
                //Disable the normal build physics systems. The CustomBuildPhysicsWorldHandle will handle everything
                World.Unmanaged.ResolveSystemStateRef(BuildPhysicWorldHandle).Enabled = false;
                //In immediate mode we don't want any of this systems runs because the step is already executing all this.
                PhysicsCreateBodyPairsGroup.Enabled = loopConfig.StepImmediateMode == 0;
                PhysicsCreateContactsGroup.Enabled = loopConfig.StepImmediateMode == 0;
                PhysicsCreateJacobiansGroup.Enabled = loopConfig.StepImmediateMode == 0;
                PhysicsSolveAndIntegrateGroup.Enabled = loopConfig.StepImmediateMode == 0;
            }
            //We enable syncing proxies, interpolation and physics velocity smoothing only for the final prediction
            //tick. The cost is usually little, but running them for nothing does not make much sense
            var isFinalTick = networkTime.IsFinalPredictionTick || networkTime.IsFinalFullPredictionTick;
            World.Unmanaged.ResolveSystemStateRef(SyncCustomPhysicsProxySystemHandle).Enabled = isFinalTick;
            World.Unmanaged.ResolveSystemStateRef(BufferInterpolatedRigidBodiesMotionHandle).Enabled = isFinalTick;
            World.Unmanaged.ResolveSystemStateRef(CopyPhysicsVelocityToSmoothingHandle).Enabled = isFinalTick;
        }
    }

    /// <summary>
    /// System that runs on the client and disable the custom physics build world before the FixedStepSimulationSystemGroup.
    /// This will make any physics world simulation (i.e client0only physics) to execute normally.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
    public partial class DisableCustomPhysicsSystems : SystemBase
    {
        private SystemHandle BuildPhysicWorldHandle;
        private SystemHandle CustomBuildPhysicsWorldHandle;
        private SystemHandle SyncCustomPhysicsProxySystemHandle;
        private SystemHandle BufferInterpolatedRigidBodiesMotionHandle;
        private SystemHandle CopyPhysicsVelocityToSmoothingHandle;
        private ComponentSystemGroup PhysicsCreateBodyPairsGroup;
        private ComponentSystemGroup PhysicsCreateContactsGroup;
        private ComponentSystemGroup PhysicsCreateJacobiansGroup;
        private ComponentSystemGroup PhysicsSolveAndIntegrateGroup;
        private SystemHandle ImmediatePhysicsStepHandle;

        protected override void OnCreate()
        {
            BuildPhysicWorldHandle = World.Unmanaged.GetExistingUnmanagedSystem<BuildPhysicsWorld>();
            CustomBuildPhysicsWorldHandle = World.Unmanaged.GetExistingUnmanagedSystem<CustomBuildPhysicsWorld>();
            SyncCustomPhysicsProxySystemHandle = World.Unmanaged.GetExistingUnmanagedSystem<SyncCustomPhysicsProxySystem>();
            BufferInterpolatedRigidBodiesMotionHandle = World.Unmanaged.GetExistingUnmanagedSystem<BufferInterpolatedRigidBodiesMotion>();
            CopyPhysicsVelocityToSmoothingHandle = World.Unmanaged.GetExistingUnmanagedSystem<CopyPhysicsVelocityToSmoothing>();
            ImmediatePhysicsStepHandle = World.Unmanaged.GetExistingUnmanagedSystem<ImmediatePhysicsStep>();
            PhysicsCreateBodyPairsGroup = World.GetExistingSystemManaged<PhysicsCreateBodyPairsGroup>();
            PhysicsCreateContactsGroup = World.GetExistingSystemManaged<PhysicsCreateContactsGroup>();
            PhysicsCreateJacobiansGroup = World.GetExistingSystemManaged<PhysicsCreateJacobiansGroup>();
            PhysicsSolveAndIntegrateGroup = World.GetExistingSystemManaged<PhysicsSolveAndIntegrateGroup>();
        }

        protected override void OnUpdate()
        {
            PhysicsCreateBodyPairsGroup.Enabled  = true;
            PhysicsCreateContactsGroup.Enabled  = true;
            PhysicsCreateJacobiansGroup.Enabled  = true;
            PhysicsSolveAndIntegrateGroup.Enabled  = true;
            World.Unmanaged.ResolveSystemStateRef(BuildPhysicWorldHandle).Enabled = true;
            World.Unmanaged.ResolveSystemStateRef(SyncCustomPhysicsProxySystemHandle).Enabled = true;
            World.Unmanaged.ResolveSystemStateRef(BufferInterpolatedRigidBodiesMotionHandle).Enabled = true;
            World.Unmanaged.ResolveSystemStateRef(CopyPhysicsVelocityToSmoothingHandle).Enabled = true;
            World.Unmanaged.ResolveSystemStateRef(ImmediatePhysicsStepHandle).Enabled = false;
            World.Unmanaged.ResolveSystemStateRef(CustomBuildPhysicsWorldHandle).Enabled = false;
        }
    }
}
