using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[DisallowMultipleComponent]
public class StaticAsteroidAuthoring : MonoBehaviour
{
    [RegisterBinding(typeof(StaticAsteroid), "InitialPosition.x", true)]
    [RegisterBinding(typeof(StaticAsteroid), "InitialPosition.y", true)]
    public float2 InitialPosition;
    [RegisterBinding(typeof(StaticAsteroid), "InitialVelocity.x", true)]
    [RegisterBinding(typeof(StaticAsteroid), "InitialVelocity.y", true)]
    public float2 InitialVelocity;
    [RegisterBinding(typeof(StaticAsteroid), "InitialAngle")]
    public float InitialAngle;
    [RegisterBinding(typeof(StaticAsteroid), "SpawnTick")]
    public Unity.NetCode.NetworkTick SpawnTick;

    class StaticAsteroidBaker : Baker<StaticAsteroidAuthoring>
    {
        public override void Bake(StaticAsteroidAuthoring authoring)
        {
            StaticAsteroid component = default(StaticAsteroid);
            component.InitialPosition = authoring.InitialPosition;
            component.InitialVelocity = authoring.InitialVelocity;
            component.InitialAngle = authoring.InitialAngle;
            component.SpawnTick = authoring.SpawnTick;
            AddComponent(component);
        }
    }
}
