using Unity.Entities;
using UnityEngine.UIElements;
using UnityEngine;

namespace Unity.DotsUISample
{
    // Base class for the UI elements
    // Inherits from ScriptableObject so that the instances can be stored in a UnityObjectRef
    public abstract class UIScreen : ScriptableObject
    {
        public const string k_VisibleClass = "screen-visible";
        public const string k_HiddenClass = "screen-hidden";

        public EntityCommandBuffer entityCommandBuffer { get; set; }

        public VisualElement RootElement { get; set; }

        public void Show()
        {
            RootElement.AddToClassList(k_VisibleClass);
            RootElement.BringToFront();
            RootElement.RemoveFromClassList(k_HiddenClass);
            RootElement.SetEnabled(true);
            RootElement.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            RootElement.AddToClassList(k_HiddenClass);
            RootElement.RemoveFromClassList(k_VisibleClass);
            RootElement.style.display = DisplayStyle.None;
        }
    }
}