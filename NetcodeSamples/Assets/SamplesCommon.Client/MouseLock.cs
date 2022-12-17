using UnityEngine;
using UnityEngine.UI;

namespace Unity.NetCode.Samples.Common
{
    public class MouseLock : MonoBehaviour
    {
        bool m_IsLocked;
        Toggle m_Toggle;
        void Start()
        {
#if !UNITY_EDITOR && !UNITY_STANDALONE
            gameObject.SetActive(false);
#endif
            m_Toggle = GetComponent<Toggle>();
        }

        void OnDestroy()
        {
            if (m_IsLocked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        void LateUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                m_Toggle.isOn = !m_Toggle.isOn;
            if (m_IsLocked != m_Toggle.isOn)
            {
                m_IsLocked = m_Toggle.isOn;
                Cursor.lockState = m_IsLocked ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !m_IsLocked;
            }
        }
    }
}
