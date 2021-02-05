using System;
using System.Diagnostics;
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
        [Range(1, 1000)]
        public int NumSystems = 1000;
        public int NumEntities = 1000;
        public bool UseSchedule = true;
        public bool ReadOnly = false;
        public int SettleCount = 1000;
        public int TargetCount = 10000;

        SystemBase[] m_Systems;
        readonly FloatStatistic m_Stats = new FloatStatistic();
        readonly Stopwatch m_StopWatch = new Stopwatch();
        int m_Count;

        // Start is called before the first frame update
        void Start()
        {
            m_Count = -SettleCount;
            m_Systems = new SystemBase[NumSystems];
            string systemName;
            if (UseSchedule)
            {
                if (ReadOnly)
                    systemName = "TestSystem_ScheduleReader";
                else
                    systemName = "TestSystem_Schedule";
            }
            else
            {
                if (ReadOnly)
                    systemName = "TestSystem_RunReader";
                else
                    systemName = "TestSystem_Run";
            }
            for (int i = 0; i < NumSystems; i++)
            {
                var typeName = $"StressTests.ManySystems.{systemName}_{i:0000}";
                var type = Type.GetType(typeName);
                m_Systems[i] = (SystemBase)Activator.CreateInstance(type);
            }

            var world = World.DefaultGameObjectInjectionWorld;
            var arr = world.EntityManager.CreateEntity(world.EntityManager.CreateArchetype(typeof(TestComponent)), NumEntities, Allocator.Temp);
            arr.Dispose();

            for (int i = 0; i < m_Systems.Length; i++)
                world.AddSystem((m_Systems[i]));
        }

        void Update()
        {
            ++m_Count;
            m_StopWatch.Reset();
            m_StopWatch.Start();
            foreach (var s in m_Systems)
                s.Update();
            m_StopWatch.Stop();

            if ((m_Count >= 0) && (m_Count <= TargetCount))
            {
                m_Stats.AddValue(m_StopWatch.ElapsedMilliseconds);

                if ((m_Stats.Count % 1000) == 0)
                {
                    UnityEngine.Debug.Log($"{m_Stats.Count} samples Mean {m_Stats.Mean}ms +/- {m_Stats.Sigma}ms");
                }
            }
            if (m_Stats.Count == TargetCount)
            {
                UnityEngine.Debug.Log($"{m_Stats.Count} samples Mean {m_Stats.Mean}ms +/- {m_Stats.Sigma}ms");
            }
        }
    }

    struct TestComponent : IComponentData
    {
        public float Value;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    partial class TestSystem_Schedule : SystemBase
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
    partial class TestSystem_Run : SystemBase
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
    partial class TestSystem_ScheduleReader : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((in TestComponent t) => {}).ScheduleParallel();
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    partial class TestSystem_RunReader : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((in TestComponent t) => {}).Run();
        }
    }
}
