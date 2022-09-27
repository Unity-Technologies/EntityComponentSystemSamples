using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// this script forces a lower fixed time step for both GO and DOTS physics to demonstrate motion smoothing
class MotionSmoothingSetup : MonoBehaviour
{
    // default to a low tick rate for demonstration purposes
    [Min(0)]
    public int StepsPerSecond = 15;

    float m_FixedTimetep;

    void OnEnable()
    {
        m_FixedTimetep = Time.fixedDeltaTime;
        Time.fixedDeltaTime = 1f / StepsPerSecond;
    }

    void OnDisable() => Time.fixedDeltaTime = m_FixedTimetep;

    void OnValidate() => StepsPerSecond = math.max(0, StepsPerSecond);

    class MotionSmoothingSetupBaker : Baker<MotionSmoothingSetup>
    {
        public override void Bake(MotionSmoothingSetup authoring)
        {
            var e = CreateAdditionalEntity();
            AddComponent(e, new SetFixedTimestep { Timestep = 1f / authoring.StepsPerSecond });
        }
    }
}

struct SetFixedTimestep : IComponentData
{
    public float Timestep;
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
partial class SetFixedTimestepSystem : SystemBase
{
    FixedStepSimulationSystemGroup m_FixedStepSimulationSystemGroup;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_FixedStepSimulationSystemGroup = World.GetOrCreateSystemManaged<FixedStepSimulationSystemGroup>();
        RequireForUpdate<SetFixedTimestep>();
    }

    protected override void OnUpdate()
    {
        var fixedStepSimulationSystemGroup = m_FixedStepSimulationSystemGroup;
        Entities
            .WithStructuralChanges()
            .ForEach((ref Entity entity, ref SetFixedTimestep setFixedTimestep) =>
            {
                fixedStepSimulationSystemGroup.Timestep = setFixedTimestep.Timestep;
                EntityManager.DestroyEntity(entity);
            }).Run();
    }
}
