using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Entities.Content;
using Unity.Entities.Serialization;

namespace ContentManagement.Sample
{
#if UNITY_EDITOR 
    public class WeakSceneAuthoring : MonoBehaviour
    {
        public WeakSceneListScriptableObject Settings;
        public UnityEditor.SceneAsset HighFidelityScene;
        // We use WeakObjectSceneReference here instead of UntypedWeakReferenceId because
        // UntypedWeakReferenceId does not take a scene as input in the inspector.
        // Alternatively, we could use SceneAsset and then create a WeakObjectSceneReference in the baker with this code:
        // new WeakObjectSceneReference { Id = UntypedWeakReferenceId.CreateFromObjectInstance(authoring.LowFidelityScene) };
        public WeakObjectSceneReference LowFidelityScene;
       
        class Baker : Baker<WeakSceneAuthoring>
        {
            public override void Bake(WeakSceneAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                
                AddComponent(entity, new HighLowWeakScene
                {
                    HighSceneRef = UntypedWeakReferenceId.CreateFromObjectInstance(authoring.HighFidelityScene),
                    LowSceneRef = authoring.LowFidelityScene,
                });

                if ((authoring.Settings.ContentSource & ContentSourcePath.Local) != 0)
                    AddComponent<LocalContent>(entity);

                if ((authoring.Settings.ContentSource & ContentSourcePath.Remote) != 0)
                    AddComponent(entity, new RemoteContent { URL = authoring.Settings.RemoteURL });
            }
        }

    }
#endif

    public struct HighLowWeakScene : IComponentData
    {
        public UntypedWeakReferenceId HighSceneRef;
        public WeakObjectSceneReference LowSceneRef;

        public bool IsHighLoaded;   // indicates whether high- or low-fidelity is currently loaded
        public Entity LoadedScene;  // stores a reference to the currently loaded scene (null if no scene is currently loaded)
    }

    public struct ContentIsReady : IComponentData
    {
    }

    public struct RemoteContent : IComponentData
    {
        public FixedString512Bytes URL;
    }

    public struct LocalContent : IComponentData
    {
    }
}