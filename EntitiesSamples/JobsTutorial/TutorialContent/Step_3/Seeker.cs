using UnityEngine;

public class Seeker : MonoBehaviour
{
    public Vector3 Direction;

    public void Update()
    {
        transform.localPosition += Direction * Time.deltaTime;
    }
}