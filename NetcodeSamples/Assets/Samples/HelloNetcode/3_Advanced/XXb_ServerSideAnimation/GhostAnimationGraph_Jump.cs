using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using Unity.Mathematics;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
#if !UNITY_DISABLE_MANAGED_COMPONENTS
using Unity.NetCode.Hybrid;
#endif

namespace Samples.HelloNetcode.Hybrid
{
    public struct JumpAnimationData : IComponentData
    {
        [GhostField(Quantization=1000)] public float Phase;
        [GhostField(Quantization=1000)] public float aimPitch;
    }

#if !UNITY_DISABLE_MANAGED_COMPONENTS
    public class JumpGhostPlayableBehaviour : GhostPlayableBehaviour
    {
        public const float k_JumpDuration = 0.2f;
        GhostAnimationController m_controller;
        AnimationClipPlayable m_clip;
        AnimationClipPlayable m_clipAim;

        // OnPlayableCreate
        // OnPlayableDestroy

        public override void PreparePredictedData(NetworkTick serverTick, float deltaTime, bool isRollback)
        {
            ref var jumpData = ref m_controller.GetPlayableDataRef<JumpAnimationData>();

            var input = m_controller.GetEntityComponentData<CharacterControllerPlayerInput>();
            var character = m_controller.GetEntityComponentData<Character>();

            if (character.OnGround == 0)
            {

                var trans = m_controller.GetEntityComponentData<LocalTransform>();
                trans.Rotation = quaternion.RotateY(input.Yaw);
                m_controller.SetEntityComponentData(trans);

            }

            jumpData.aimPitch = 90 + input.Pitch * 180.0f / 3.1415f;

            float jumpTime = 0;
            if (character.JumpStart.IsValid)
            {
                var jumpStart = character.JumpStart;
                if (jumpStart == serverTick)
                    jumpTime = 0;
                else
                {
                    jumpStart.Increment();
                    var deltaTicks = serverTick.TicksSince(jumpStart);
                    jumpTime = deltaTime + deltaTicks / 60f;
                    if (jumpTime > k_JumpDuration)
                        jumpTime = k_JumpDuration;
                }
            }

            jumpData.Phase = jumpTime / k_JumpDuration;
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            var jumpData = m_controller.GetPlayableData<JumpAnimationData>();
            m_clip.SetTime(jumpData.Phase * m_clip.GetDuration());

            // Update aim
            float aimPitchFraction = jumpData.aimPitch / 180.0f;
            m_clipAim.SetTime(aimPitchFraction * m_clipAim.GetDuration());

            base.PrepareFrame(playable, info);
        }
        public void Initialize(GhostAnimationController controller, PlayableGraph graph, Playable owner, AnimationClip clip, AnimationClip aimClip)
        {
            m_controller = controller;
            m_clip = AnimationClipPlayable.Create(graph, clip);
            m_clip.SetDuration(clip.length);

            m_clipAim = AnimationClipPlayable.Create(graph, aimClip);
            //m_clipAim.SetApplyFootIK(false);
            //m_clipAim.Pause();
            m_clipAim.SetDuration(aimClip.length);

            // Setup other additive mixer
            var additiveMixer = AnimationLayerMixerPlayable.Create(graph);
            var locoMixerPort = additiveMixer.AddInput(m_clip, 0);
            additiveMixer.SetInputWeight(locoMixerPort, 1);

            var aimMixerPort = additiveMixer.AddInput(m_clipAim, 0);
            additiveMixer.SetInputWeight(aimMixerPort, 1);
            additiveMixer.SetLayerAdditive((uint)aimMixerPort, true);

            owner.SetInputCount(1);
            graph.Connect(additiveMixer, 0, owner, 0);
            owner.SetInputWeight(0, 1);
        }
    }

    [CreateAssetMenu(fileName = "Jump", menuName = "NetCode/Animation/Jump")]
    public class GhostAnimationGraph_Jump : GhostAnimationGraphAsset
    {
        public AnimationClip Clip;
        public AnimationClip AimClip;
        public override Playable CreatePlayable(GhostAnimationController controller, PlayableGraph graph, List<GhostPlayableBehaviour> behaviours)
        {
            var behaviourPlayable = ScriptPlayable<JumpGhostPlayableBehaviour>.Create(graph);
            var behaviour = behaviourPlayable.GetBehaviour();
            // This registers the behaviour for receiving PreparePredictedData, skip this if predicted data is updated by a system (PrepareFrame is still called)
            behaviours.Add(behaviour);

            behaviour.Initialize(controller, graph, behaviourPlayable, Clip, AimClip);
            return behaviourPlayable;
        }
        public override void RegisterPlayableData(IRegisterPlayableData register)
        {
            register.RegisterPlayableData<JumpAnimationData>();
        }
    }
#endif
}
