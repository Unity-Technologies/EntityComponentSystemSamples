using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.NetCode.Samples.Common;
using UnityEngine;

namespace Samples.HelloNetcode
{
    [GhostComponent(PrefabType=GhostPrefabType.AllPredicted, OwnerSendType = SendToOwnerType.SendToNonOwner)]
    public struct CharacterControllerPlayerInput : IInputComponentData
    {
        [GhostField] public float2 Movement;
        [GhostField] public InputEvent Jump;
        [GhostField] public InputEvent PrimaryFire;
        [GhostField] public InputEvent SecondaryFire;
        [GhostField] public float Pitch;
        [GhostField] public float Yaw;

        /// <summary>Implemented to get better packet dump info.</summary>
        public FixedString512Bytes ToFixedString() => $"move({Movement}, j:{Jump.Count}), shoot(p:{PrimaryFire.Count}, s{SecondaryFire.Count}), mouse(pitch:{Pitch}, yaw:{Yaw})";
    }

    [UpdateInGroup(typeof(HelloNetcodeInputSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct SampleCharacterControllerPlayerInputSystem : ISystem
    {
        bool m_WasJumpTouch;
        bool m_WasFireTouch;
        bool m_WasSecondaryFireTouch;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CharacterControllerPlayerInput>();
            state.RequireForUpdate<NetworkStreamInGame>();
        }
        public void OnUpdate(ref SystemState state)
        {
            foreach (var input in SystemAPI.Query<RefRW<CharacterControllerPlayerInput>>().WithAll<GhostOwnerIsLocal>())
            {
                input.ValueRW.Movement = default;
                input.ValueRW.Jump = default;
                input.ValueRW.PrimaryFire = default;
                input.ValueRW.SecondaryFire = default;
                if (TouchInput.GetKey(TouchInput.KeyCode.LeftStick))
                    input.ValueRW.Movement = TouchInput.GetStick(TouchInput.StickCode.LeftStick);
                else
                {
                    if (Input.GetKey("left") || Input.GetKey("a"))
                        input.ValueRW.Movement.x -= 1;
                    if (Input.GetKey("right") || Input.GetKey("d"))
                        input.ValueRW.Movement.x += 1;
                    if (Input.GetKey("down") || Input.GetKey("s"))
                        input.ValueRW.Movement.y -= 1;
                    if (Input.GetKey("up") || Input.GetKey("w"))
                        input.ValueRW.Movement.y += 1;
                }
                var jumpTouch = TouchInput.GetKey(TouchInput.KeyCode.Space);
                if (Input.GetKeyDown("space") || (jumpTouch && !m_WasJumpTouch))
                    input.ValueRW.Jump.Set();
                m_WasJumpTouch = jumpTouch;

                float2 lookDelta = float2.zero;
                if (TouchInput.GetKey(TouchInput.KeyCode.RightStick))
                {
                    lookDelta = TouchInput.GetStick(TouchInput.StickCode.RightStick) * SystemAPI.Time.DeltaTime;
                }
#if !UNITY_IOS && !UNITY_ANDROID
                else
                {
                    lookDelta = new float2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
                    // You'll want to expose userSpecifiedMouseSensitivity in your games UI.
                    // The server doesn't need to know about it.
                    // Example valid range: 0.002 - 0.4.
                    const float userSpecifiedMouseSensitivity = .04f;
                    lookDelta *= userSpecifiedMouseSensitivity;
                }
#endif
                input.ValueRW.Pitch = math.clamp(input.ValueRW.Pitch+lookDelta.y, -math.PI/2, math.PI/2);
                input.ValueRW.Yaw = math.fmod(input.ValueRW.Yaw + lookDelta.x, 2*math.PI);

                var fireTouch = TouchInput.GetKey(TouchInput.KeyCode.Left);
                var secondaryFireTouch = TouchInput.GetKey(TouchInput.KeyCode.Right);
                if ((fireTouch && !m_WasFireTouch)
                #if !UNITY_IOS && !UNITY_ANDROID
                    || Input.GetKeyDown(KeyCode.Mouse0)
                #endif
                    )
                {
                    input.ValueRW.PrimaryFire.Set();
                }
                if ((secondaryFireTouch && !m_WasSecondaryFireTouch)
                #if !UNITY_IOS && !UNITY_ANDROID
                    || Input.GetKeyDown(KeyCode.Mouse1)
                #endif
                    )
                {
                    input.ValueRW.SecondaryFire.Set();
                }
                m_WasFireTouch = fireTouch;
                m_WasSecondaryFireTouch = secondaryFireTouch;
            }
        }
    }
}
