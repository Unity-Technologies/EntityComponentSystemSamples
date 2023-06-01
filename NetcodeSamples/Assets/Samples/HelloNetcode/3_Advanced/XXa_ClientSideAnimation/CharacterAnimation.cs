using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;
#if !UNITY_DISABLE_MANAGED_COMPONENTS
using Unity.NetCode.Hybrid;
#endif

namespace Samples.HelloNetcode
{
    public class CharacterAnimation : MonoBehaviour
    {
#if !UNITY_DISABLE_MANAGED_COMPONENTS
        public bool IgnoreEvents;

        const float k_TurnAngle = 90.0f;

        Animator m_Animator;
        float m_RemainingTurnAngle;
        float m_AirPhase;
        GhostPresentationGameObjectEntityOwner m_EntityOwner;

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

        public AnimationClip TurnAnimationClip;
        public float TurnSpeed = 250;
        public Transform RightOffhandIk;
        public Vector3 Offset = new Vector3(-90,0,-90);

        void Start()
        {
            m_Animator = GetComponent<Animator>();
            m_Animator.fireEvents = !IgnoreEvents;
            m_EntityOwner = GetComponent<GhostPresentationGameObjectEntityOwner>();
        }

        void Update()
        {
            var input = GetEntityComponentData<CharacterControllerPlayerInput>();
            var character = GetEntityComponentData<Character>();
            var onGround = character.OnGround == 1;
            var isShooting = input.PrimaryFire.IsSet;

            UpdateAim(input.Pitch);

            var state = ComputeAnimationState(onGround, isShooting, input);
            switch (state)
            {
                case CharacterAnimationState.Stand:
                    StandingAnimation(input.Yaw);
                    SetEntityComponentData(input);
                    break;
                case CharacterAnimationState.Run:
                    UpdateRotation(input.Yaw);
                    RunAnimation(input.Movement.x, input.Movement.y);
                    break;
                case CharacterAnimationState.Jump:
                    UpdateRotation(input.Yaw);
                    break;
            }
        }

        CharacterAnimationState ComputeAnimationState(bool onGround, bool isShooting, CharacterControllerPlayerInput input)
        {
            m_Animator.SetBool(Jumping, !onGround);
            if (isShooting) { m_Animator.SetTrigger(Shooting); }
            if (!onGround)
            {
                return CharacterAnimationState.Jump;
            }

            if (input.Movement.x == 0 && input.Movement.y == 0)
            {
                m_Animator.SetBool(Moving, false);
                return CharacterAnimationState.Stand;
            }

            m_Animator.SetBool(Moving, true);
            return CharacterAnimationState.Run;

        }

        void UpdateAim(float pitch)
        {
            var aimPitch = 90 + pitch * 180.0f / 3.1415f;
            var aimPitchFraction = aimPitch / 180.0f;
            m_Animator.SetFloat(AimPitch, aimPitchFraction);
        }

        void UpdateRotation(float yaw)
        {
            var rot = quaternion.RotateY(yaw);
#if !ENABLE_TRANSFORM_V1
            SetEntityComponentData(LocalTransform.FromPositionRotation(transform.position, rot));
#else
            SetEntityComponentData(new Rotation { Value = rot });
#endif
        }

        void RunAnimation(float horizontal, float vertical)
        {
            var moveInput = new Vector3(horizontal, 0, vertical);
            var normalized = moveInput.normalized;
            var normalInput2D = new Vector2(normalized.x, normalized.z);

            m_Animator.SetFloat(VelX, normalInput2D.x);
            m_Animator.SetFloat(VelY, normalInput2D.y);
        }

        void StandingAnimation(float yawRadians)
        {
            var yaw = math.degrees(yawRadians);
            var eulerAnglesY = transform.rotation.eulerAngles.y;
            var aimDelta = Mathf.DeltaAngle(eulerAnglesY, yaw);

            if (m_RemainingTurnAngle == 0 && math.abs(aimDelta) > k_TurnAngle)
            {
                m_RemainingTurnAngle = k_TurnAngle * math.sign(aimDelta);
            }

            UpdateTurn();

            float aimYawFraction = aimDelta / k_TurnAngle;
            m_Animator.SetFloat(AimYaw, aimYawFraction);
        }

        void UpdateTurn()
        {
            var sign = math.sign(m_RemainingTurnAngle);
            if (UpdateTurnAnimation(sign)) { return; }

            UpdateTurnTransform(sign);
        }

        void UpdateTurnTransform(float sign)
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

#if !ENABLE_TRANSFORM_V1
            var localTransform = GetEntityComponentData<LocalTransform>();
            var x = math.mul(quaternion.RotateY(math.radians(absRotationThisFrame * sign)), localTransform.Rotation);
            SetEntityComponentData(localTransform.WithRotation(x));
#else
            var rotation = GetEntityComponentData<Rotation>().Value;
            rotation *= Quaternion.Euler(0, math.radians(absRotationThisFrame * sign), 0);
            SetEntityComponentData(new Rotation { Value = rotation });
#endif
        }

        bool UpdateTurnAnimation(float sign)
        {
            if (m_RemainingTurnAngle == 0)
            {
                m_Animator.SetFloat(TurnDirection, 0);
                return true;
            }

            var fraction = 1f - math.abs(m_RemainingTurnAngle / (k_TurnAngle * sign));
            m_Animator.SetFloat(TurnTime, fraction * TurnAnimationClip.length);
            m_Animator.SetFloat(TurnDirection, sign);
            return false;
        }

        void OnAnimatorIK(int layerIndex)
        {
            if (m_Animator == null) { return; }
            // Avatar point left hand to IK left
            m_Animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 100);
            m_Animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 100);
            m_Animator.SetIKPosition(AvatarIKGoal.LeftHand, RightOffhandIk.position);
            m_Animator.SetIKRotation(AvatarIKGoal.LeftHand, RightOffhandIk.rotation * Quaternion.Euler(Offset));
        }

        void SetEntityComponentData<T>(T data) where T : unmanaged, IComponentData
        {
            m_EntityOwner.World.EntityManager.SetComponentData(m_EntityOwner.Entity, data);
        }

        T GetEntityComponentData<T>() where T : unmanaged, IComponentData
        {
            return m_EntityOwner.World.EntityManager.GetComponentData<T>(m_EntityOwner.Entity);
        }
#endif
    }
}
