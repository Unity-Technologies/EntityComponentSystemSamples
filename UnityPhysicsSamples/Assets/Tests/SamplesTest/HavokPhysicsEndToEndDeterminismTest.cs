#if HAVOK_PHYSICS_EXISTS
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Jobs;
using NUnit.Framework;

namespace Unity.Physics.Samples.Test
{
    [UpdateAfter(typeof(ExportPhysicsWorld)), UpdateBefore(typeof(EndFramePhysicsSystem))]
    public class HavokPhysicsDeterminismTestSystem : JobComponentSystem, IDeterminismTestSystem
    {
        private BuildPhysicsWorld m_BuildPhysicsWorld;
        private StepPhysicsWorld m_StepPhysicsWorld;
        private ExportPhysicsWorld m_ExportPhysicsWorld;
        private EnsureHavokSystem m_EnsureHavokSystem;
        protected bool m_TestingFinished = false;

        public int SimulatedFramesInCurrentTest = 0;
        public const int k_TestDurationInFrames = 100;

        public void BeginTest()
        {
            SimulatedFramesInCurrentTest = 0;
            Enabled = true;
            m_ExportPhysicsWorld.Enabled = true;
            m_StepPhysicsWorld.Enabled = true;
            m_BuildPhysicsWorld.Enabled = true;
            m_EnsureHavokSystem.Enabled = true;

            m_TestingFinished = false;
        }

        public bool TestingFinished() => m_TestingFinished;

        protected override void OnCreate()
        {
            Enabled = false;

            m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            m_ExportPhysicsWorld = World.GetOrCreateSystem<ExportPhysicsWorld>();
            m_EnsureHavokSystem = World.GetOrCreateSystem<EnsureHavokSystem>();
        }

        protected void FinishTesting()
        {
            SimulatedFramesInCurrentTest = 0;
            m_ExportPhysicsWorld.Enabled = false;
            m_StepPhysicsWorld.Enabled = false;
            m_BuildPhysicsWorld.Enabled = false;
            Enabled = false;

            m_TestingFinished = true;
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            SimulatedFramesInCurrentTest++;
            inputDeps = JobHandle.CombineDependencies(inputDeps, m_ExportPhysicsWorld.FinalJobHandle);

            if (SimulatedFramesInCurrentTest == k_TestDurationInFrames)
            {
                inputDeps.Complete();
                FinishTesting();
            }

            return inputDeps;
        }
    }

    [UpdateBefore(typeof(BuildPhysicsWorld))]
    public class EnsureHavokSystem : ComponentSystem
    {
        protected override void OnCreate()
        {
            Enabled = false;
        }

        protected override void OnUpdate()
        {
            if (HasSingleton<PhysicsStep>())
            {
                var component = GetSingleton<PhysicsStep>();
                if (component.SimulationType != SimulationType.HavokPhysics)
                {
                    component.SimulationType = SimulationType.HavokPhysics;
                    SetSingleton(component);
                }
                Enabled = false;
            }
        }
    }

    // Only works in standalone build, since it needs synchronous Burst compilation.
#if !UNITY_EDITOR
    [TestFixture]
#endif
    class HavokPhysicsEndToEndDeterminismTest : UnityPhysicsEndToEndDeterminismTest
    {
        protected override IDeterminismTestSystem GetTestSystem() => DefaultWorld.GetOrCreateSystem<HavokPhysicsDeterminismTestSystem>();
    }

}
#endif
