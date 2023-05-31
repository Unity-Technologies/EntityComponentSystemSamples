// in order to circumvent API breakages that do not affect physics, some packages are removed from the project on CI
// any code referencing APIs in com.unity.inputsystem must be guarded behind UNITY_INPUT_SYSTEM_EXISTS
using CharacterController;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Common.Scripts
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    partial class DemoInputGatheringSystem : SystemBase
#if UNITY_INPUT_SYSTEM_EXISTS
        ,
        InputActions.ICharacterControllerActions,
        InputActions.IVehicleActions
#endif
    {
        EntityQuery m_CharacterControllerInputQuery;
        EntityQuery m_CharacterGunInputQuery;
        EntityQuery m_VehicleInputQuery;

#pragma warning disable 649
        Vector2 m_CharacterMovement;
        Vector2 m_CharacterLooking;
        float m_CharacterFiring;
        bool m_CharacterJumped;

        Vector2 m_VehicleLooking;
        Vector2 m_VehicleSteering;
        float m_VehicleThrottle;
        int m_VehicleChanged;
#pragma warning restore 649

        protected override void OnCreate()
        {
#if UNITY_INPUT_SYSTEM_EXISTS
            m_InputActions = new InputActions();
            m_InputActions.CharacterController.SetCallbacks(this);
            m_InputActions.Vehicle.SetCallbacks(this);
#endif

            m_CharacterControllerInputQuery = GetEntityQuery(typeof(CharacterController.Input));
            m_CharacterGunInputQuery = GetEntityQuery(typeof(CharacterGunInput));
            m_VehicleInputQuery = GetEntityQuery(typeof(RaycastCar.VehicleInput));
        }

#if UNITY_INPUT_SYSTEM_EXISTS
        InputActions m_InputActions;

        protected override void OnStartRunning() => m_InputActions.Enable();

        protected override void OnStopRunning() => m_InputActions.Disable();

        void InputActions.ICharacterControllerActions.OnMove(InputAction.CallbackContext context) => m_CharacterMovement = context.ReadValue<Vector2>();
        void InputActions.ICharacterControllerActions.OnLook(InputAction.CallbackContext context) => m_CharacterLooking = context.ReadValue<Vector2>();
        void InputActions.ICharacterControllerActions.OnFire(InputAction.CallbackContext context) => m_CharacterFiring = context.ReadValue<float>();
        void InputActions.ICharacterControllerActions.OnJump(InputAction.CallbackContext context) { if (context.started) m_CharacterJumped = true; }

        void InputActions.IVehicleActions.OnLook(InputAction.CallbackContext context) => m_VehicleLooking = context.ReadValue<Vector2>();
        void InputActions.IVehicleActions.OnSteering(InputAction.CallbackContext context) => m_VehicleSteering = context.ReadValue<Vector2>();
        void InputActions.IVehicleActions.OnThrottle(InputAction.CallbackContext context) => m_VehicleThrottle = context.ReadValue<float>();
        void InputActions.IVehicleActions.OnPrevious(InputAction.CallbackContext context) { if (context.started) m_VehicleChanged = -1; }
        void InputActions.IVehicleActions.OnNext(InputAction.CallbackContext context) { if (context.started) m_VehicleChanged = 1; }
#endif

        protected override void OnUpdate()
        {
            // character controller
            if (m_CharacterControllerInputQuery.CalculateEntityCount() == 0)
                EntityManager.CreateEntity(typeof(CharacterController.Input));

            m_CharacterControllerInputQuery.SetSingleton(new CharacterController.Input
            {
                Looking = m_CharacterLooking,
                Movement = m_CharacterMovement,
                Jumped = m_CharacterJumped ? 1 : 0
            });

            if (m_CharacterGunInputQuery.CalculateEntityCount() == 0)
                EntityManager.CreateEntity(typeof(CharacterGunInput));

            m_CharacterGunInputQuery.SetSingleton(new CharacterGunInput
            {
                Looking = m_CharacterLooking,
                Firing = m_CharacterFiring,
            });

            m_CharacterJumped = false;

            // vehicle
            if (m_VehicleInputQuery.CalculateEntityCount() == 0)
                EntityManager.CreateEntity(typeof(RaycastCar.VehicleInput));

            m_VehicleInputQuery.SetSingleton(new RaycastCar.VehicleInput
            {
                Looking = m_VehicleLooking,
                Steering = m_VehicleSteering,
                Throttle = m_VehicleThrottle,
                Change = m_VehicleChanged
            });

            m_VehicleChanged = 0;
        }
    }
}
