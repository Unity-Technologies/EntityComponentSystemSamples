using System;
using Unity.Entities;
using UnityEngine;

namespace Unity.NetCode.Samples.Common
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class SetLocalPlayerGraphicsColorsSystem : SystemBase
    {
        int m_LastNetworkId;
        int m_LastAppliedListVersion;

        public Color TargetColor { get; private set; }

        protected override void OnCreate()
        {
            TargetColor = Color.grey;
        }

        protected override void OnUpdate()
        {
            if (SetOutlineToLocalPlayerColor.All.Count <= 0)
                return;

            SystemAPI.TryGetSingleton(out NetworkId networkId);
            if (networkId.Value != m_LastNetworkId)
            {
                m_LastNetworkId = networkId.Value;
                TargetColor = networkId.Value > 0 ? NetworkIdDebugColorUtility.GetColor(networkId.Value) : Color.grey;
                RefreshWidgets();
            }
            else if (SetOutlineToLocalPlayerColor.ListVersion != m_LastAppliedListVersion)
            {
                RefreshWidgets();
            }
        }

        void RefreshWidgets()
        {
            m_LastAppliedListVersion = SetOutlineToLocalPlayerColor.ListVersion;

            foreach (var target in SetOutlineToLocalPlayerColor.All)
                target.Refresh(TargetColor);
        }
    }
}
