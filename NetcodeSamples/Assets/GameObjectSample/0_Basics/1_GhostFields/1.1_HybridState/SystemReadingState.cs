using System;
using Unity.Entities;
using Unity.Netcode;
using UnityEngine;

namespace Sample
{
    [Serializable]
    public struct MyShieldComp : IComponentData // used in Bridge (source gen only for initialization)
    {
        [GhostField] public int Shield;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct SystemReadingState : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MyShieldComp>();
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach (var myShieldComp in SystemAPI.Query<MyShieldComp>())
            {
                // Debug.Log($"We got a MyShield from a system in client world! Value is {myShieldComp.Shield}");
            }
        }
    }
}
