using NUnit.Framework;
using Unity.Entities;
using Unity.Entities.Hybrid.EndToEnd.Tests;
using Unity.Entities.Tests;
using Unity.Mathematics;
using Unity.Scenes;
using Unity.Scenes.Tests;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;

public class SubSceneTests : ECSTestsFixture
{
    [Test]
    [Ignore("Hybrid conversion doesn't appear to be deterministic")]
    public void CheckEntitySceneImporterDeterminism_HybridLight()
    {
        EntitySceneImporterDeterminismChecker.Check("Assets/StressTests/HybridLights/HybridLights/SubScene.unity");
    }

    [Test]
    public void CheckEntitySceneImporterDeterminism([Values(
        "Assets/StressTests/LODSubSceneTest/LodSubSceneDynamicAndStatic.unity",
        "Assets/Advanced/BlobAssetScalable/BlobAsset/Subscene.unity"
    )]string path)
    {
        EntitySceneImporterDeterminismChecker.Check(path);
    }

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

#if !UNITY_DISABLE_MANAGED_COMPONENTS
    [Test]
    public void SimpleHybridComponent()
    {
        TestUtilities.RegisterSystems(World, TestUtilities.SystemCategories.Streaming | TestUtilities.SystemCategories.HybridComponents);

        var loadParams = new SceneSystem.LoadParameters
        {
            Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn
        };

        var sceneGUID = new GUID(AssetDatabase.AssetPathToGUID("Assets/StressTests/HybridLights/HybridLights/SubScene.unity"));

        World.GetExistingSystem<SceneSystem>().LoadSceneAsync(sceneGUID, loadParams);
        World.Update();


        var entity = EmptySystem.GetSingletonEntity<Light>();
        var companion = m_Manager.GetComponentObject<Light>(entity).gameObject;

        void MoveAndCheck(float3 testpos)
        {
            m_Manager.SetComponentData(entity, new LocalToWorld { Value = float4x4.Translate(testpos) });
            World.Update();
            Assert.AreEqual(testpos, (float3)companion.transform.position);
        }

        MoveAndCheck(new float3(1, 2, 3));
        MoveAndCheck(new float3(2, 3, 4));
    }

#endif
}
