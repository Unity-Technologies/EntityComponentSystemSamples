using UnityEngine;

namespace Tutorials.Tanks.Step8
{
    // There are many ways of getting access to the main camera, but the approach using
    // a singleton (as we use here) works for any kind of MonoBehaviour.
    public class CameraSingleton : MonoBehaviour
    {
        public static Camera Instance;

        void Awake()
        {
            Instance = GetComponent<Camera>();
        }
    }
}
