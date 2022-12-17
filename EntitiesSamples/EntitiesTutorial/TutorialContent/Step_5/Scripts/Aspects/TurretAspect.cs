using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

// Instead of directly accessing the Turret component, we are creating an aspect.
// Aspects allows you to provide a customized API for accessing your components.
readonly partial struct TurretAspect : IAspect
{
    // This reference provides read only access to the Turret component.
    // Trying to use ValueRW (instead of ValueRO) on a read-only reference is an error.
    readonly RefRO<Turret> m_Turret;

    // Note the use of ValueRO in the following properties.
    public Entity CannonBallSpawn => m_Turret.ValueRO.CannonBallSpawn;
    public Entity CannonBallPrefab => m_Turret.ValueRO.CannonBallPrefab;
}