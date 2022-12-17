using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct PrespawnDataElement : IBufferElementData
    {
        [GhostField] public int Value;
    }

    [DisallowMultipleComponent]
    public class PrespawnDataElementAuthoring : MonoBehaviour
    {
        public int[] Values;

        class Baker : Baker<PrespawnDataElementAuthoring>
        {
            public override void Bake(PrespawnDataElementAuthoring authoring)
            {
                DynamicBuffer<PrespawnDataElement> dynamicBuffer = AddBuffer<PrespawnDataElement>();
                dynamicBuffer.ResizeUninitialized(authoring.Values.Length);
                for (int i = 0; i < dynamicBuffer.Length; i++)
                {
                    dynamicBuffer[i] = new PrespawnDataElement{Value = authoring.Values[i]};
                }
            }
        }
    }
}
