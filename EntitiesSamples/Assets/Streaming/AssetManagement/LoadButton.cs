using UnityEngine;

namespace Streaming.AssetManagement
{
    public class LoadButton : MonoBehaviour
    {
#if !UNITY_DISABLE_MANAGED_COMPONENTS

        public bool Loaded;
        public bool Toggle;

        public void Start()
        {
            Loaded = true;
        }

        void OnGUI()
        {
            if (Loaded)
            {
                if (GUI.Button(new Rect(10, 10, 150, 80), "Unload Assets"))
                {
                    Toggle = true;
                }
            }
            else
            {
                if (GUI.Button(new Rect(10, 10, 150, 80), "Load Assets"))

                {
                    Toggle = true;
                }
            }
        }
#endif
    }
}
