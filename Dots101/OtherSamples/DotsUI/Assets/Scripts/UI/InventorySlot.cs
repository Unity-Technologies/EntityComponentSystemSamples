using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.DotsUISample
{
    public class InventorySlot : VisualElement
    {
        Image m_IconImage;
        Sprite m_BaseSprite;
        
        const string k_SlotUssClassName = "inventory-slot";
        const string k_SlotIconUssClassName = "inventory-slot-icon";

        public InventorySlot()
        {
            AddToClassList(k_SlotUssClassName);

            m_IconImage = new Image();
            m_IconImage.AddToClassList(k_SlotIconUssClassName);
            Add(m_IconImage);
        }

        public void SetItem(Sprite icon)
        {
            m_BaseSprite = icon;
            m_IconImage.image = m_BaseSprite != null ? icon.texture : null;
        }
    }
}