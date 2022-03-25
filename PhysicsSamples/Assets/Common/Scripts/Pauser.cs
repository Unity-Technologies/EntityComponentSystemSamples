using UnityEngine;

public class Pauser : MonoBehaviour
{
    void FixedUpdate()
    {
    }

    void Update()
    {
        if (++CurrentStep == PauseStep)
        {
            Debug.Break();
        }
    }

    public int CurrentStep;
    public int PauseStep;
}
