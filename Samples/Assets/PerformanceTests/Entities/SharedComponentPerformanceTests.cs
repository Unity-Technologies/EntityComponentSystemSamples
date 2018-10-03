using NUnit.Framework;
using Unity.Collections;
using Unity.PerformanceTesting;

namespace Unity.Entities.PerformanceTests
{
    public sealed class SharedComponentPerformanceTests
    {
        private World m_PreviousWorld;
        private World m_World;
        private EntityManager m_Manager;

        [SetUp]
        public void Setup()
        {
            m_PreviousWorld = World.Active;
            m_World = World.Active = new World("Test World");
            m_Manager = m_World.GetOrCreateManager<EntityManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (m_Manager != null)
            {
                m_World.Dispose();
                m_World = null;

                World.Active = m_PreviousWorld;
                m_PreviousWorld = null;
                m_Manager = null;
            }
        }


        struct TestShared1 : ISharedComponentData
        {
            public int value;
        }

        struct TestShared2 : ISharedComponentData
        {
            public int value;
        }

        unsafe struct TestData1 : IComponentData
        {
            public fixed long value[16];
        }

        struct TestData2 : IComponentData
        {
#pragma warning disable 649
            public int value;
#pragma warning restore 649
        }


        [PerformanceTest]
        public void SetSharedComponentDataPerformanceTest()
        {
            var archetype = m_Manager.CreateArchetype(typeof(TestData1), typeof(TestShared1), typeof(TestShared2));
            var setSharedComponentData = new SampleGroupDefinition("SetSharedComponentData");

            Measure.Method(() =>
                {
                    NativeArray<Entity> entities = new NativeArray<Entity>(16384, Allocator.Temp);
                    m_Manager.CreateEntity(archetype, entities);
                    using (Measure.Scope(setSharedComponentData))
                    {
                        for (int i = 0; i < entities.Length; ++i)
                        {
                            m_Manager.SetSharedComponentData(entities[i], new TestShared1{value = i&0x003F});
                            m_Manager.SetSharedComponentData(entities[i], new TestShared2{value = i&0x0FC0});
                        }

                    }

                    m_Manager.DestroyEntity(entities);
                    entities.Dispose();
                }).Run();
        }

        [PerformanceTest]
        public void AddComponentPerformanceTest()
        {
            var archetype = m_Manager.CreateArchetype(typeof(TestData1), typeof(TestShared1), typeof(TestShared2));
            var addComponent = new SampleGroupDefinition("AddComponent");

            Measure.Method(() =>
                {
                    NativeArray<Entity> entities = new NativeArray<Entity>(16384, Allocator.Temp);
                    m_Manager.CreateEntity(archetype, entities);
                    for (int i = 0; i < entities.Length; ++i)
                    {
                        m_Manager.SetSharedComponentData(entities[i], new TestShared1{value = i&0x003F});
                        m_Manager.SetSharedComponentData(entities[i], new TestShared2{value = i&0x0FC0});
                    }

                    using (Measure.Scope(addComponent))
                    {
                        for (int i = 0; i < entities.Length; ++i)
                        {
                            m_Manager.AddComponentData(entities[i], new TestData2());
                        }
                    }

                    m_Manager.DestroyEntity(entities);
                    entities.Dispose();
                }).Run();
        }

    }
}
