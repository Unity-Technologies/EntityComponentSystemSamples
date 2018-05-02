using System.Collections.Generic;
using UnityEngine;

namespace TwoStickClassicExample
{
    public class Health : MonoBehaviour
    {
        public static ICollection<Health> All => allHealths;
        private static readonly HashSet<Health> allHealths = new HashSet<Health>();

        private void Awake()
        {
            allHealths.Add(this);
        }
        
        private void OnDestroy()
        {
            allHealths.Remove(this);
        }

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