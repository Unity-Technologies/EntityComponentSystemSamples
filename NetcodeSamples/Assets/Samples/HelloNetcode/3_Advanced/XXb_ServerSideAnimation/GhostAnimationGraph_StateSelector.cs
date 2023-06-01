using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.NetCode;
#if !UNITY_DISABLE_MANAGED_COMPONENTS
using Unity.NetCode.Hybrid;
#endif

namespace Samples.HelloNetcode.Hybrid
{
    public struct StateAnimationData : IComponentData
    {
        [GhostField] public int Value;
    }
#if !UNITY_DISABLE_MANAGED_COMPONENTS
    public class StateSelectorGhostPlayableBehaviour : GhostPlayableBehaviour
    {
        GhostAnimationController m_controller;
        AnimationMixerPlayable m_mixer;
        float[] m_transitions;

        public override void PreparePredictedData(NetworkTick serverTick, float deltaTime, bool isRollback)
        {
            ref var stateData = ref m_controller.GetPlayableDataRef<StateAnimationData>();

            var input = m_controller.GetEntityComponentData<CharacterControllerPlayerInput>();
            var character = m_controller.GetEntityComponentData<Character>();

            if (character.OnGround != 0)
                stateData.Value = (input.Movement.x != 0 || input.Movement.y != 0) ? 1 : 0;
            else
            {
                float jumpTime = -1;
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
                        if (jumpTime > JumpGhostPlayableBehaviour.k_JumpDuration)
                            jumpTime = -1;
                    }
                }
                stateData.Value = jumpTime < 0 ? 3 : 2;
            }
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            var stateData = m_controller.GetPlayableData<StateAnimationData>();

            StateTransition(stateData.Value, info.deltaTime);
            base.PrepareFrame(playable, info);
        }
        void StateTransition(int active, float deltaTime)
        {
            float currentWeight = m_mixer.GetInputWeight(active);
            if (currentWeight == 1)
                return;
            float transitionTime = m_transitions[active];
            currentWeight += deltaTime / transitionTime;
            float remainingWeight = 1-currentWeight;
            if (currentWeight >= 1)
            {
                currentWeight = 1;
                remainingWeight = 0;
            }
            float weightSum = 0;
            for (int i = 0; i < m_transitions.Length; ++i)
            {
                if (i != active)
                    weightSum += m_mixer.GetInputWeight(i);
            }
            float scale = remainingWeight / weightSum;
            for (int i = 0; i < m_transitions.Length; ++i)
            {
                if (i != active)
                    m_mixer.SetInputWeight(i, scale * m_mixer.GetInputWeight(i));
            }
            m_mixer.SetInputWeight(active, currentWeight);
        }
        public void Initialize(GhostAnimationController controller, PlayableGraph graph, List<GhostPlayableBehaviour> behaviours, Playable owner,
            GhostAnimationGraph_StateSelector.ControllerDefinition[] controllers)
        {
            m_controller = controller;
            m_mixer = AnimationMixerPlayable.Create(graph, controllers.Length);
            m_transitions = new float[controllers.Length];
            for (int i = 0; i < controllers.Length; ++i)
            {
                var playable = controllers[i].template.CreatePlayable(controller, graph, behaviours);
                graph.Connect(playable, 0, m_mixer, i);
                m_mixer.SetInputWeight(i, 0);
                m_transitions[i] = controllers[i].transitionTime;
            }
            m_mixer.SetInputWeight(0, 1);

            owner.SetInputCount(1);
            graph.Connect(m_mixer, 0, owner, 0);
            owner.SetInputWeight(0, 1);
        }
    }

    [CreateAssetMenu(fileName = "StateSelector", menuName = "NetCode/Animation/StateSelector")]
    public class GhostAnimationGraph_StateSelector : GhostAnimationGraphAsset
    {
        public enum CharacterAnimationState
        {
            Stand,
            Run,
            Jump,
            InAir,
            NumStates
        }
        [Serializable]
        public struct TransitionDefinition
        {
            public CharacterAnimationState sourceState;
            public float transtionTime;
        }

        [Serializable]
        public struct ControllerDefinition
        {
            public CharacterAnimationState animationState;

            public GhostAnimationGraphAsset template;
            [Tooltip("Default transition time from any other state (unless overwritten)")]
            public float transitionTime;
            [Tooltip("Custom transition times from specific states")]
            public TransitionDefinition[] customTransitions;
        }
        public ControllerDefinition[] controllers;
        public override Playable CreatePlayable(GhostAnimationController controller, PlayableGraph graph, List<GhostPlayableBehaviour> behaviours)
        {
            var behaviourPlayable = ScriptPlayable<StateSelectorGhostPlayableBehaviour>.Create(graph);
            var behaviour = behaviourPlayable.GetBehaviour();
            // This registers the behaviour for receiving PreparePredictedData, skip this if predicted data is updated by a system (PrepareFrame is still called)
            behaviours.Add(behaviour);

            behaviour.Initialize(controller, graph, behaviours, behaviourPlayable, controllers);
            return behaviourPlayable;
        }
        public override void RegisterPlayableData(IRegisterPlayableData register)
        {
            register.RegisterPlayableData<StateAnimationData>();
            for (int i = 0; i < controllers.Length; ++i)
                controllers[i].template.RegisterPlayableData(register);
        }
    }
#endif
}
