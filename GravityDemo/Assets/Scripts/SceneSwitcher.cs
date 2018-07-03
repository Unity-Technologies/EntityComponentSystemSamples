using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public float SceneSwitchInterval = 5.0f;

    public float TimeUntilNextSwitch = 0.0f;

    public int CurrentSceneIndex = 0;
	// Use this for initialization
	void Start ()
	{
	    DontDestroyOnLoad(this);
	    LoadNextScene();
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

        TimeUntilNextSwitch = SceneSwitchInterval;
        CurrentSceneIndex = nextIndex;

        var entityManager = World.Active.GetExistingManager<EntityManager>();
        var entities = entityManager.GetAllEntities();
        entityManager.DestroyEntity(entities);
        entities.Dispose();

        SceneManager.LoadScene(nextIndex);


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

	    LoadNextScene();
	}
}
