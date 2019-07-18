using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityObject = UnityEngine.Object;

class LoaderScene : MonoBehaviour
{
    #if UNITY_EDITOR
    internal void SetScenes(IReadOnlyList<UnityEditor.EditorBuildSettingsScene> scenes)
    {
        m_SceneData.Clear();
        for (int i = 0, count = scenes.Count; i < count; ++i)
        {
            if (!scenes[i].enabled || scenes[i].path == gameObject.scene.path)
                continue;
            m_SceneData.Add(new SceneData { Name = Path.GetFileNameWithoutExtension(scenes[i].path), Index = i });
        }
    }
    #endif

#pragma warning disable 649
    [Serializable]
    struct SceneData
    {
        public string Name;
        public int Index;
    }
    
    [SerializeField, HideInInspector]
    List<SceneData> m_SceneData;

    [SerializeField]
    GameObject m_LoaderSceneEntry;

    [SerializeField]
    LayoutGroup m_EntryLayoutGroup;

    [SerializeField]
    SceneNavigation m_Navigation;
#pragma warning restore 649

    void Start()
    {
        m_Navigation.Loader = this;

        foreach (var scene in m_SceneData)
        {
            var entry = Instantiate(m_LoaderSceneEntry, m_EntryLayoutGroup.transform);
            entry.SetActive(true);
            entry.GetComponentInChildren<Button>().onClick.AddListener(() =>
            {
                m_Navigation.gameObject.SetActive(true);
                SceneManager.LoadScene(scene.Index, LoadSceneMode.Single);
            });
            entry.GetComponentInChildren<Text>().text = scene.Name;
        }

        Application.targetFrameRate = (int)(1f / Time.fixedDeltaTime);
    }

    internal void LoadLevel(int indexOffset)
    {
        World.Active.EntityManager.DestroyEntity(World.Active.EntityManager.CreateEntityQuery(Array.Empty<ComponentType>()));
        var i = m_SceneData.FindIndex(s => s.Index == SceneManager.GetActiveScene().buildIndex);
        i += indexOffset;
        i = (i % m_SceneData.Count + m_SceneData.Count) % m_SceneData.Count;
        SceneManager.LoadScene(m_SceneData[i].Index, LoadSceneMode.Single);
    }
}

#if UNITY_EDITOR
static class LoaderSceneConfigurator
{
    [UnityEditor.Callbacks.PostProcessScene]
    static void OnPostProcessScene()
    {
        var loader = UnityObject.FindObjectsOfType<LoaderScene>().FirstOrDefault();
        if (loader != null)
            loader.SetScenes(UnityEditor.EditorBuildSettings.scenes);
    }
}
#endif
