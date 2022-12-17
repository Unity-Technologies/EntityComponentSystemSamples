using Unity.Entities;
using Unity.Mathematics;

class CannonBallAuthoring : UnityEngine.MonoBehaviour
{
    class CannonBallBaker : Baker<CannonBallAuthoring>
    {
        public override void Bake(CannonBallAuthoring authoring)
        {
            // By default, components are zero-initialized.
            // So in this case, the Speed field in CannonBall will be float3.zero.
            AddComponent<CannonBall>();
        }
    }
}

// Same approach for the cannon ball, we are creating a component to identify the entities.
// But this time it's not a tag component (empty) because it contains data: the Speed field.
// It won't be used immediately, but will become relevant when we implement motion.
struct CannonBall : IComponentData
{
    public float3 Speed;
}

