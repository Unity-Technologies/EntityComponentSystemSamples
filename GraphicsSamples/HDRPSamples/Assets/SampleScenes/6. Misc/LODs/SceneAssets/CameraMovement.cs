using System.Collections;
using System.Collections.Generic;
using Unity.Transforms;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    private Vector3 m_direction;

    private static float TooClose = 3.0f;
    private static float TooFar = 500.0f;
    private static float SpeedFactor = 1.0f;

    // Start is called before the first frame update
    void Start()
    {
        // Just move away from the origin
        m_direction = transform.position.normalized;
    }

    // Update is called once per frame
    void Update()
    {
        float distance = transform.position.magnitude;
        if (distance >= TooFar || distance <= TooClose)
        {
            m_direction = -m_direction;
        }

        // Make speed comparable to the distance to make the LOD transitions happen quicker
        float speed = SpeedFactor * distance;
        
        Vector3 translation = speed * Time.deltaTime * m_direction;
        transform.Translate(translation, Space.World);
    }
}
