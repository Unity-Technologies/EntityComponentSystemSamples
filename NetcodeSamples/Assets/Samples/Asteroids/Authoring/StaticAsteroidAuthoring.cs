using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public class StaticAsteroidAuthoring : MonoBehaviour
{
    [RegisterBinding(typeof(StaticAsteroid), "InitialPosition.x")]
    [RegisterBinding(typeof(StaticAsteroid), "InitialPosition.y")]
    public float2 InitialPosition;
    [RegisterBinding(typeof(StaticAsteroid), "InitialVelocity.x")]
    [RegisterBinding(typeof(StaticAsteroid), "InitialVelocity.y")]
    public float2 InitialVelocity;
    [RegisterBinding(typeof(StaticAsteroid), "InitialAngle")]
    public float InitialAngle;

    class Baker : Baker<StaticAsteroidAuthoring>
    {
        public override void Bake(StaticAsteroidAuthoring authoring)
        {
            StaticAsteroid component = default(StaticAsteroid);
            component.InitialPosition = authoring.InitialPosition;
            component.InitialVelocity = authoring.InitialVelocity;
            component.InitialAngle = authoring.InitialAngle;
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}
