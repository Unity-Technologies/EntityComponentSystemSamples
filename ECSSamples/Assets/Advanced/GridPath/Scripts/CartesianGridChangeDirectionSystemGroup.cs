using Unity.Entities;

[UpdateBefore(typeof(CartesianGridMoveForwardSystem))]
public class CartesianGridChangeDirectionSystemGroup : ComponentSystemGroup
{
}