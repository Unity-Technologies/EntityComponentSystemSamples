using System;
using System.Linq;
using Unity.Entities;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevel;
using Hash128 = Unity.Entities.Hash128;
#if UNITY_EDITOR 
using Unity.Build;
#endif
#pragma warning disable 649

[ExecuteAlways]
public class SetBuildSettingsComponent : MonoBehaviour
{
#if UNITY_EDITOR 
    [SerializeField] 
    public BuildSettings _BuildSettingsA;
    [SerializeField]
    public BuildSettings _BuildSettingsB;
#endif
    
    [SerializeField]
    [HideInInspector]
    Hash128 _BuildSettingsGUIDA;

    [SerializeField]
    [HideInInspector]
    Hash128 _BuildSettingsGUIDB;
    
    
    private World worldA;
    private World worldB;

    static void SetBuildSettingOnWorld(Hash128 buildSettingsGUID, World world)
    {
        if (world == null)
            return;
        world.GetExistingSystem<SceneSystem>().BuildSettingsGUID = buildSettingsGUID;
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        _BuildSettingsGUIDA = new GUID(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_BuildSettingsA)));
        _BuildSettingsGUIDB = new GUID(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_BuildSettingsB)));
#endif

        SetBuildSettingOnWorld(_BuildSettingsGUIDA, worldA);
        SetBuildSettingOnWorld(_BuildSettingsGUIDB, worldB);
    }

    private void OnEnable()
    {
        var worldNameA = "BuildSettings Test World A";
        var worldNameB = "BuildSettings Test World B";
        
        World.DisposeAllWorlds();
        DefaultWorldInitialization.Initialize(worldNameA, !Application.isPlaying);
        DefaultWorldInitialization.Initialize(worldNameB, !Application.isPlaying);

        worldA = World.AllWorlds.First(w => w.Name == worldNameA);
        worldB = World.AllWorlds.First(w => w.Name == worldNameB);

        OnValidate();
                
        //@TODO: This API is confusing. Should be way more explicit.
        //       Current API makes it very easy to have the same system injected multiple times
        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(worldA, null);
        ScriptBehaviourUpdateOrder.UpdatePlayerLoop(worldB, PlayerLoop.GetCurrentPlayerLoop());
    }

    private void OnDisable()
    {
        World.DisposeAllWorlds();
        DefaultWorldInitialization.Initialize("Default World", !Application.isPlaying);
    }
}
