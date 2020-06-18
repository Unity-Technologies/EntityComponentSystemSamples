using UnityEngine;

public class Rotate_GO : MonoBehaviour
{
    public Vector3 LocalAngularVelocity = Vector3.zero;

    void Update()
    {
        var av = LocalAngularVelocity * Time.deltaTime;
        var rotation = Quaternion.Euler(av);
        transform.localRotation *= rotation;
    }
}
