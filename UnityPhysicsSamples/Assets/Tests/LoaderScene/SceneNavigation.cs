using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneNavigation : MonoBehaviour
{
#pragma warning disable 649
    [SerializeField]
    Button m_MenuButton;

    [SerializeField]
    Button m_PreviousButton;

    [SerializeField]
    Text m_Title;

    [SerializeField]
    Button m_NextButton;

    [SerializeField]
    Button m_ReloadButton;

    [SerializeField]
    EventSystem m_EventSystem;

    internal LoaderScene Loader;
#pragma warning restore 649

    void Start()
    {
        DontDestroyOnLoad(m_EventSystem);
        DontDestroyOnLoad(gameObject);

        m_MenuButton.onClick.AddListener(() =>
        {
            LoaderScene.ResetDefaultWorld();
            SceneManager.LoadScene(0, LoadSceneMode.Single);
            Destroy(gameObject);
            Destroy(m_EventSystem.gameObject);
        });

        m_PreviousButton.onClick.AddListener(() => { Loader.LoadLevel(-1); });

        m_NextButton.onClick.AddListener(() => { Loader.LoadLevel(1); });

        m_ReloadButton.onClick.AddListener(() => { Loader.LoadLevel(0); });

        OnSceneLoaded(SceneManager.GetActiveScene(), default);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        m_Title.text = scene.name;
        EventSystem.current.SetSelectedGameObject(m_NextButton.gameObject);
    }

    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;
}
