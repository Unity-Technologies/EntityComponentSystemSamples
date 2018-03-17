using UnityEngine;

namespace TwoStickClassicExample
{
    public class Health : MonoBehaviour
    {

        public float Value
        {
            get { return m_Value; }
            set
            {
                m_Value = value;
                if (m_Value <= 0f)
                    Destroy(gameObject);
            }
        }
        [SerializeField] private float m_Value;
    }
}