using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// this script forces a lower fixed time step for both GO and DOTS physics to demonstrate motion smoothing
class MotionSmoothingSetup : MonoBehaviour, IConvertGameObjectToEntity
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

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var e = conversionSystem.CreateAdditionalEntity(this);
        dstManager.AddComponentData(e, new SetFixedTimestep { Timestep = 1f / StepsPerSecond });
    }
}

struct SetFixedTimestep : IComponentData
{
    public float Timestep;
}

[UpdateInGroup(typeof(InitializationSystemGroup))]
class SetFixedTimestepSystem : SystemBase
{
    FixedStepSimulationSystemGroup m_FixedStepSimulationSystemGroup;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_FixedStepSimulationSystemGroup = World.GetOrCreateSystem<FixedStepSimulationSystemGroup>();
        RequireForUpdate(
            GetEntityQuery(new EntityQueryDesc { All = new ComponentType[] { typeof(SetFixedTimestep) } })
        );
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
