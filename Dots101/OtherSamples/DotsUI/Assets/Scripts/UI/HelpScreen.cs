using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.DotsUISample
{
    // displays the game instructions and tips
    public class HelpScreen : UIScreen
    {
        private Button closeButton;
        private Button backButton;

        public struct CloseClickEvent : IComponentData
        {
        }

        public static HelpScreen Instantiate(VisualElement parentElement)
        {
            var screen = ScriptableObject.CreateInstance<HelpScreen>();
            screen.RootElement = parentElement;
            
            screen.closeButton = screen.RootElement.Q<Button>("help__close-button");
            screen.backButton = screen.RootElement.Q<Button>("help__back-button");

            screen.closeButton.clicked += screen.OnClickClose;
            screen.backButton.clicked += screen.OnClickClose;
            
            screen.RootElement.style.display = DisplayStyle.None;
            
            return screen;
        }

        public void OnClickClose()
        {
            var entity = entityCommandBuffer.CreateEntity();
            entityCommandBuffer.AddComponent<HelpScreen.CloseClickEvent>(entity);
            entityCommandBuffer.AddComponent<Event>(entity);
        }
    }
}