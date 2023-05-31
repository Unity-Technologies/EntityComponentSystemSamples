#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif
using Unity.Entities;
using Hash128 = Unity.Entities.Hash128;

public struct SceneListData : IComponentData
{
    public Hash128 Level1A;
    public Hash128 Level1B;
    public Hash128 Level2A;
    public Hash128 Level2B;
}

#if UNITY_EDITOR
public class SceneListAuthoring : MonoBehaviour
{
    // public List<SceneAsset> Level1;
    // public List<SceneAsset> Level2;
    public SceneAsset Level1A;
    public SceneAsset Level1B;
    public SceneAsset Level2A;
    public SceneAsset Level2B;
}

public class SceneListAuthoringBaker : Baker<SceneListAuthoring>
{
    public override void Bake(SceneListAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new SceneListData()
        {
            Level1A = new GUID(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(authoring.Level1A))),
            Level1B = new GUID(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(authoring.Level1B))),
            Level2A = new GUID(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(authoring.Level2A))),
            Level2B = new GUID(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(authoring.Level2B)))
        });
    }
}
#endif
