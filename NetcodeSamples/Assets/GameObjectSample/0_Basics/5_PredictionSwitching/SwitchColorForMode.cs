using System;
using Unity.Netcode;
using UnityEngine;

namespace GameObjectSample
{
    /// <summary>
    /// Component added to all asteroids by the client that allow changing the material
    /// when the ghost mode is changed.
    /// </summary>
    public class SwitchColor : MonoBehaviour
    {
        private Color m_OriginalColor;
        private Material m_CachedMaterial;
        [SerializeField]
        private Color m_PredictedColor;

        void Awake()
        {
            if (!this.gameObject.NetcodeWorld().IsClient())
            {
                enabled = false;
                this.GetComponent<MeshRenderer>().enabled = false;
                return;
            }
        }

        void OnEnable()
        {
            if (m_CachedMaterial == null)
            {
                m_CachedMaterial = GetComponent<MeshRenderer>().material;
                m_OriginalColor = m_CachedMaterial.color;
            }
            m_CachedMaterial.color = m_OriginalColor;
            var ghost = GetComponent<GhostObject>();
            if (ghost.CanWriteState)
                SwitchToPredicted();
        }
        public void SwitchToInterpolated()
        {
            m_CachedMaterial.color = m_OriginalColor;
        }
        public void SwitchToPredicted()
        {
            m_CachedMaterial.color = m_PredictedColor;
        }
    }
}
