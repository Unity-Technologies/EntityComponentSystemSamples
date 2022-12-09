using UnityEngine;

public class RotationAndScaleReference : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        RotationAndScale rs = default;
        rs.ComputeTransform(Time.time);
        transform.localRotation = rs.R;
        transform.localScale = rs.S;
    }
}
