using System;
using UnityEngine;

public class ExperimentalWarning : MonoBehaviour
{
    void Awake()
    {
#if UNITY_6000_3_OR_NEWER && NETCODE_GAMEOBJECT_BRIDGE_EXPERIMENTAL
        GameObject.Destroy(this.gameObject);
#endif
    }

    void OnGUI()
    {
        // With experimental defines, samples are just doing nothing. It could appear as a bug to users inspecting those. Giving them a warning they are missing key pieces
        // to make the sample work.
        // This lives outside specific sample assemblies, since those whole assemblies could have a define constraint
        bool isU6 = false;
        bool ghostBridgeEnabled = false;
        #if UNITY_6000_3_OR_NEWER
            isU6 = true;
        #endif
        #if NETCODE_GAMEOBJECT_BRIDGE_EXPERIMENTAL
            ghostBridgeEnabled = true;
        #endif
        GUI.color = Color.red;
        if (!ghostBridgeEnabled)
        {
            GUI.Label(new Rect(10, 10, 600, 50), "This sample scene is using experimental features and is disabled, please use the NETCODE_GAMEOBJECT_BRIDGE_EXPERIMENTAL define.");
        }

        if (!isU6)
        {
            GUI.Label(new Rect(10, 70, 600, 100), "This sample scene requires Unity 6.3 and above and so is currently disabled.");
        }
    }
}
