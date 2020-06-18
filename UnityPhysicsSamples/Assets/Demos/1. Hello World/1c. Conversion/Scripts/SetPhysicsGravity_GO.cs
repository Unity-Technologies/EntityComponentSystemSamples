using UnityEngine;

public class SetPhysicsGravity_GO : MonoBehaviour
{
    public Vector3 Gravity = new Vector3(0, -9.81f, 0);

    void Awake()
    {
        Physics.gravity = Gravity;
    }
}
