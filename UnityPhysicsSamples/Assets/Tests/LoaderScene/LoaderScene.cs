using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
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
    ScrollRect m_EntryScrollRect;

    [SerializeField]
    SceneNavigation m_Navigation;
#pragma warning restore 649

    GameObject m_Selected;

    void Start()
    {
        m_Navigation.Loader = this;

        GameObject firstEntry = null;
        foreach (var scene in m_SceneData)
        {
            var entry = Instantiate(m_LoaderSceneEntry, m_EntryLayoutGroup.transform);
            entry.SetActive(true);
            var button = entry.GetComponentInChildren<Button>();
            button.onClick.AddListener(() =>
            {
                m_Navigation.gameObject.SetActive(true);
                SceneManager.LoadScene(scene.Index, LoadSceneMode.Single);
            });
            entry.GetComponentInChildren<Text>().text = scene.Name;
            if (firstEntry == null)
                firstEntry = button.gameObject;
        }

        if (firstEntry != null)
        {
            EventSystem.current.SetSelectedGameObject(firstEntry);
            m_Selected = firstEntry;
        }

        Application.targetFrameRate = (int)(1f / BasePhysicsDemo.DefaultWorld.GetExistingSystem<FixedStepSimulationSystemGroup>().Timestep);
    }

    internal void LoadLevel(int indexOffset)
    {
        BasePhysicsDemo.ResetDefaultWorld();
        var i = m_SceneData.FindIndex(s => s.Index == SceneManager.GetActiveScene().buildIndex);
        i += indexOffset;
        i = (i % m_SceneData.Count + m_SceneData.Count) % m_SceneData.Count;
        SceneManager.LoadScene(m_SceneData[i].Index, LoadSceneMode.Single);
    }

    void Update()
    {
        // update the scroll rect position if selection is outside current viewport
        if (m_Selected == EventSystem.current.currentSelectedGameObject)
            return;

        m_Selected = EventSystem.current.currentSelectedGameObject;
        if (m_Selected == null)
            return;

        var selectedRect = m_Selected.transform as RectTransform;
        var viewportRect = m_EntryScrollRect.viewport;
        var selectedInViewport =
            viewportRect.InverseTransformPoint(selectedRect.TransformPoint(selectedRect.rect.center));
        if (!viewportRect.rect.Contains(selectedInViewport))
        {
            var contentRect = m_EntryScrollRect.content;
            var selectedInContent =
                contentRect.InverseTransformPoint(selectedRect.TransformPoint(selectedRect.rect.center));
            var positionInContentRect = (Vector2)selectedInContent - contentRect.rect.position;
            var verticalValue = positionInContentRect.y / contentRect.rect.height;
            m_EntryScrollRect.verticalNormalizedPosition = verticalValue < m_EntryScrollRect.verticalNormalizedPosition
                ? verticalValue * (1f + m_EntryScrollRect.verticalScrollbar.size)
                : verticalValue;
        }
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
