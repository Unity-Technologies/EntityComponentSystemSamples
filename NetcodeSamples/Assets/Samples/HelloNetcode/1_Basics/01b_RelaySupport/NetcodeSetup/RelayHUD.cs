using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;

namespace Samples.HelloNetcode
{
    public struct JoinCode : IComponentData
    {
        public FixedString64Bytes Value;
    }

    public class RelayHUD : MonoBehaviour
    {
        public Text JoinCodeLabel;

        public void Awake()
        {
            var world = World.All[0];
            var joinQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<JoinCode>());
            if (joinQuery.HasSingleton<JoinCode>())
            {
                var joinCode = joinQuery.GetSingleton<JoinCode>().Value;
                JoinCodeLabel.text = $"Join code: {joinCode}";
            }
        }
    }
}
