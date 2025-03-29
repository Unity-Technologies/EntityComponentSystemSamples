using UnityEngine.InputSystem;

namespace Unity.DotsUISample
{
    public static class GameInput
    {
        public static InputAction Move;
        public static InputAction Interact;
        public static InputAction ShowInventory;
        public static InputAction Cancel;

        public static void Initialize()
        {
            var actionsMap = InputSystem.actions.FindActionMap("Player");
            Move = actionsMap.FindAction("Move");
            Interact = actionsMap.FindAction("Interact");
            ShowInventory = actionsMap.FindAction("ShowInventory");
            Cancel = actionsMap.FindAction("Cancel");
        }
    }
}