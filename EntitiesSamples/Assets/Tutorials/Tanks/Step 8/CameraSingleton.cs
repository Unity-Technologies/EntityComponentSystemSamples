using UnityEngine;

namespace Tutorials.Tanks.Step8
{
    // There are many ways of getting access to the main camera, but the approach here using
    // a singleton works for any kind of MonoBehaviour.
    public class CameraSingleton : MonoBehaviour
    {
        public static Camera Instance;

        void Awake()
        {
            Instance = GetComponent<Camera>();
        }
    }
}
