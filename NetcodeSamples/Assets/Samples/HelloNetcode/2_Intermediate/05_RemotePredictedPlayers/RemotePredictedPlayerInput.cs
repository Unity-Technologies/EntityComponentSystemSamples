using System;
using Unity.Collections;
using Unity.NetCode;

namespace Samples.HelloNetcode
{
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct RemotePredictedPlayerInput : IInputComponentData
    {
        [GhostField] public int Horizontal;
        [GhostField] public int Vertical;
        [GhostField] public InputEvent Jump;

        public FixedString512Bytes ToFixedString() => $"h:{Horizontal},v:{Vertical},jump:{Jump.ToFixedString()}";
    }
}
