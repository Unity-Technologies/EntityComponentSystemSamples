using Unity.Entities;
using Unity.Scenes;
using UnityEngine;


class ToggleSubsceneLoading : MonoBehaviour
{
    public int FramesBetweenStreamingOperations = 10;
    public int FramesUntilToggleLoad;

    public void OnValidate()
    {
        if (FramesBetweenStreamingOperations < 1)
            FramesBetweenStreamingOperations = 1;
    }

    public static World DefaultWorld
    {
        private set { }
        get
        {
#if UNITY_ENTITIES_0_2_0_OR_NEWER
            return World.DefaultGameObjectInjectionWorld;
#else
            return World.Active;
#endif
        }
    }

    void Update()
    {
        var subscene = GetComponent<SubScene>();
        if (subscene == null)
            return;

        if (++FramesUntilToggleLoad >= FramesBetweenStreamingOperations)
        {
            FramesUntilToggleLoad = 0;

            var entityManager = DefaultWorld.EntityManager;
            foreach (var sceneEntity in subscene._SceneEntities)
            {
                if (!entityManager.HasComponent<RequestSceneLoaded>(sceneEntity))
                {
                    entityManager.AddComponentData(sceneEntity, new RequestSceneLoaded());
                }
                else
                {
                    entityManager.RemoveComponent<RequestSceneLoaded>(sceneEntity);
                }
            }
        }
    }
}
