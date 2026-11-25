using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Netcode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Sample
{
    [Serializable]
    public struct MyStruct // used in GhostField (source genned component)
    {
        public float valueFloat;
        public short valueShort;
        public byte valueByte;
    }

    public partial class MyGhostBehaviour : GhostBehaviour
    {
        // Generated underlying ECS component
        [SerializeField] GhostField<float> someValue;
        [SerializeField] GhostField<int> m_Health = new(fieldConfig: new FieldConfig(){Quantization = 0});
        [SerializeField] GhostField<int> m_Mana = new(10);
        public GhostField<float> m_Armor = new(20.1f, new (){MaxSmoothingDistance = 10f});
        [SerializeField] GhostField<MyStruct> m_SomeState;
        GhostField<Entity> m_RefToAnotherGhost; // This allows syncing a reference to another ghost. Netcode will automatically find the appropriate ghost on the other side

        GhostField<FixedString512Bytes> m_SomeString; // A bit of an anti pattern to sync strings using state syncing, but doable using FixedStrings
        GhostField<NetworkEndpoint> m_SomeEndpoint;
        GhostField<NetworkTick> m_SomeTick;

        // Manually specify ECS component (can be read by other systems and shared with other GhostBehaviours)
        [SerializeField] GhostComponentRef<MyShieldComp> m_Shield;

        void Update()
        {
            if (!IsServer)
                return;

            m_Health.Value += 1;
            var shield = m_Shield.Read();
            shield.Shield = 123; // alone, this doesn't do anything, you need to set it again with Write() since this is a value type
            m_Armor.Value += 1.11111f;
            m_Mana.Value += 123;

            // returning by copy means you need a temp var
            var someState = m_SomeState.Value;
            someState.valueByte += 1;
            someState.valueFloat += 1f;
            someState.valueShort += 1;
            m_SomeState.Value = someState;

            var comp = m_Shield.Read();
            comp.Shield += 2;
            m_Shield.Write(comp);

            m_SomeString.Value = "Hello World";
        }
    }
}
