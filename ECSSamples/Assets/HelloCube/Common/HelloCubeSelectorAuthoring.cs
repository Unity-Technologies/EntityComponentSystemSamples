using Unity.Entities;
using Unity.Scenes;
using UnityEngine;

namespace HelloCube
{
    public abstract class SceneSelectorGroup : ComponentSystemGroup
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            var subScene = Object.FindObjectOfType<SubScene>();
            if (subScene != null)
            {
                Enabled = SceneName == subScene.gameObject.scene.name;
            }
            else
            {
                Enabled = false;
            }
        }

        protected abstract string SceneName { get; }
    }

    public class HelloCubeGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(HelloCubeGroup))]
    public class MainThreadGroup : SceneSelectorGroup
    {
        protected override string SceneName => "HelloCube_MainThread";
    }

    [UpdateInGroup(typeof(HelloCubeGroup))]
    [UpdateAfter(typeof(MainThreadGroup))]
    public class JobEntityGroup : SceneSelectorGroup
    {
        protected override string SceneName => "HelloCube_IJobEntity";
    }

    [UpdateInGroup(typeof(HelloCubeGroup))]
    [UpdateAfter(typeof(JobEntityGroup))]
    public class AspectsGroup : SceneSelectorGroup
    {
        protected override string SceneName => "HelloCube_Aspects";
    }

    [UpdateInGroup(typeof(HelloCubeGroup))]
    [UpdateAfter(typeof(AspectsGroup))]
    public class PrefabsGroup : SceneSelectorGroup
    {
        protected override string SceneName => "HelloCube_Prefabs";
    }

    [UpdateInGroup(typeof(HelloCubeGroup))]
    [UpdateAfter(typeof(PrefabsGroup))]
    public class JobChunkGroup : SceneSelectorGroup
    {
        protected override string SceneName => "HelloCube_IJobChunk";
    }
}
