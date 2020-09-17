using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

#pragma warning disable 0649

namespace StressTests.ManySystems
{
    /// <summary>
    /// This stress-test creates lots of small systems that each access a subset of 10 components randomly (with read/write
    /// set randomly as well). This allows testing job-scheduling and debugging overhead.
    /// </summary>
    public class ManySystems_Complex_Bootstrap : MonoBehaviour
    {
        public int NumSystems = 1000;
        public int NumEntities = 1000;
        public bool UseSchedule = true;
        public float ReadOnlyProbability = .5f;
        public float ComponentIncludeProbability = .5f;

        TestSystem_Complex[] m_Systems;

        // Start is called before the first frame update
        void Start()
        {
            m_Systems = new TestSystem_Complex[NumSystems];
            for (int i = 0; i < NumSystems; i++)
            {
                m_Systems[i] = new TestSystem_Complex();
                m_Systems[i].UseRun = !UseSchedule;
                m_Systems[i].Types = RandomTypes();
            }

            var world = World.DefaultGameObjectInjectionWorld;
            var arr = world.EntityManager.CreateEntity(world.EntityManager.CreateArchetype(typeof(TestComponent)), NumEntities, Allocator.Temp);
            arr.Dispose();

            for (int i = 0; i < m_Systems.Length; i++)
                world.AddSystem(m_Systems[i]);
        }

        static readonly Type[] k_Components =
        {
            typeof(TestComponent0),
            typeof(TestComponent1),
            typeof(TestComponent2),
            typeof(TestComponent3),
            typeof(TestComponent4),
            typeof(TestComponent5),
            typeof(TestComponent6),
            typeof(TestComponent7),
            typeof(TestComponent8),
            typeof(TestComponent9),
        };

        ComponentType[] RandomTypes()
        {
            var list = new List<ComponentType>();
            for (int i = 0; i < k_Components.Length; i++)
            {
                if (UnityEngine.Random.value < ComponentIncludeProbability)
                {
                    bool readOnly = UnityEngine.Random.value < ReadOnlyProbability;
                    list.Add(new ComponentType(k_Components[i], readOnly ? ComponentType.AccessMode.ReadOnly : ComponentType.AccessMode.ReadWrite));
                }
            }

            return list.ToArray();
        }

        void Update()
        {
            foreach (var s in m_Systems)
                s.Update();
        }
    }

    struct TestComponent0 : IComponentData
    {
        public float Value;
    }

    struct TestComponent1 : IComponentData
    {
        public float Value;
    }

    struct TestComponent2 : IComponentData
    {
        public float Value;
    }

    struct TestComponent3 : IComponentData
    {
        public float Value;
    }

    struct TestComponent4 : IComponentData
    {
        public float Value;
    }

    struct TestComponent5 : IComponentData
    {
        public float Value;
    }

    struct TestComponent6 : IComponentData
    {
        public float Value;
    }

    struct TestComponent7 : IComponentData
    {
        public float Value;
    }

    struct TestComponent8 : IComponentData
    {
        public float Value;
    }

    struct TestComponent9 : IComponentData
    {
        public float Value;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [DisableAutoCreation]
    [AlwaysUpdateSystem]
    class TestSystem_Complex : SystemBase
    {
        EntityQuery m_Query;
        public ComponentType[] Types;
        public bool UseRun;

        protected override void OnCreate()
        {
            m_Query = GetEntityQuery(Types);
        }

        protected override void OnUpdate()
        {
            if (UseRun)
                BuildJob().Run(m_Query);
            else
                Dependency = BuildJob().ScheduleParallel(m_Query, Dependency);
        }

        unsafe TestJob BuildJob()
        {
            var job = new TestJob();
            DynamicComponentTypeHandle* ptr = (DynamicComponentTypeHandle*)&job.Type0;
            {
                for (int i = 0; i < Types.Length; i++)
                {
                    ptr[i] = GetDynamicComponentTypeHandle(Types[i]);
                }
            }

            return job;
        }

        struct TestJob : IJobChunk
        {
            [NativeDisableContainerSafetyRestriction]
            public DynamicComponentTypeHandle Type0;
            [NativeDisableContainerSafetyRestriction]
            public DynamicComponentTypeHandle Type1;
            [NativeDisableContainerSafetyRestriction]
            public DynamicComponentTypeHandle Type2;
            [NativeDisableContainerSafetyRestriction]
            public DynamicComponentTypeHandle Type3;
            [NativeDisableContainerSafetyRestriction]
            public DynamicComponentTypeHandle Type4;
            [NativeDisableContainerSafetyRestriction]
            public DynamicComponentTypeHandle Type5;
            [NativeDisableContainerSafetyRestriction]
            public DynamicComponentTypeHandle Type6;
            [NativeDisableContainerSafetyRestriction]
            public DynamicComponentTypeHandle Type7;
            [NativeDisableContainerSafetyRestriction]
            public DynamicComponentTypeHandle Type8;
            [NativeDisableContainerSafetyRestriction]
            public DynamicComponentTypeHandle Type9;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex) {}
        }
    }
}
#pragma warning restore 0649
