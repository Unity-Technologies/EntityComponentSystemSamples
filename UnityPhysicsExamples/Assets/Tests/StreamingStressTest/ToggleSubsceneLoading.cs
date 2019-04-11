using Unity.Entities;
using Unity.Scenes;
using UnityEngine;

public class ToggleSubsceneLoading : MonoBehaviour
{
    public int FramesBetweenStreamingOperations = 10;
    public int FramesUntilToggleLoad;

    public void OnValidate()
    {
        if (FramesBetweenStreamingOperations < 1)
            FramesBetweenStreamingOperations = 1;
    }

    void Update()
    {
        var subscene = GetComponent<SubScene>();
        if (subscene == null)
            return;

        if (++FramesUntilToggleLoad >= FramesBetweenStreamingOperations)
        {
            FramesUntilToggleLoad = 0;

            var entityManager = World.Active.EntityManager;
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
