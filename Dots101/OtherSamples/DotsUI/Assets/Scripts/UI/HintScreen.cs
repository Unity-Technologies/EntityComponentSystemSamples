using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.DotsUISample
{
    // hint text that appears next to the nearest collectable or cauldron when in range
    public class HintScreen : UIScreen
    {
        public static HintScreen Instantiate(VisualElement root)
        {
            var screen = ScriptableObject.CreateInstance<HintScreen>();
            screen.RootElement = root;
            screen.RootElement.style.display = DisplayStyle.None;
            return screen;
        }

        public void SetPosition(Vector3 position, Camera camera)
        {
            Vector3 screenPosition = camera.WorldToScreenPoint(position + Vector3.up * 1.5f);
            screenPosition = RuntimePanelUtils.ScreenToPanel(RootElement.panel,
                new Vector2(screenPosition.x, Screen.height - screenPosition.y));
            RootElement.transform.position = new Vector3(screenPosition.x, screenPosition.y, screenPosition.z);
        }

        public void SetMessage(string message)
        {
            RootElement.Q<Label>().text = message;
        }
    }
}