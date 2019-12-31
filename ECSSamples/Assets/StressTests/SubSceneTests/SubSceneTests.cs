using NUnit.Framework;
using Unity.Entities;
using Unity.Entities.Hybrid.EndToEnd.Tests;
using Unity.Entities.Tests;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;

public class SubSceneTests : ECSTestsFixture
{
    [Test]
    public void SynchronousLoad()
    {
        TestUtilities.RegisterSystems(World, TestUtilities.SystemCategories.Streaming);

        var loadParams = new SceneSystem.LoadParameters
        {
            Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn
        };

        var sceneGUID = new GUID(AssetDatabase.AssetPathToGUID("Assets/StressTests/SubSceneTests/SubSceneHost/SubSceneA.unity"));

        World.GetExistingSystem<SceneSystem>().LoadSceneAsync(sceneGUID, loadParams);
        World.Update();

        // Expected entities:
        // 1. Scene
        // 2. Section
        // 3. Cube
        // 4. Public references
        // 5. Time singleton
        // 6. RetainBlobAssets
        Assert.AreEqual(6, m_Manager.UniversalQuery.CalculateEntityCount());
    }
}