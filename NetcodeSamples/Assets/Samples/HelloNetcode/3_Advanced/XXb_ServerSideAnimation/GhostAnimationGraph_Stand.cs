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
    public struct StandAnimationData : IComponentData
    {
        [GhostField(Quantization=1000)] public float aimPitch;
        [GhostField(Quantization=1000)] public float aimYaw;
        [GhostField(Quantization=1000)] public float remainingTurnAngle;
    }
#if !UNITY_DISABLE_MANAGED_COMPONENTS
    public class StandGhostPlayableBehaviour : GhostPlayableBehaviour
    {
        enum LocoMixerPort
        {
            Idle,
            TurnL,
            TurnR,
            Count
        }
        enum AimMixerPort
        {
            AimLeft,
            AimMid,
            AimRight,
            Count
        }

        GhostAnimationController m_controller;
        AnimationMixerPlayable m_locomotionMixer;
        AnimationClipPlayable m_clipIdle;
        AnimationClipPlayable m_clipTurnL;
        AnimationClipPlayable m_clipTurnR;

        AnimationMixerPlayable m_aimMixer;
        AnimationClipPlayable m_clipAimL;
        AnimationClipPlayable m_clipAimMid;
        AnimationClipPlayable m_clipAimR;

        AnimationLayerMixerPlayable m_additiveMixer;

        // OnPlayableCreate
        // OnPlayableDestroy

        public override void PreparePredictedData(NetworkTick serverTick, float deltaTime, bool isRollback)
        {
            ref var standData = ref m_controller.GetPlayableDataRef<StandAnimationData>();

            var input = m_controller.GetEntityComponentData<CharacterControllerPlayerInput>();
            var character = m_controller.GetEntityComponentData<Character>();


            var rot = m_controller.GetEntityComponentData<LocalTransform>().Rotation;


            standData.aimPitch = 90 + input.Pitch * 180.0f / 3.1415f;

            // aimYaw is local rotation
            var localRot = quaternion.RotateY(input.Yaw);
            localRot = math.mul(localRot, math.inverse(rot));
            standData.aimYaw = math.acos(localRot.value.w) * 2f * 180.0f / 3.1415f;
            if (localRot.value.y < 0)
                standData.aimYaw = -standData.aimYaw;

            float turnThreshold = 90.0f;
            float turnFastThreshold = 100.0f;
            float turnAngle = 90.0f;
            float turnSpeed = 125.0f;

            if (m_controller.ApplyRootMotion && isRollback && standData.remainingTurnAngle != 0)
            {
                var fraction = 1f - math.abs(standData.remainingTurnAngle / turnAngle);
                m_clipTurnL.SetTime(m_clipTurnL.GetAnimationClip().length * fraction);
                m_clipTurnR.SetTime(m_clipTurnR.GetAnimationClip().length * fraction);
            }


            if (standData.remainingTurnAngle == 0 && math.abs(standData.aimYaw) > turnThreshold)
            {
                standData.remainingTurnAngle = turnAngle*math.sign(standData.aimYaw);
            }

            // Turning update
            if (standData.remainingTurnAngle != 0)
            {
                if (math.abs(standData.aimYaw) > turnFastThreshold)
                {
                    var factor = 1.0f - (180 - math.abs(standData.aimYaw)) / turnFastThreshold;
                    turnSpeed = turnSpeed + factor * 300;
                }
                var deltaAngle = deltaTime * turnSpeed * math.sign(standData.remainingTurnAngle);
                if (math.abs(deltaAngle) >= math.abs(standData.remainingTurnAngle))
                {
                    deltaAngle = standData.remainingTurnAngle;
                    standData.remainingTurnAngle = 0;
                }
                else
                    standData.remainingTurnAngle -= deltaAngle;

                var transform = m_controller.GetEntityComponentData<LocalTransform>();
                if (!m_controller.ApplyRootMotion && input.Movement.x == 0 && input.Movement.y == 0 && character.OnGround == 1)
                    m_controller.SetEntityComponentData(transform.WithRotation(math.mul(quaternion.Euler(0, deltaAngle*math.PI / 180.0f, 0), rot)));
            }
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            var standData = m_controller.GetPlayableData<StandAnimationData>();

            if (m_locomotionMixer.GetInputWeight((int)LocoMixerPort.TurnL) < 0.01f)
                m_clipTurnL.SetTime(0f);
            if (m_locomotionMixer.GetInputWeight((int)LocoMixerPort.TurnR) < 0.01f)
                m_clipTurnR.SetTime(0f);
            if (standData.remainingTurnAngle == 0)
            {
                TurnTransition((int)LocoMixerPort.Idle, info.deltaTime);
            }
            else
            {
                float animTurnAngle = 90;
                var fraction = 1f - math.abs(standData.remainingTurnAngle / animTurnAngle);
                var mixerPort = (standData.remainingTurnAngle < 0) ? (int)LocoMixerPort.TurnL : (int)LocoMixerPort.TurnR;
                var anim = (standData.remainingTurnAngle < 0) ? m_clipTurnL : m_clipTurnR;

                TurnTransition(mixerPort, info.deltaTime);
                anim.SetTime(anim.GetAnimationClip().length * fraction);

                // Reset the time of the idle, so it's reset when we transition back
                if (m_locomotionMixer.GetInputWeight((int)LocoMixerPort.Idle) < 0.01f)
                    m_clipIdle.SetTime(0f);
            }

            // Update aim
            float aimPitchFraction = standData.aimPitch / 180.0f;

            m_clipAimL.SetTime(aimPitchFraction * m_clipAimL.GetDuration());
            m_clipAimMid.SetTime(aimPitchFraction * m_clipAimMid.GetDuration());
            m_clipAimR.SetTime(aimPitchFraction * m_clipAimR.GetDuration());

            float aimYawAngle = 90.0f;
            float aimYawLocal = standData.aimYaw;
            float aimYawFraction = Mathf.Abs(aimYawLocal / aimYawAngle);

            m_aimMixer.SetInputWeight((int)AimMixerPort.AimMid, 1.0f - aimYawFraction);
            if (aimYawLocal < 0)
            {
                m_aimMixer.SetInputWeight((int)AimMixerPort.AimLeft, aimYawFraction);
                m_aimMixer.SetInputWeight((int)AimMixerPort.AimRight, 0.0f);
            }
            else
            {
                m_aimMixer.SetInputWeight((int)AimMixerPort.AimLeft, 0.0f);
                m_aimMixer.SetInputWeight((int)AimMixerPort.AimRight, aimYawFraction);
            }
            base.PrepareFrame(playable, info);
        }
        void TurnTransition(int active, float deltaTime)
        {
            float turnTransitionSpeed = 7.5f;
            float currentWeight = m_locomotionMixer.GetInputWeight(active);
            if (currentWeight == 1)
                return;
            currentWeight += turnTransitionSpeed * deltaTime;
            float remainingWeight = 1-currentWeight;
            if (currentWeight >= 1)
            {
                currentWeight = 1;
                remainingWeight = 0;
            }
            float weightSum = 0;
            for (int i = 0; i < (int)LocoMixerPort.Count; ++i)
            {
                if (i != active)
                    weightSum += m_locomotionMixer.GetInputWeight(i);
            }
            float scale = remainingWeight / weightSum;
            for (int i = 0; i < (int)LocoMixerPort.Count; ++i)
            {
                if (i != active)
                    m_locomotionMixer.SetInputWeight(i, scale * m_locomotionMixer.GetInputWeight(i));
            }
            m_locomotionMixer.SetInputWeight(active, currentWeight);
        }
        public void Initialize(GhostAnimationController controller, PlayableGraph graph, Playable owner,
            AnimationClip idleClip, AnimationClip turnLClip, AnimationClip turnRClip,
            AnimationClip aimLClip, AnimationClip aimMidClip, AnimationClip aimRClip)
        {
            m_controller = controller;
            m_locomotionMixer = AnimationMixerPlayable.Create(graph, (int)LocoMixerPort.Count);

            m_clipIdle = AnimationClipPlayable.Create(graph, idleClip);
            //m_clipIdle.SetApplyFootIK(true);
            graph.Connect(m_clipIdle, 0, m_locomotionMixer, (int)LocoMixerPort.Idle);
            m_locomotionMixer.SetInputWeight((int)LocoMixerPort.Idle, 1.0f);

            m_clipTurnL = CreateTurnAnim(graph, turnLClip, (int)LocoMixerPort.TurnL);
            m_clipTurnR = CreateTurnAnim(graph, turnRClip, (int)LocoMixerPort.TurnR);

            // Aim and Aim mixer
            m_aimMixer = AnimationMixerPlayable.Create(graph, (int)AimMixerPort.Count);

            m_clipAimL = CreateAimAnim(graph, aimLClip, (int)AimMixerPort.AimLeft);
            m_clipAimMid = CreateAimAnim(graph, aimMidClip, (int)AimMixerPort.AimMid);
            m_clipAimR = CreateAimAnim(graph, aimRClip, (int)AimMixerPort.AimRight);

            // Setup other additive mixer
            m_additiveMixer = AnimationLayerMixerPlayable.Create(graph);
            var locoMixerPort = m_additiveMixer.AddInput(m_locomotionMixer, 0);
            m_additiveMixer.SetInputWeight(locoMixerPort, 1);

            var aimMixerPort = m_additiveMixer.AddInput(m_aimMixer, 0);
            m_additiveMixer.SetInputWeight(aimMixerPort, 1);
            m_additiveMixer.SetLayerAdditive((uint)aimMixerPort, true);

            owner.SetInputCount(1);
            graph.Connect(m_additiveMixer, 0, owner, 0);
            owner.SetInputWeight(0, 1);
        }
        AnimationClipPlayable CreateAimAnim(PlayableGraph graph, AnimationClip clip, int mixerPort)
        {
            AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);
            //playable.SetApplyFootIK(false);
            //playable.Pause();
            playable.SetDuration(clip.length);
            graph.Connect(playable, 0, m_aimMixer, (int)mixerPort);
            return playable;
        }
        AnimationClipPlayable CreateTurnAnim(PlayableGraph graph, AnimationClip clip, int mixerPort)
        {
            AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);
            //playable.SetApplyFootIK(true);
            //playable.Pause();
            playable.SetDuration(clip.length);
            graph.Connect(playable, 0, m_locomotionMixer, mixerPort);
            m_locomotionMixer.SetInputWeight(mixerPort, 0.0f);
            return playable;
        }
    }

    [CreateAssetMenu(fileName = "Stand", menuName = "NetCode/Animation/Stand")]
    public class GhostAnimationGraph_Stand : GhostAnimationGraphAsset
    {
        public AnimationClip IdleClip;
        public AnimationClip TurnLeftClip;
        public AnimationClip TurnRightClip;
        public AnimationClip AimLeftClip;
        public AnimationClip AimMidClip;
        public AnimationClip AimRightClip;
        public override Playable CreatePlayable(GhostAnimationController controller, PlayableGraph graph, List<GhostPlayableBehaviour> behaviours)
        {
            var behaviourPlayable = ScriptPlayable<StandGhostPlayableBehaviour>.Create(graph);
            var behaviour = behaviourPlayable.GetBehaviour();
            // This registers the behaviour for receiving PreparePredictedData, skip this if predicted data is updated by a system (PrepareFrame is still called)
            behaviours.Add(behaviour);

            behaviour.Initialize(controller, graph, behaviourPlayable, IdleClip, TurnLeftClip, TurnRightClip,
                AimLeftClip, AimMidClip, AimRightClip);
            return behaviourPlayable;
        }
        public override void RegisterPlayableData(IRegisterPlayableData register)
        {
            register.RegisterPlayableData<StandAnimationData>();
        }
    }
#endif
}
