using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.DotsUISample
{
    // displays buttons for opening the inventory and help
    public class HUDScreen : UIScreen
    {
        Button m_HelpButton;
        Button m_InventoryButton;

        public struct InventoryClickEvent : IComponentData { }

        public struct HelpClickEvent : IComponentData { }

        public static HUDScreen Instantiate(VisualElement parentElement)
        {
            var screen = ScriptableObject.CreateInstance<HUDScreen>();
            screen.RootElement = parentElement;

            screen.m_HelpButton = screen.RootElement.Q<Button>("hud__help-button");
            screen.m_InventoryButton = screen.RootElement.Q<Button>("hud__inventory-button");

            screen.m_HelpButton.clicked += screen.OnClickHelp;
            screen.m_InventoryButton.clicked += screen.OnClickInventory;

            screen.RootElement.style.display = DisplayStyle.None;
            return screen;
        }

        public void OnClickInventory()
        {
            var entity = entityCommandBuffer.CreateEntity();
            entityCommandBuffer.AddComponent<HUDScreen.InventoryClickEvent>(entity);
            entityCommandBuffer.AddComponent<Event>(entity);
        }

        public void OnClickHelp()
        {
            var entity = entityCommandBuffer.CreateEntity();
            entityCommandBuffer.AddComponent<HUDScreen.HelpClickEvent>(entity);
            entityCommandBuffer.AddComponent<Event>(entity);
        }
    }
}
