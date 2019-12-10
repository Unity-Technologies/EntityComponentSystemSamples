using Unity.Entities;
using Unity.Scenes;
using UnityEngine;

[RequireComponent(typeof(SubScene))]
class ToggleSubsceneLoading : MonoBehaviour
{
    public int FramesBetweenStreamingOperations = 10;
    public int FramesUntilToggleLoad;
    SubScene m_SubScene;

    void OnValidate()
    {
        if (FramesBetweenStreamingOperations < 1)
            FramesBetweenStreamingOperations = 1;
    }

    void Start() => m_SubScene = GetComponent<SubScene>();

    void Update()
    {
        if (++FramesUntilToggleLoad >= FramesBetweenStreamingOperations)
        {
            if (m_SubScene == null)
                return;

            FramesUntilToggleLoad = 0;

            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var sceneSystem = entityManager.World.GetExistingSystem<SceneSystem>();
            var sceneEntity = sceneSystem.GetSceneEntity(m_SubScene.SceneGUID);

            if (!entityManager.HasComponent<RequestSceneLoaded>(sceneEntity))
                sceneSystem.LoadSceneAsync(m_SubScene.SceneGUID);
            else
                sceneSystem.UnloadScene(m_SubScene.SceneGUID);
        }
    }
}
