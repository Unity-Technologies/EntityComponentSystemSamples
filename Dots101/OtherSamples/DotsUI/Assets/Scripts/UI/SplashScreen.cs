using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.DotsUISample
{
    public class SplashScreen : UIScreen
    {
        public static SplashScreen Instantiate(VisualElement parentElement)
        {
            var instance = ScriptableObject.CreateInstance<SplashScreen>();
            instance.RootElement = parentElement;
            instance.RootElement.style.display = DisplayStyle.None;
            return instance;
        }
    }
}