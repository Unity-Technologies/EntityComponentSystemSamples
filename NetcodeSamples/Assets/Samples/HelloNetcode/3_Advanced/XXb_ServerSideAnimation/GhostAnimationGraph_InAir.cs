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
    public struct InAirAnimationData : IComponentData
    {
        [GhostField(Quantization=1000)] public float Phase;
        [GhostField(Quantization=1000)] public float aimPitch;
    }

#if !UNITY_DISABLE_MANAGED_COMPONENTS
    public class InAirGhostPlayableBehaviour : GhostPlayableBehaviour
    {
        GhostAnimationController m_controller;
        AnimationClipPlayable m_clip;
        AnimationClipPlayable m_clipAim;

        // OnPlayableCreate
        // OnPlayableDestroy

        public override void PreparePredictedData(NetworkTick serverTick, float deltaTime, bool isRollback)
        {
            ref var inAirData = ref m_controller.GetPlayableDataRef<InAirAnimationData>();

            var input = m_controller.GetEntityComponentData<CharacterControllerPlayerInput>();
            var character = m_controller.GetEntityComponentData<Character>();

            if (character.OnGround == 0)
            {

                var trans = m_controller.GetEntityComponentData<LocalTransform>();
                trans.Rotation = quaternion.RotateY(input.Yaw);
                m_controller.SetEntityComponentData(trans);

            }

            inAirData.aimPitch = 90 + input.Pitch * 180.0f / 3.1415f;

            inAirData.Phase += deltaTime / (float)m_clip.GetDuration();
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            var inAirData = m_controller.GetPlayableData<InAirAnimationData>();
            m_clip.SetTime(inAirData.Phase * m_clip.GetDuration());

            // Update aim
            float aimPitchFraction = inAirData.aimPitch / 180.0f;
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

    [CreateAssetMenu(fileName = "InAir", menuName = "NetCode/Animation/InAir")]
    public class GhostAnimationGraph_InAir : GhostAnimationGraphAsset
    {
        public AnimationClip Clip;
        public AnimationClip AimClip;
        public override Playable CreatePlayable(GhostAnimationController controller, PlayableGraph graph, List<GhostPlayableBehaviour> behaviours)
        {
            var behaviourPlayable = ScriptPlayable<InAirGhostPlayableBehaviour>.Create(graph);
            var behaviour = behaviourPlayable.GetBehaviour();
            // This registers the behaviour for receiving PreparePredictedData, skip this if predicted data is updated by a system (PrepareFrame is still called)
            behaviours.Add(behaviour);

            behaviour.Initialize(controller, graph, behaviourPlayable, Clip, AimClip);
            return behaviourPlayable;
        }
        public override void RegisterPlayableData(IRegisterPlayableData register)
        {
            register.RegisterPlayableData<InAirAnimationData>();
        }
    }
#endif
}
