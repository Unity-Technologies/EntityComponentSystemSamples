using System.IO;
using UnityEngine;
using Unity.Entities.Content;

namespace ContentManagement.Sample
{
    [System.Flags]
    public enum ContentSourcePath : byte
    {
        Local = 1 << 0,
        Remote = 1 << 1,
    }
    
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    [CreateAssetMenu(fileName = "SceneListSO", menuName = "Scriptable Objects/SceneListSO")]
    public class WeakSceneListScriptableObject : ScriptableObject
    {
        public ContentSourcePath ContentSource;

        // in the sample, we only have one scene to publish to the catalog, but we make this
        // an array to demonstrate how you could publish multiple scenes
        public WeakObjectSceneReference[] LocalScenes;
        public WeakObjectSceneReference[] RemoteScenes;

        public static string RootPath = Directory.GetParent(Application.dataPath).FullName;

        // a 'set' is a unit of downloadable content within a catalog
        public static string ContentSetName = "remote";
        public static string ContentDir = "Catalog";
        public static string ContentPath = Path.Combine(RootPath, ContentDir) + Path.DirectorySeparatorChar;

        // The RemoteURL can be set to either a local file path or a cloud URL.
        //
        // - To use local content (e.g., during development or on a device), set RemoteURL to a file path using the "file:///" schema:
        //     Example: "file:///C:/git/content-management-sample/"
        //   This will cause the content manager to load assets from the specified local directory, instead of from StreamingAssets.
        //
        // - To use remote/cloud content, set RemoteURL to an HTTP/HTTPS path or an IP:
        //     Example: "https://domain.com/content/ or https://127.0.0.1/content/"
        //   This allows the content manager to download (assets, catalog, dependencies) from a server.
        //
        // Switching the RemoteURL effectively changes the source location for managed content.
        public static readonly string RemoteURL = "file:///C:/git/content-management-sample/";

        // the player will store downloaded objects from content catalogs in this cache
        public static string CachePath = Path.GetFullPath(Path.Combine(RootPath, "Cache"));
    }
}