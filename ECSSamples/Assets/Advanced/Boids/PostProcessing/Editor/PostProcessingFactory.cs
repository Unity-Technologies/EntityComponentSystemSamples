using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEditor.ProjectWindowCallback;
using System.IO;

namespace UnityEditor.PostProcessing
{
    public class PostProcessingFactory
    {
        [MenuItem("Assets/Create/Post-Processing Profile", priority = 201)]
        static void MenuCreatePostProcessingProfile()
        {
            var icon = (Texture2D)null; // TODO: Post-processing profile texture
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<DoCreatePostProcessingProfile>(), "New Post-Processing Profile.asset", icon, null);
        }

        internal static PostProcessingProfile CreatePostProcessingProfileAtPath(string path)
        {
            var profile = ScriptableObject.CreateInstance<PostProcessingProfile>();
            profile.name = Path.GetFileName(path);
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }
    }

    class DoCreatePostProcessingProfile : EndNameEditAction
    {
        public override void Action(int instanceId, string pathName, string resourceFile)
        {
            PostProcessingProfile profile = PostProcessingFactory.CreatePostProcessingProfileAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(profile);
        }
    }
}
