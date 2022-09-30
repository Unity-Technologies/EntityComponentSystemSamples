using Unity.Entities;
using UnityEngine;

[AddComponentMenu("DOTS Samples/GridPath/Cartesian Grid Target")]
public class CartesianGridTargetAuthoring : MonoBehaviour
{
    class Baker : Baker<CartesianGridTargetAuthoring>
    {
        public override void Bake(CartesianGridTargetAuthoring authoring)
        {
            AddComponent<CartesianGridTarget>();
            AddComponent(new CartesianGridTargetCoordinates { x = -1, y = -1 });
        }
    }
}
