using System.Collections;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEditor.SceneManagement;
using UnityEditor;

public class Editmode_001
{
    //https://stackoverflow.com/questions/32809888/how-can-i-save-unity-statistics-or-unity-profiler-statistics-stats-on-cpu-rend

    public string scenePath = "Assets/Tests/Editor/EditmodeTests/Editmode_001.unity";

    [UnityTest]
    public IEnumerator Test_Editmode_001_Cube1()
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        yield return null;
        yield return new EnterPlayMode();
        GameObject cube = GameObject.Find("Cube1");
        Assert.That(cube, !Is.EqualTo(null), "Oh no Cube1 is null");
    }

    [UnityTest]
    public IEnumerator Test_Editmode_001_Cube2()
    {
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        yield return null;
        yield return new EnterPlayMode();
        GameObject cube = GameObject.Find("Cube2");
        Assert.That(cube, !Is.EqualTo(null), "Oh no Cube2 is null");
    }
}
