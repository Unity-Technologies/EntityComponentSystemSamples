using System;
using NUnit.Framework;
using UnityEngine.LowLevel;

namespace Unity.Entities.Tests
{
    class AutoCreateSystemsTests
    {
        World m_World;
        World m_PreviousWorld;
        PlayerLoopSystem m_PreviousPlayerLoop;

        [OneTimeSetUp]
        public void Setup()
        {
            m_PreviousWorld = World.DefaultGameObjectInjectionWorld;
            DefaultWorldInitialization.Initialize("TestWorld", false);
            m_World = World.DefaultGameObjectInjectionWorld;
            m_PreviousPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            PlayerLoop.SetPlayerLoop(m_PreviousPlayerLoop);
            World.DefaultGameObjectInjectionWorld = m_PreviousWorld;
            m_World?.Dispose();
        }

        [Test]
        public void Systems_WithoutDisableAutoCreation_AreAutoCreated()
        {
            Assert.IsNotNull(m_World.GetExistingSystemManaged<SystemShouldBeCreated>(), $"{nameof(SystemShouldBeCreated)} was not automatically created");
        }

        [Test]
        public void Systems_WithDisableAutoCreation_AreNotCreated()
        {
            Assert.IsNull(m_World.GetExistingSystemManaged<SystemShouldNotBeCreated>(), $"{nameof(SystemShouldNotBeCreated)} was created even though it was marked with [{nameof(DisableAutoCreationAttribute)}].");
        }

        [Test]
        public void ISystems_WithoutDisableAutoCreation_AreAutoCreated()
        {
#if UNITY_EDITOR
            Assert.DoesNotThrow(() => m_World.GetExistingSystem<ISystemShouldBeCreated>(), $"{nameof(ISystemShouldBeCreated)} was not automatically created");
#else
            Assert.IsTrue(m_World.GetExistingSystem<ISystemShouldBeCreated>() != default, $"{nameof(ISystemShouldBeCreated)} was not automatically created");
#endif
        }

        [Test]
        public void ISystems_WithDisableAutoCreation_AreNotCreated()
        {
            Assert.IsTrue(m_World.GetExistingSystem<ISystemShouldNotBeCreated>() == default, $"{nameof(ISystemShouldNotBeCreated)} was created even though it was marked with [{nameof(DisableAutoCreationAttribute)}].");
        }

        partial class SystemShouldBeCreated : SystemBase
        {
            protected override void OnUpdate()
            {
            }
        }

        [DisableAutoCreation]
        partial class SystemShouldNotBeCreated : SystemBase
        {
            protected override void OnUpdate()
            {
            }
        }

        partial struct ISystemShouldBeCreated : ISystem
        {
            public void OnCreate(ref SystemState state)
            {
            }
            public void OnDestroy(ref SystemState state)
            {
            }
            public void OnUpdate(ref SystemState state)
            {
            }
        }

        [DisableAutoCreation]
        partial struct ISystemShouldNotBeCreated : ISystem
        {
            public void OnCreate(ref SystemState state)
            {
            }
            public void OnDestroy(ref SystemState state)
            {
            }
            public void OnUpdate(ref SystemState state)
            {
            }
        }
    }
}
