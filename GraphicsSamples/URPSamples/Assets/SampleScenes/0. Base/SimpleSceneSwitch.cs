using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using Unity.Scenes;
using UnityEngine.SceneManagement;

public class SimpleSceneSwitch : MonoBehaviour
{
    public float scale = 1f;
    public bool useInputButton = false;
    private GUIStyle customButton;

    public void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    public void Start()
    {
        if(SceneManager.GetActiveScene().buildIndex ==0)
        {
            NextScene();
        }
    }

    void Update()
    {       
        if(useInputButton)
        {
            if (Input.GetButtonDown("Fire1"))
            {
                NextScene();
            }

            if (Input.GetButtonDown("Fire2"))
            {
                PrevScene();
            }
        }
    }

    void OnGUI()
    {
        if(customButton == null)
        {
            customButton = new GUIStyle("button");
            customButton.fontSize = GUI.skin.label.fontSize;
        }
        
        GUI.skin.label.fontSize = Mathf.RoundToInt ( 16 * scale );
        GUI.color = new Color(1, 1, 1, 1);
        float w = 410 * scale;
        float h = 90 * scale;
        GUILayout.BeginArea(new Rect(Screen.width - w -5, Screen.height - h -5, w, h), GUI.skin.box);

        GUILayout.BeginHorizontal();
        if(useInputButton)
        {
            GUILayout.Label("Press Fire1 / Fire2 to switch scene",GUILayout.Height(50 * scale));
        }
        else
        {
            if(GUILayout.Button("\n Prev \n",customButton,GUILayout.Width(200 * scale), GUILayout.Height(50 * scale))) PrevScene();
            if(GUILayout.Button("\n Next \n",customButton,GUILayout.Width(200 * scale), GUILayout.Height(50 * scale))) NextScene();
        }
        GUILayout.EndHorizontal();

        int currentpage = SceneManager.GetActiveScene().buildIndex;
        int totalpages = SceneManager.sceneCountInBuildSettings-1;
        GUILayout.Label( currentpage + " / " + totalpages + " " + SceneManager.GetActiveScene().name );

        GUILayout.EndArea();
    }

    public void NextScene()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        int loadIndex = sceneIndex+1;

        if (loadIndex >= SceneManager.sceneCountInBuildSettings)
        {
            loadIndex = 1;
        }

        SwitchScene(loadIndex);
    }

    public void PrevScene()
    {
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        int loadIndex = sceneIndex-1;

        if (loadIndex <= 0)
        {
            loadIndex = SceneManager.sceneCountInBuildSettings-1;
        }

        SwitchScene(loadIndex);
    }

    public void SwitchScene(int loadIndex)
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var entityManager = world.EntityManager;
        var entities = entityManager.GetAllEntities();

        for (int i = 0; i < entities.Length; i++)
        {
            string ename = entityManager.GetName(entities[i]);
            bool isSubscene = entityManager.HasComponent<SubScene>(entities[i]);
            bool isSceneSection = entityManager.HasComponent<SceneSection>(entities[i]);

            //Runtime generated entities requires manual deletion, 
            //but we need to skip for some specific entities otherwise there will be spamming error
            if( ename != "SceneSectionStreamingSingleton" && !isSubscene && !isSceneSection && !ename.Contains("GameObject Scene:") )
            {
                entityManager.DestroyEntity(entities[i]);
            }
        }

        SceneManager.LoadScene(loadIndex);
    }
}
