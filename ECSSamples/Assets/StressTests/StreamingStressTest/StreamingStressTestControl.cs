using System.Collections.Generic;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;
using Hash128 = UnityEngine.Hash128;

public class StreamingStressTestControl : MonoBehaviour
{
    private List<Hash128> m_SceneGuid;
    private List<Entity> m_SceneEntity;
    // If a scene was just unloaded, this cooldown will prevent us from reloading it immediately
    private List<int> m_SceneCooldown;

    // These bools control which scenes are part of the stress test. Changing them after the stress test has started
    // does not have any effect.
    public bool IncludeLightScenes = true;
    public bool IncludeCubeScenes = true;

    // If this is set, we wait briefly after unloading a scene before we consider it again for reloading.
    [Range(0, 10)]
    public int CooldownFrames = 10;

    [Range(0, 1)] public float LoadProbability = 0.5f;
    [Range(0, 1)] public float UnloadProbability = 0.5f;
    [Range(0, 1)] public float UnloadWhileLoadingProbability = 0.2f;
    // Sometimes it is useful to ensure that occasionally no scenes are loaded anymore. This will make leaks apparent.
    [Range(0, 1)] public float UnloadAllProbability = 0.01f;

    private Unity.Mathematics.Random m_Rng;

    void Start()
    {
        var subScenes = FindObjectsOfType<SubScene>();
        m_SceneGuid = new List<Hash128>(subScenes.Length);
        m_SceneEntity = new List<Entity>();
        m_SceneCooldown = new List<int>();
        for (int i = 0; i < subScenes.Length; i++)
        {
            var scene = subScenes[i].GetComponent<StreamingStressTestInstances>();
            if (IncludeLightScenes && scene.HasLights ||
                IncludeCubeScenes && scene.HasCubes)
            {
                int numInstances = scene.NumInstances;
                for (int k = 0; k < numInstances; k++)
                {
                    m_SceneGuid.Add(subScenes[i].SceneGUID);
                    m_SceneEntity.Add(Entity.Null);
                    m_SceneCooldown.Add(0);
                }
            }
        }

        m_Rng = Unity.Mathematics.Random.CreateFromIndex(1234);
    }

    void Update()
    {
        var defaultWorld = World.DefaultGameObjectInjectionWorld;
        var unloadParams = SceneSystem.UnloadParameters.DestroySceneProxyEntity | SceneSystem.UnloadParameters.DestroySectionProxyEntities;
        if (m_Rng.NextFloat() >= 1 - UnloadAllProbability)
        {
            // unload all scenes and reset cooldown
            for (int i = 0; i < m_SceneGuid.Count; i++)
            {
                if (m_SceneEntity[i] != Entity.Null)
                {
                    SceneSystem.UnloadScene(defaultWorld.Unmanaged, m_SceneEntity[i], unloadParams);
                    m_SceneEntity[i] = Entity.Null;
                    m_SceneCooldown[i] = CooldownFrames;
                }
            }
        }
        else
        {
            var loadParams = new SceneSystem.LoadParameters { Flags = SceneLoadFlags.NewInstance };
            for (int i = 0; i < m_SceneGuid.Count; i++)
            {
                if (m_SceneEntity[i] == Entity.Null)
                {
                    if (m_SceneCooldown[i] > 0)
                        m_SceneCooldown[i] -= 1;
                    if (m_SceneCooldown[i] == 0 && m_Rng.NextFloat() >= 1 - LoadProbability)
                        m_SceneEntity[i] = SceneSystem.LoadSceneAsync(defaultWorld.Unmanaged, m_SceneGuid[i], loadParams);
                }
                else
                {
                    float chance = UnloadProbability;
                    if (!SceneSystem.IsSceneLoaded(defaultWorld.Unmanaged, m_SceneEntity[i]))
                    {
                        // sometimes, we'll also unload scenes that haven't finished loading yet, just for good measure.
                        chance = UnloadWhileLoadingProbability;
                    }

                    if (m_Rng.NextFloat() >= 1 - chance)
                    {
                        SceneSystem.UnloadScene(defaultWorld.Unmanaged, m_SceneEntity[i], unloadParams);
                        m_SceneEntity[i] = Entity.Null;
                        m_SceneCooldown[i] = CooldownFrames < 0 ? 0 : CooldownFrames;
                    }
                }
            }
        }
    }
}
