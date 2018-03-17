using System.Collections;
using UnityEngine;

public class animatedCookie : MonoBehaviour
{
    public Texture[] cookies;
    public float framesPerSecond = 15;
	Light m_light;
	int m_index = 0;


	void Awake ()
    {
        m_light = GetComponent<Light>();
        StartCoroutine(animateCookies());
    }
	
	IEnumerator animateCookies ()
    {
        while(true)
        {
            m_light.cookie = cookies[m_index];
            m_index++;
            if (m_index == cookies.Length)
            {
                m_index = 0;
            }

            yield return new WaitForSeconds (1/ framesPerSecond);
        }
       
    }
}
