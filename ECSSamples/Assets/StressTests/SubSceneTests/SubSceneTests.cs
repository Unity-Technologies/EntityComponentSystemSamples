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
using UnityEngine.TestTools;

public class SubSceneTests : ECSTestsFixture
{
    [Test]
    [Ignore("Hybrid conversion doesn't appear to be deterministic")]
    public void CheckEntitySceneImporterDeterminism_HybridLight()
    {
        EntitySceneImporterDeterminismChecker.Check("Assets/StressTests/HybridLights/HybridLights/SubScene.unity");
    }

    [Test]
    [Ignore("Instability - DOTS-4581")]
    public void CheckEntitySceneImporterDeterminism([Values(
        "Assets/StressTests/LODSubSceneTest/LodSubSceneDynamicAndStatic.unity",
        "Assets/Advanced/BlobAssetScalable/BlobAsset/Subscene.unity"
    )]string path)
    {
        EntitySceneImporterDeterminismChecker.Check(path);
    }

    [Test]
    // Disabled on Linux because it hangs during asset import - likely related to DOTS-4581 and running on the latest Ubuntu Bokken VM
    [UnityPlatform(exclude = new[] {RuntimePlatform.LinuxEditor})]
    public void SynchronousLoad()
    {
        TestUtilities.RegisterSystems(World, TestUtilities.SystemCategories.Streaming);

        var loadParams = new SceneSystem.LoadParameters
        {
            Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn
        };

        var sceneGUID = new GUID(AssetDatabase.AssetPathToGUID("Assets/StressTests/SubSceneTests/SubSceneHost/SubSceneA.unity"));

        SceneSystem.LoadSceneAsync(World.Unmanaged, sceneGUID, loadParams);
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
    // Disabled on Linux because it hangs during asset import - likely related to DOTS-4581 and running on the latest Ubuntu Bokken VM
    [UnityPlatform(exclude = new[] {RuntimePlatform.LinuxEditor})]
    public void SimpleCompanionComponent()
    {
        TestUtilities.RegisterSystems(World, TestUtilities.SystemCategories.Streaming | TestUtilities.SystemCategories.CompanionComponents);

        var loadParams = new SceneSystem.LoadParameters
        {
            Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn
        };

        var sceneGUID = new GUID(AssetDatabase.AssetPathToGUID("Assets/StressTests/HybridLights/HybridLights/SubScene.unity"));

        SceneSystem.LoadSceneAsync(World.Unmanaged, sceneGUID, loadParams);
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
