using System.Collections.Generic;
using Unity.Entities;
using Unity.Netcode;
using UnityEngine;

namespace Samples.HelloNetcode.Physics
{
    [RequireComponent(typeof(Rigidbody))]
    public partial class PushableBody : GhostBehaviour
    {
        public static Dictionary<World, List<Rigidbody>> AllBodies = new();

        Rigidbody m_Rigidbody;

        public override void Awake()
        {
            base.Awake();
            m_Rigidbody = GetComponent<Rigidbody>();
            if (!AllBodies.ContainsKey(Ghost.World))
                AllBodies.Add(Ghost.World, new List<Rigidbody>());
            AllBodies[Ghost.World].Add(m_Rigidbody);
        }

        public override void OnDestroy()
        {
            AllBodies[Ghost.World].Remove(m_Rigidbody);
            base.OnDestroy();
        }
    }
}
