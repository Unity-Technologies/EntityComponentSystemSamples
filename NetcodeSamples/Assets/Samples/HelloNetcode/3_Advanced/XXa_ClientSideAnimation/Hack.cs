using UnityEngine;
#if false // guarded with the disabled hack below
using Unity.Entities;
#endif

namespace Samples.HelloNetcode
{
    // The managed-component part of this sample hack is disabled (see the #if false block below), parked
    // during the removal of managed components.
    //
    // Background: dots PR #9042 (2023, no Jira number recorded) added this "Hack" scene to work around a
    // runtime bug. Baked entity references to UnityEngine.Objects were deserialized in the wrong order on
    // awake, so the Animator's animation clip references came up unassigned in a standalone build of
    // ClientSideAnimation.unity. HackComponent forced baking to keep a reference to the clips (as a managed
    // AnimationClip[]) so they were serialized into the build.
    //
    // It relied on Baker.AddComponentObject, which has been removed along with managed-component baking.
    // The component and its baker are disabled rather than converted on purpose: the bug is about how baked
    // UnityEngine.Object references serialize, so switching the clips to UnityObjectRef<AnimationClip> would
    // change that exact path and could quietly bring the original bug back.
    //
    // The Hack MonoBehaviour is kept compiled because ClientSideAnimation/Hack.unity references it; removing
    // it would leave a missing script in that subscene.
    //
    // To pick this up: check whether the serialization-ordering bug still reproduces in a standalone build.
    // If it's fixed, delete this file and the Hack subscene. If not, re-implement the workaround with an
    // unmanaged component (e.g. a DynamicBuffer of UnityObjectRef<AnimationClip>) and confirm the clips still
    // end up in the standalone build.
    public class Hack : MonoBehaviour
    {
        public GameObject Reference;
    }

#if false
    public class HackComponent : IComponentData
    {
        public AnimationClip[] Clip;
    }

    public class HackBaker : Baker<Hack>
    {
        public override void Bake(Hack authoring)
        {
            AddComponentObject(GetEntity(TransformUsageFlags.Dynamic), new HackComponent
            {
                Clip = authoring.Reference.GetComponent<Animator>().runtimeAnimatorController.animationClips,
            });
        }
    }
#endif
}
