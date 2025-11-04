using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.DotsUISample
{
    // displays the game instructions and tips
    public class HelpScreen : UIScreen
    {
        Button m_CloseButton;
        Button m_BackButton;

        public struct CloseClickEvent : IComponentData { }

        public static HelpScreen Instantiate(VisualElement parentElement)
        {
            var screen = ScriptableObject.CreateInstance<HelpScreen>();
            screen.RootElement = parentElement;

            screen.m_CloseButton = screen.RootElement.Q<Button>("help__close-button");
            screen.m_BackButton = screen.RootElement.Q<Button>("help__back-button");

            screen.m_CloseButton.clicked += screen.OnClickClose;
            screen.m_BackButton.clicked += screen.OnClickClose;

            screen.RootElement.style.display = DisplayStyle.None;

            return screen;
        }

        public void OnClickClose()
        {
            var entity = entityCommandBuffer.CreateEntity();
            entityCommandBuffer.AddComponent<CloseClickEvent>(entity);
            entityCommandBuffer.AddComponent<Event>(entity);
        }
    }
}
