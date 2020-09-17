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
            Assert.IsNotNull(m_World.GetExistingSystem<SystemShouldBeCreated>(), $"{nameof(SystemShouldBeCreated)} was not automatically created");
        }

        [Test]
        public void Systems_WithDisableAutoCreation_AreNotCreated()
        {
            Assert.IsNull(m_World.GetExistingSystem<SystemShouldNotBeCreated>(), $"{nameof(SystemShouldNotBeCreated)} was created even though it was marked with [{nameof(DisableAutoCreationAttribute)}].");
        }

        class SystemShouldBeCreated : SystemBase
        {
            protected override void OnUpdate()
            {
            }
        }

        [DisableAutoCreation]
        class SystemShouldNotBeCreated : SystemBase
        {
            protected override void OnUpdate()
            {
            }
        }
    }
}
