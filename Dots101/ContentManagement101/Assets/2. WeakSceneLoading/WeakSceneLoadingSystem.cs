using Unity.Entities;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ContentManagement.Sample
{
    public partial struct WeakSceneLoadingSystem : ISystem
    {
        private bool init;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ContentIsReady>();
            state.RequireForUpdate<HighLowWeakScene>();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            var weakScene = SystemAPI.GetSingleton<HighLowWeakScene>();

            var loadParams = new SceneSystem.LoadParameters
            {
                AutoLoad = true,
                Flags = SceneLoadFlags.LoadAdditive
            };

            if (!init)
            {
                Debug.Log("Hit Enter to toggle between low and high fidelity");
                
                // initial load of the low-fidelity scene
                weakScene.LoadedScene = SceneSystem.LoadSceneAsync(state.WorldUnmanaged, weakScene.LowSceneRef.Id.GlobalId.AssetGUID, loadParams);
                
                SystemAPI.SetSingleton(weakScene);
                init = true;
                return;
            }
            
            // only switch scenes when user hits enter key
            if (!Keyboard.current.enterKey.wasPressedThisFrame)
            {
                return;
            }

            // toggle between the two scenes
            SceneSystem.UnloadScene(state.WorldUnmanaged, weakScene.LoadedScene);  // unload current scene
            
            if (weakScene.IsHighLoaded)
            {
                // load low fidelity
                weakScene.LoadedScene = SceneSystem.LoadSceneAsync(state.WorldUnmanaged, weakScene.LowSceneRef.Id.GlobalId.AssetGUID, loadParams);
                weakScene.IsHighLoaded = false;
            }
            else
            {
                // load high fidelity
                weakScene.LoadedScene = SceneSystem.LoadSceneAsync(state.WorldUnmanaged, weakScene.HighSceneRef.GlobalId.AssetGUID, loadParams);
                weakScene.IsHighLoaded = true;
            }
            
            SystemAPI.SetSingleton(weakScene);
        }
    }
}