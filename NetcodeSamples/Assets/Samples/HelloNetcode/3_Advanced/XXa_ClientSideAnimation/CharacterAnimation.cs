#if !UNITY_DISABLE_MANAGED_COMPONENTS
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Samples.HelloNetcode
{
    /// <summary>
    /// Describe the current state of the character to be played by the animation system.
    /// State such as direction of movement, aim direction etc.
    /// The system <see cref="UpdateAnimationStateSystem"/> will invoke <see cref="CharacterAnimation.UpdateAnimationState"/>
    /// with information gathered from the player entity.
    /// </summary>
    public struct CharacterAnimationData
    {
        public bool OnGround;
        public bool IsShooting;
        public float Pitch;
        public float Yaw;
        public float2 Movement;
    }

    /// <summary>
    /// Update animator based on the <see cref="CharacterAnimationData"/> sent from the <see cref="UpdateAnimationState"/> system.
    /// It is expected that an Animator with related controller is attached to the same game object.
    /// </summary>
    public class CharacterAnimation : MonoBehaviour
    {
        public bool IgnoreEvents;

        const float k_TurnAngle = 90.0f;

        Animator m_Animator;
        float m_RemainingTurnAngle;
        float m_AirPhase;

        enum CharacterAnimationState
        {
            Stand,
            Run,
            Jump,
        }

        static readonly int VelX = Animator.StringToHash("VelX");
        static readonly int VelY = Animator.StringToHash("VelY");
        static readonly int Moving = Animator.StringToHash("Moving");
        static readonly int TurnDirection = Animator.StringToHash("TurnDirection");
        static readonly int TurnTime = Animator.StringToHash("TurnTime");
        static readonly int AimPitch = Animator.StringToHash("AimPitch");
        static readonly int AimYaw = Animator.StringToHash("AimYaw");
        static readonly int Jumping = Animator.StringToHash("Jumping");
        static readonly int Shooting = Animator.StringToHash("Shooting");

        /// <summary>
        /// Used to control the rotation of the character based on the length of the animation clip
        /// </summary>
        public AnimationClip TurnAnimationClip;
        public Transform RightOffhandIk;
        public Vector3 Offset = new Vector3(-90,0,-90);

        void Start()
        {
            m_Animator = GetComponent<Animator>();
            m_Animator.fireEvents = !IgnoreEvents;
        }

        public LocalTransform UpdateAnimationState(CharacterAnimationData data, LocalTransform localTransform)
        {
            if (m_Animator == null)
            {
                return localTransform;
            }

            UpdateAim(data.Pitch);

            var state = ComputeAnimationState(data.OnGround, data.IsShooting, data.Movement);
            switch (state)
            {
                case CharacterAnimationState.Stand:
                    return StandingAnimation(data.Yaw, localTransform);
                case CharacterAnimationState.Run:
                    RunAnimation(data.Movement.x, data.Movement.y);
                    return UpdateRotation(data.Yaw, localTransform);
                case CharacterAnimationState.Jump:
                    return UpdateRotation(data.Yaw, localTransform);
            }

            return localTransform;
        }

        /// <summary>
        /// Returns whether two <see cref="float2"/>s are equal within <see cref="float.Epsilon"/>
        /// </summary>
        static bool NearlyEqual(float2 a, float2 b)
        {
            return math.abs(a.x - b.x) <= float.Epsilon && math.abs(a.y - b.y) <= float.Epsilon;
        }

        /// <summary>
        /// Return the <see cref="CharacterAnimationState"/> and set the animator state accordingly.
        /// E.g. If the input system says that the character is not on the ground,
        /// the animation state should be Jumping.
        /// </summary>
        CharacterAnimationState ComputeAnimationState(bool onGround, bool isShooting, float2 movement)
        {
            m_Animator.SetBool(Jumping, !onGround);
            if (isShooting) { m_Animator.SetTrigger(Shooting); }
            if (!onGround)
            {
                return CharacterAnimationState.Jump;
            }

            if (NearlyEqual(movement, float2.zero))
            {
                m_Animator.SetBool(Moving, false);
                return CharacterAnimationState.Stand;
            }

            m_Animator.SetBool(Moving, true);
            return CharacterAnimationState.Run;

        }

        /// <summary>
        /// Updates the pitch of the character.
        /// This is limited by 180 degrees in total. 90 degrees down and up.
        /// </summary>
        void UpdateAim(float pitch)
        {
            var aimPitch = 90 + pitch * 180.0f / 3.1415f;
            var aimPitchFraction = aimPitch / 180.0f;
            m_Animator.SetFloat(AimPitch, aimPitchFraction);
        }

        /// <summary>
        /// Returns <paramref name="transform"/> with the rotation set to <paramref name="yawRadians"/>.
        /// </summary>
        static LocalTransform UpdateRotation(float yawRadians, LocalTransform transform)
        {
            var rot = quaternion.RotateY(yawRadians);
            return LocalTransform.FromPositionRotation(transform.Position, rot);
        }

        /// <summary>
        /// Updates two animation floats to be used by the blend tree
        /// in the animator state machine to determine run direction.
        ///
        /// The values will be normalized between 0 and 1
        /// </summary>
        void RunAnimation(float horizontal, float vertical)
        {
            var moveInput = new Vector3(horizontal, 0, vertical);
            var normalized = moveInput.normalized;
            var normalInput2D = new Vector2(normalized.x, normalized.z);

            m_Animator.SetFloat(VelX, normalInput2D.x);
            m_Animator.SetFloat(VelY, normalInput2D.y);
        }

        /// <summary>
        /// When standing still the character will turn once <paramref name="yawRadians"/> converted to degrees
        /// surpass the <see cref="k_TurnAngle"/> constant.
        /// This turn will be updated every frame using <see cref="Time.deltaTime"/>.
        /// </summary>
        LocalTransform StandingAnimation(float yawRadians, LocalTransform localTransform)
        {
            var yaw = math.degrees(yawRadians);
            var eulerAnglesY = ((Quaternion)localTransform.Rotation).eulerAngles.y;
            var aimDelta = Mathf.DeltaAngle(eulerAnglesY, yaw);

            if (m_RemainingTurnAngle == 0 && math.abs(aimDelta) > k_TurnAngle)
            {
                m_RemainingTurnAngle = k_TurnAngle * math.sign(aimDelta);
            }

            var aimYawFraction = aimDelta / k_TurnAngle;
            m_Animator.SetFloat(AimYaw, aimYawFraction);

            return UpdateTurn(localTransform);
        }

        LocalTransform UpdateTurn(LocalTransform localTransform)
        {
            var sign = math.sign(m_RemainingTurnAngle);
            return UpdateTurnAnimation(sign)
                ? localTransform
                : UpdateTurnTransform(sign, localTransform);
        }

        LocalTransform UpdateTurnTransform(float sign, LocalTransform localTransform)
        {
            var absRotationThisFrame = Time.deltaTime * k_TurnAngle / TurnAnimationClip.length;
            if (absRotationThisFrame >= math.abs(m_RemainingTurnAngle))
            {
                absRotationThisFrame = math.abs(m_RemainingTurnAngle);
                m_RemainingTurnAngle = 0;
            }
            else
            {
                m_RemainingTurnAngle -= absRotationThisFrame * sign;
            }

            var x = math.mul(quaternion.RotateY(math.radians(absRotationThisFrame * sign)), localTransform.Rotation);
            return localTransform.WithRotation(x);
        }

        bool UpdateTurnAnimation(float sign)
        {
            if (m_RemainingTurnAngle == 0)
            {
                m_Animator.SetFloat(TurnDirection, 0);
                return true;
            }

            var fraction = 1f - math.abs(m_RemainingTurnAngle / (k_TurnAngle * sign));
            m_Animator.SetFloat(TurnTime, fraction);
            m_Animator.SetFloat(TurnDirection, sign);
            return false;
        }

        /// <summary>
        /// This will attach the left hand to the gun using the <see cref="RightOffhandIk"/> point.
        /// </summary>
        void OnAnimatorIK(int layerIndex)
        {
            if (m_Animator == null) { return; }
            // Avatar point left hand to IK left
            m_Animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 100);
            m_Animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 100);
            m_Animator.SetIKPosition(AvatarIKGoal.LeftHand, RightOffhandIk.position);
            m_Animator.SetIKRotation(AvatarIKGoal.LeftHand, RightOffhandIk.rotation * Quaternion.Euler(Offset));
        }
    }
}
#endif
