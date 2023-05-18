using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Unity.NetCode.Samples
{
    /// <summary>
    /// Authoring class used in the tests or in game scene. Add all the client-only components to the ghost prefab.
    /// </summary>
    internal class ClientOnlyAuthoring : MonoBehaviour
    {
    }

    /// <summary>
    /// Singleton component used to enable the client-only backup systems.
    /// </summary>
    public struct EnableClientOnlyBackup : IComponentData
    {
    }
}
