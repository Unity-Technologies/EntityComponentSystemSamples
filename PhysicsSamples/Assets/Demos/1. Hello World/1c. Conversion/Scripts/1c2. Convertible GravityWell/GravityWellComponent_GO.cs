using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class GravityWellComponent_GO : MonoBehaviour
{
    public float Strength = 100.0f;
    public float Radius = 10.0f;

    // Added Baker, but is disabled as this is in a redundant sample with ConvertToEntity gone
    /*
    #region ECS
    class Baker : Baker<GravityWellComponent_GO>
    {
        public override void Bake(GravityWellComponent_GO authoring)
        {
            AddComponent(new GravityWellComponent_GO_ECS
            {
                Strength = authoring.Strength,
                Radius = authoring.Radius,
                Position = GetComponent<Transform>().position,
            });
        }
    }
    #endregion
    */
}

#region ECS
public struct GravityWellComponent_GO_ECS : IComponentData
{
    public float Strength;
    public float Radius;
    // Include position of gravity well so all data accessible in one location
    public float3 Position;
}
#endregion
