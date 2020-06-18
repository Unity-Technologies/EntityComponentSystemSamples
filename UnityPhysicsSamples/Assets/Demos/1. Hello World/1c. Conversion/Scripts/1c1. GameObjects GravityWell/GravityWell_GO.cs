using UnityEngine;
using UnityEngine.Profiling;

public class GravityWell_GO : MonoBehaviour
{
    public float Strength = 100.0f;
    public float Radius = 10.0f;

    void Update()
    {
        Profiler.BeginSample("GravityWell_GO:Update");

        // Apply gravity well force to all Rigidbody components
        foreach (var dynamicBody in GameObject.FindObjectsOfType<Rigidbody>())
        {
            dynamicBody.AddExplosionForce(-Strength, gameObject.transform.position, Radius);
        }

        Profiler.EndSample();
    }
}
