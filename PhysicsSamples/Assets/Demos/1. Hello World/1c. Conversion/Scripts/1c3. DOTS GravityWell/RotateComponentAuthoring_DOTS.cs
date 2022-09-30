using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class RotateComponentAuthoring_DOTS : MonoBehaviour
{
    public Vector3 LocalAngularVelocity = Vector3.zero; // in degrees/sec

    class RotateComponentBaker : Baker<RotateComponentAuthoring_DOTS>
    {
        public override void Bake(RotateComponentAuthoring_DOTS authoring)
        {
            AddComponent(new RotateComponent_DOTS
            {
                // We can convert to radians/sec once here.
                LocalAngularVelocity = math.radians(authoring.LocalAngularVelocity),
            });
        }
    }
}
