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
    public struct LocomotionAnimationData : IComponentData
    {
        [GhostField(Quantization=1000)] public float2 Direction;
        [GhostField(Quantization=1000)] public float Phase;
        [GhostField(Quantization=1000)] public float aimPitch;
    }
#if false
    [UpdateInGroup(typeof(GhostPredictionSystemGroup))]
    public class LocomotionAnimationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            var deltaTime = Time.DeltaTime;
            Entities.ForEach((Entity entity, ref LocomotionAnimationData locoData) =>
            {
                locoData.Direction.x = (locoData.Phase%2) - 1;
                locoData.Direction.y = 1;
                locoData.Direction = math.normalizesafe(locoData.Direction);

                // FIXME: the data passed into these is not correct yet, needs to be stored elsewhere
                var blendedClipLength = RunGhostPlayableBehaviour.CalculateWeights(m_positions, m_clipLengths, m_weights, locoData.Direction);
                locoData.Phase += deltaTime / blendedClipLength;
            }).Run();
        }
    }
#endif

#if !UNITY_DISABLE_MANAGED_COMPONENTS
    public class RunGhostPlayableBehaviour : GhostPlayableBehaviour
    {
        GhostAnimationController m_controller;
        AnimationMixerPlayable m_mixer;
        private AnimationClipPlayable[] m_clips;
        private float2[] m_positions;

        private float[] m_clipLengths;
        private float[] m_weights;

        AnimationClipPlayable m_clipAim;

        // OnPlayableCreate
        // OnPlayableDestroy

        public override void PreparePredictedData(NetworkTick serverTick, float deltaTime, bool isRollback)
        {
            ref var locoData = ref m_controller.GetPlayableDataRef<LocomotionAnimationData>();

            var input = m_controller.GetEntityComponentData<CharacterControllerPlayerInput>();
            var character = m_controller.GetEntityComponentData<Character>();


            var trans = m_controller.GetEntityComponentData<LocalTransform>();

            locoData.aimPitch = 90 + input.Pitch * 180.0f / 3.1415f;

            if ((input.Movement.x != 0 || input.Movement.y != 0) && character.OnGround == 1)
            {
                trans.Rotation = quaternion.RotateY(input.Yaw);
                m_controller.SetEntityComponentData(trans);
            }



            locoData.Direction = math.normalizesafe(input.Movement);

            var blendedClipLength = CalculateWeights(m_positions, m_clipLengths, m_weights, locoData.Direction);
            locoData.Phase += deltaTime / blendedClipLength;
        }

        public override void PrepareFrame(Playable playable, FrameData info)
        {
            // FIXME: make sure locomotionData is read-only
            var locoData = m_controller.GetPlayableData<LocomotionAnimationData>();
            var blendedClipLength = CalculateWeights(m_positions, m_clipLengths, m_weights, locoData.Direction);
            for (var i = 0; i < m_clips.Length; i++)
            {
                m_mixer.SetInputWeight(i, m_weights[i]);

                m_clips[i].SetSpeed(m_clipLengths[i] / blendedClipLength);
                m_clips[i].SetTime(locoData.Phase * m_clipLengths[i]);
            }
            // Update aim
            float aimPitchFraction = locoData.aimPitch / 180.0f;
            m_clipAim.SetTime(aimPitchFraction * m_clipAim.GetDuration());

            base.PrepareFrame(playable, info);
        }
        public void Initialize(GhostAnimationController controller, PlayableGraph graph, Playable owner, List<GhostAnimationGraph_Run.BlendSpaceNode> blendSpaceNodes, AnimationClip aimClip)
        {
            m_controller = controller;
            m_mixer = AnimationMixerPlayable.Create(graph, blendSpaceNodes.Count);

            m_clips = new AnimationClipPlayable[blendSpaceNodes.Count];
            m_positions = new float2[blendSpaceNodes.Count];
            m_clipLengths = new float[blendSpaceNodes.Count];
            m_weights = new float[blendSpaceNodes.Count];

            for (var i = 0; i < blendSpaceNodes.Count; i++)
            {
                var node = blendSpaceNodes[i];
                var clip = AnimationClipPlayable.Create(graph, node.clip);
                m_positions[i] = math.normalizesafe(node.position);
                m_clipLengths[i] = node.clip.length;
                m_mixer.ConnectInput(i, clip, 0);
                m_clips[i] = clip;
                m_mixer.SetInputWeight(i, 0);
            }

            m_clipAim = AnimationClipPlayable.Create(graph, aimClip);
            //m_clipAim.SetApplyFootIK(false);
            //m_clipAim.Pause();
            m_clipAim.SetDuration(aimClip.length);

            // Setup other additive mixer
            var additiveMixer = AnimationLayerMixerPlayable.Create(graph);
            var locoMixerPort = additiveMixer.AddInput(m_mixer, 0);
            additiveMixer.SetInputWeight(locoMixerPort, 1);

            var aimMixerPort = additiveMixer.AddInput(m_clipAim, 0);
            additiveMixer.SetInputWeight(aimMixerPort, 1);
            additiveMixer.SetLayerAdditive((uint)aimMixerPort, true);

            owner.SetInputCount(1);
            graph.Connect(additiveMixer, 0, owner, 0);
            owner.SetInputWeight(0, 1);
        }
        static float CalculateWeights(float2[] positionArray, float[] clipLengthArray, float[] weightArray, float2 blendPosition)
        {
            var count = positionArray.Length;

            // Initialize all weights to 0
            for (var i = 0; i < weightArray.Length; i++)
            {
                weightArray[i] = 0f;
            }

            // Handle fallback
            if (count < 2)
            {
                if (count == 1)
                {
                    weightArray[0] = 1;
                    return clipLengthArray[0];
                }
                return 1;
            }

            // Handle special case when sampled ecactly in the middle
            if (math.all(blendPosition == float2.zero))
            {
                // If we have a center motion, give that one all the weight
                for (var i = 0; i < count; i++)
                {
                    if (math.all(positionArray[i] == float2.zero))
                    {
                        weightArray[i] = 1;
                        return clipLengthArray[i];
                    }
                }

                // Otherwise divide weight evenly
                float sharedWeight = 1.0f / count;
                float avgCenterLen = 0;
                for (var i = 0; i < count; i++)
                {
                    weightArray[i] = sharedWeight;
                    avgCenterLen += sharedWeight * clipLengthArray[i];
                }

                return avgCenterLen;
            }

            int indexA = -1;
            int indexB = -1;
            int indexCenter = -1;
            float maxDotForNegCross = -100000.0f;
            float maxDotForPosCross = -100000.0f;
            for (var i = 0; i < count; i++)
            {
                if (math.all(positionArray[i] == float2.zero))
                {
                    if (indexCenter >= 0)
                        return 1;
                    indexCenter = i;
                    continue;
                }

                var posNormalized = positionArray[i];
                var dot = math.dot(posNormalized, blendPosition);
                var cross = posNormalized.x * blendPosition.y - posNormalized.y * blendPosition.x;
                if (cross > 0f)
                {
                    if (dot > maxDotForPosCross)
                    {
                        maxDotForPosCross = dot;
                        indexA = i;
                    }
                }
                else
                {
                    if (dot > maxDotForNegCross)
                    {
                        maxDotForNegCross = dot;
                        indexB = i;
                    }
                }
            }

            float centerWeight = 0;
            float avgLen = 0;

            if (indexA < 0 || indexB < 0)
            {
                // Fallback if sampling point is not inside a triangle
                centerWeight = 1;
            }
            else
            {
                var a = positionArray[indexA];
                var b = positionArray[indexB];

                // Calculate weights using barycentric coordinates
                // (formulas from http://en.wikipedia.org/wiki/Barycentric_coordinate_system_%28mathematics%29 )
                float det = b.y * a.x - b.x * a.y; // Simplified from: (b.y-0)*(a.x-0) + (0-b.x)*(a.y-0);

                // TODO: Is x and y used correctly below??
                float wA = (b.y * blendPosition.x - b.x * blendPosition.y) / det; // Simplified from: ((b.y-0)*(l.x-0) + (0-b.x)*(l.y-0)) / det;
                float wB = (a.x * blendPosition.y - a.y * blendPosition.x) / det; // Simplified from: ((0-a.y)*(l.x-0) + (a.x-0)*(l.y-0)) / det;
                centerWeight = 1 - wA - wB;

                // Clamp to be inside triangle
                if (centerWeight < 0)
                {
                    centerWeight = 0;
                    float sum = wA + wB;
                    wA /= sum;
                    wB /= sum;
                }
                else if (centerWeight > 1)
                {
                    centerWeight = 1;
                    wA = 0;
                    wB = 0;
                }

                // Give weight to the two vertices on the periphery that are closest
                weightArray[indexA] = wA;
                weightArray[indexB] = wB;

                avgLen += clipLengthArray[indexA] * wA + clipLengthArray[indexB] * wB;
            }

            if (indexCenter >= 0)
            {
                weightArray[indexCenter] = centerWeight;
                avgLen += clipLengthArray[indexCenter] * centerWeight;
            }
            else
            {
                // Give weight to all children when input is in the center
                float sharedWeight = 1.0f / count;
                for (var i = 0; i < count; i++)
                {
                    weightArray[i] += sharedWeight * centerWeight;
                    avgLen += sharedWeight * centerWeight;
                }
            }
            return avgLen;
        }
    }

    [CreateAssetMenu(fileName = "Run", menuName = "NetCode/Animation/Run")]
    public class GhostAnimationGraph_Run : GhostAnimationGraphAsset
    {
        [Serializable]
        public struct BlendSpaceNode
        {
            public AnimationClip clip;
            public float2 position;
        }
        public List<BlendSpaceNode> BlendSpaceNodes;
        public AnimationClip AimClip;
        public override Playable CreatePlayable(GhostAnimationController controller, PlayableGraph graph, List<GhostPlayableBehaviour> behaviours)
        {
            var behaviourPlayable = ScriptPlayable<RunGhostPlayableBehaviour>.Create(graph);
            var behaviour = behaviourPlayable.GetBehaviour();
            // This registers the behaviour for receiving PreparePredictedData, skip this if predicted data is updated by a system (PrepareFrame is still called)
            behaviours.Add(behaviour);

            behaviour.Initialize(controller, graph, behaviourPlayable, BlendSpaceNodes, AimClip);
            return behaviourPlayable;
        }
        public override void RegisterPlayableData(IRegisterPlayableData register)
        {
            register.RegisterPlayableData<LocomotionAnimationData>();
        }
    }
#endif
}
