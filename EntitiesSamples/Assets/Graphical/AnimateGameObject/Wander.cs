using UnityEngine;

namespace Graphical.AnimationWithGameObjects
{
    public class Wander : MonoBehaviour
    {
        private float m_nextActionTime;
        private float m_period;
        private Animator m_animator;
        private int m_isMovingID;

        private void Awake()
        {
            m_nextActionTime = Time.time;
            m_animator = GetComponent<Animator>();
            m_isMovingID = Animator.StringToHash("IsMoving");
        }

        void Update()
        {
            var timeLeft = m_nextActionTime - Time.time;

            if (timeLeft < 0f)
            {
                m_period = Random.Range(2f, 5f);
                m_nextActionTime += m_period;
                var angle = Random.Range(0f, 360f);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
            else
            {
                bool isMoving = timeLeft > m_period / 2f;
                m_animator.SetBool(m_isMovingID, isMoving);

                if (isMoving)
                {
                    transform.position += transform.forward * (Time.deltaTime * 3f);
                }
            }
        }
    }
}
