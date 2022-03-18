using System;
using Unity.Entities;
using UnityEngine;

namespace EnterPlayModeTests
{
    [ExecuteAlways]
    public class DefaultWorldInitializationVerificationScript : MonoBehaviour
    {
        [NonSerialized]
        public bool WasEnabled = false;

        public void OnEnable()
        {
            Debug.Log("DefaultWorldInitializationVerificationScript.OnEnable");
            DefaultWorldInitialization.DefaultLazyEditModeInitialize();

            WasEnabled = true;
        }

        public void OnDisable()
        {
            Debug.Log("DefaultWorldInitializationVerificationScript.OnDisable");
            WasEnabled = false;
        }
    }
}
