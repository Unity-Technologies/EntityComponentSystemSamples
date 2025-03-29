using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.DotsUISample
{
    public class InventorySlot : VisualElement
    {
        public Image IconImage;
        public Sprite BaseSprite;
        
        private const string k_SlotUssClassName = "inventory-slot";
        private const string k_SlotIconUssClassName = "inventory-slot-icon";

        public InventorySlot()
        {
            AddToClassList(k_SlotUssClassName);

            IconImage = new Image();
            IconImage.AddToClassList(k_SlotIconUssClassName);
            Add(IconImage);
        }

        public void SetItem(Sprite icon)
        {
            BaseSprite = icon;
            IconImage.image = BaseSprite != null ? icon.texture : null;
        }
    }
}