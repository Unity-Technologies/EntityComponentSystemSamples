using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Scenes;

struct TestSceneReference : IComponentData
{
    public EntitySceneReference SceneReference;
}

struct TestPrefabReference : IComponentData
{
    public EntityPrefabReference PrefabReference;
}

[UnityEngine.ExecuteAlways]
public partial class TestWeakRefLoadingSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem m_EndSimECBSystem;

    protected override void OnCreate()
    {
        m_EndSimECBSystem = World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        var ecb = m_EndSimECBSystem.CreateCommandBuffer().AsParallelWriter();

        Entities.ForEach((Entity entity, int entityInQueryIndex, in TestSceneReference sceneRef) =>
        {
            var sceneEntity = ecb.CreateEntity(entityInQueryIndex);
            ecb.AddComponent(entityInQueryIndex, sceneEntity, new RequestSceneLoaded());
            ecb.AddComponent(entityInQueryIndex, sceneEntity, new SceneReference(sceneRef.SceneReference));
            ecb.RemoveComponent<TestSceneReference>(entityInQueryIndex, entity);
        }).ScheduleParallel();

        Entities.WithNone<RequestEntityPrefabLoaded>().ForEach((Entity entity, int entityInQueryIndex, in TestPrefabReference prefabRef) =>
        {
            ecb.AddComponent(entityInQueryIndex, entity, new RequestEntityPrefabLoaded {Prefab = prefabRef.PrefabReference});
        }).ScheduleParallel();

        Entities.WithAll<TestPrefabReference>().ForEach((Entity entity, int entityInQueryIndex, in PrefabLoadResult loadedPrefab) =>
        {
            ecb.Instantiate(entityInQueryIndex, loadedPrefab.PrefabRoot);
            ecb.RemoveComponent<TestPrefabReference>(entityInQueryIndex, entity);
            ecb.RemoveComponent<RequestEntityPrefabLoaded>(entityInQueryIndex, entity);
        }).ScheduleParallel();

        m_EndSimECBSystem.AddJobHandleForProducer(Dependency);
    }
}
