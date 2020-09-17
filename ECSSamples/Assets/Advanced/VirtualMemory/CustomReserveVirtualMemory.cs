using System.Runtime.InteropServices;
using Unity.Entities;
using UnityEngine;

public class CustomReserveVirtualMemory : ICustomBootstrap
{
    // Using a custom bootstrap that returns false lets you set extra data (such as the total address space you want to reserve for ECS Chunks) before worlds are default initialized.
    public bool Initialize(string defaultWorldName)
    {
        // ICustomBoostrap runs for all scenes in a project, so to gate this sample to a particular scene, we have to use this hack that checks the active scene's name.
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "VirtualMemory")
            EntityManager.TotalChunkAddressSpaceInBytes = 1024UL * 1024UL * 16UL;

        return false;
    }
}
