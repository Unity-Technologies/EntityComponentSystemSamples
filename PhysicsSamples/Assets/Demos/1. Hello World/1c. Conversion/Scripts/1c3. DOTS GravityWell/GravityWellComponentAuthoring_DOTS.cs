using Unity.Entities;
using UnityEngine;

public class GravityWellComponentAuthoring_DOTS : MonoBehaviour
{
    public float Strength = 100.0f;
    public float Radius = 10.0f;

    class GravityWellComponentBaker : Baker<GravityWellComponentAuthoring_DOTS>
    {
        public override void Bake(GravityWellComponentAuthoring_DOTS authoring)
        {
            AddComponent(new GravityWellComponent_DOTS
            {
                Strength = authoring.Strength,
                Radius = authoring.Radius,
                Position = GetComponent<Transform>().position
            });
        }
    }
}
