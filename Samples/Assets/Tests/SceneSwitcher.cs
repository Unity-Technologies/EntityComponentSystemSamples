using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    [Serializable]
    public class SceneConfig
    {
        public string SceneName;
        public int CustomDuration;
    }
    public int SceneSwitchInterval = 5;

    public float TimeUntilNextSwitch = 0.0f;

    public int CurrentSceneIndex = 0;

    public bool EntitiesDestroyed = false;

    public SceneConfig[] SceneConfigs;

	// Use this for initialization
	void Start ()
	{
	    DontDestroyOnLoad(this);
	    LoadNextScene();
	}

    private void DestroyAllEntitiesInScene()
    {
        var entityManager = World.Active.EntityManager;
        var entities = entityManager.GetAllEntities();
        entityManager.DestroyEntity(entities);
        entities.Dispose();
        EntitiesDestroyed = true;
    }

    private void LoadNextScene()
    {
        var sceneCount = SceneManager.sceneCountInBuildSettings;
        var nextIndex = CurrentSceneIndex + 1;
        if (nextIndex >= sceneCount)
        {
            Quit();
            return;
        }

        var nextScene = SceneUtility.GetScenePathByBuildIndex(nextIndex);
        TimeUntilNextSwitch = GetSceneDuration(nextScene);
        CurrentSceneIndex = nextIndex;

        SceneManager.LoadScene(nextIndex);
        EntitiesDestroyed = false;
    }

    private int GetSceneDuration(string scenePath)
    {
        foreach (var scene in SceneConfigs)
        {
            if (!scenePath.EndsWith(scene.SceneName +".unity"))
                continue;
            if (scene.CustomDuration <= 0)
                continue;
            return scene.CustomDuration;
        }

        return SceneSwitchInterval;
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif

    }

    // Update is called once per frame
	void Update ()
	{
	    TimeUntilNextSwitch -= Time.deltaTime;
	    if (TimeUntilNextSwitch > 0.0f)
	        return;

	    if (!EntitiesDestroyed)
	    {
	        DestroyAllEntitiesInScene();
	    }
	    else
	    {
	        DestroyAllEntitiesInScene();
	        LoadNextScene();
	    }
	}
}
