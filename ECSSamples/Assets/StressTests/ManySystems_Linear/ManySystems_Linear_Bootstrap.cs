using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace StressTests.ManySystems
{
    /// <summary>
    /// This stress-test creates systems that all access the same single component.
    /// </summary>
    public class ManySystems_Linear_Bootstrap : MonoBehaviour
    {
        public int NumSystems = 1000;
        public int NumEntities = 1000;
        public bool UseSchedule = true;
        public bool ReadOnly = false;

        SystemBase[] m_Systems;

        // Start is called before the first frame update
        void Start()
        {
            m_Systems = new SystemBase[NumSystems];
            for (int i = 0; i < NumSystems; i++)
            {
                if (UseSchedule)
                {
                    if (ReadOnly)
                        m_Systems[i] = new TestSystem_ScheduleReader();
                    else
                        m_Systems[i] = new TestSystem_Schedule();
                }
                else
                {
                    if (ReadOnly)
                        m_Systems[i] = new TestSystem_RunReader();
                    else
                        m_Systems[i] = new TestSystem_Run();
                }
            }

            var world = World.DefaultGameObjectInjectionWorld;
            var arr = world.EntityManager.CreateEntity(world.EntityManager.CreateArchetype(typeof(TestComponent)), NumEntities, Allocator.Temp);
            arr.Dispose();

            for (int i = 0; i < m_Systems.Length; i++)
                world.AddSystem((m_Systems[i]));
        }

        void Update()
        {
            foreach (var s in m_Systems)
                s.Update();
        }
    }

    struct TestComponent : IComponentData
    {
        public float Value;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    class TestSystem_Schedule : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((ref TestComponent t) =>
            {
                t.Value += 1;
            }).ScheduleParallel();
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    class TestSystem_Run : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((ref TestComponent t) =>
            {
                t.Value += 1;
            }).Run();
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    class TestSystem_ScheduleReader : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((in TestComponent t) => {}).ScheduleParallel();
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    class TestSystem_RunReader : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((in TestComponent t) => {}).Run();
        }
    }
}
