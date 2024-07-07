#if !UNITY_DISABLE_MANAGED_COMPONENTS
using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public class HackComponent : IComponentData
    {
        public AnimationClip[] Clip;
    }

    /// <summary>
    /// This is a necessary hack to make the ClientSideAnimation.unity scene work in standalone in the case
    /// where this scene is the only one added to the build settings.
    ///
    /// It fixes an issue during baking by forcing a reference the clips that
    /// is necessary for the animator state machine to work.
    /// </summary>
    public class Hack : MonoBehaviour
    {
        public GameObject Reference;

        class Baker : Baker<Hack>
        {
            public override void Bake(Hack authoring)
            {
                AddComponentObject(GetEntity(TransformUsageFlags.Dynamic), new HackComponent
                {
                    Clip = authoring.Reference.GetComponent<Animator>().runtimeAnimatorController.animationClips,
                });
            }
        }
    }
}
#endif
