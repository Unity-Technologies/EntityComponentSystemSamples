using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.DotsUISample
{
    public class InventoryScreen : UIScreen
    {
        private Label energyLabel;
        private Button backButton;
        private VisualElement slotsContainer;

        private const int k_InitialSlotCount = 16;
        private InventorySlot[] slots = new InventorySlot[k_InitialSlotCount];

        private const string k_SlotUssClassName = "inventory-slot";

        public struct BackClickedEvent : IComponentData {}

        public static InventoryScreen Instantiate(VisualElement parentElement)
        {
            var screen = ScriptableObject.CreateInstance<InventoryScreen>();
            screen.RootElement = parentElement;    
            
            screen.energyLabel = screen.RootElement.Q<Label>("inventory__energy-label");
            screen.backButton = screen.RootElement.Q<Button>("inventory__back-button");
            screen.slotsContainer = screen.RootElement.Q<VisualElement>("inventory__slots-container");

            screen.InitializeSlots();

            screen.backButton.clicked += screen.OnBackClicked;
                
            screen.RootElement.style.display = DisplayStyle.None;
            return screen;
        }

        public void UpdateInventory(DynamicBuffer<InventoryItem> itemsBuf, int energyCount, CollectablesData collectablesData)
        {
            for (int i = 0; i < itemsBuf.Length && i < slots.Length; i++)
            {
                var type = itemsBuf[i].Type;
                var sprite = collectablesData.Collectables[(int)type].Icon;
                slots[i].SetItem(sprite);
            }
            energyLabel.text = energyCount.ToString();
        }

        public void OnBackClicked()
        {
            var entity = entityCommandBuffer.CreateEntity();
            entityCommandBuffer.AddComponent<InventoryScreen.BackClickedEvent>(entity);
            entityCommandBuffer.AddComponent<Event>(entity);
        }

        private void InitializeSlots()
        {
            for (int i = 0; i < k_InitialSlotCount; i++)
            {
                var slot = new InventorySlot();
                slot.AddToClassList(k_SlotUssClassName);
                slotsContainer.Add(slot);
                slots[i] = slot;
            }
        }
    }
}