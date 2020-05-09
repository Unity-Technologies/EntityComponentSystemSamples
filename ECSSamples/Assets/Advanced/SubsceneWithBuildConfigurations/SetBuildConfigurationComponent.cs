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
public class SetBuildConfigurationComponent : MonoBehaviour
{
#if UNITY_EDITOR 
    [SerializeField] 
    public BuildConfiguration _BuildConfigurationA;
    [SerializeField]
    public BuildConfiguration _BuildConfigurationB;
#endif
    
    [SerializeField]
    [HideInInspector]
    Hash128 _BuildConfigurationGUIDA;

    [SerializeField]
    [HideInInspector]
    Hash128 _BuildConfigurationGUIDB;
    
    
    private World worldA;
    private World worldB;

    static void SetBuildConfigurationOnWorld(Hash128 buildConfigurationGUID, World world)
    {
        if (world == null)
            return;
        world.GetExistingSystem<SceneSystem>().BuildConfigurationGUID = buildConfigurationGUID;
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        _BuildConfigurationGUIDA = new GUID(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_BuildConfigurationA)));
        _BuildConfigurationGUIDB = new GUID(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(_BuildConfigurationB)));
#endif

        SetBuildConfigurationOnWorld(_BuildConfigurationGUIDA, worldA);
        SetBuildConfigurationOnWorld(_BuildConfigurationGUIDB, worldB);
    }

    private void OnEnable()
    {
        var worldNameA = "BuildConfiguration Test World A";
        var worldNameB = "BuildConfiguration Test World B";
        
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
