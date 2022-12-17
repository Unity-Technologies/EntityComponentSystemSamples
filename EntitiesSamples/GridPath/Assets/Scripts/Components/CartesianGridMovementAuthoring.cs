using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

[AddComponentMenu("DOTS Samples/GridPath/Cartesian Grid Movement")]
public class CartesianGridMovementAuthoring : MonoBehaviour
{
    public enum MovementOptions
    {
        Bounce,
        FollowTarget
    };

    [Range(0.0f, 2.0f)]
    public float Speed;
    public MovementOptions Movement;

    class Baker : Baker<CartesianGridMovementAuthoring>
    {
        public override void Bake(CartesianGridMovementAuthoring authoring)
        {
            AddComponent( new CartesianGridDirection
            {
                Value = 0, // default N
            });
            AddComponent( new CartesianGridSpeed
            {
                Value = (ushort)(authoring.Speed * 1024.0f)
            });
            AddComponent( new CartesianGridCoordinates
            {
                x = 0,
                y = 0
            });

            if (authoring.Movement == MovementOptions.FollowTarget)
                AddComponent( new CartesianGridFollowTarget());
        }
    }
}
