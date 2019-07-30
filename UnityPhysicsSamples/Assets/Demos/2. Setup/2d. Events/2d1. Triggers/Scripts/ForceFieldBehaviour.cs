using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

public struct ForceField : IComponentData
{
    public int enabled;
    public float3 center;
    public float deadzone;
    public float strength;
    public float rotation;
    public int axis;
    public int proportional;
    public int massInvariant;
}

public class ForceFieldBehaviour : MonoBehaviour, IConvertGameObjectToEntity
{
    public enum Direction { Center, XAxis, YAxis, ZAxis };

    public bool componentEnabled = true;
    public float strength = 10f;
    public float deadzone = 0.5f;
    public Direction axis = Direction.Center;
    public float rotation = 0;
    public bool proportional = true;
    public bool massInvariant = false;

    void OnEnable() { }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (enabled)
        {
            dstManager.AddComponentData<ForceField>(entity, new ForceField()
            {
                enabled = componentEnabled ? 1 : 0,
                center = transform.position,
                strength = strength,
                deadzone = (deadzone == 0) ? 0.001f : math.abs(deadzone),
                axis = (int)axis - 1,
                rotation = math.radians(rotation),
                proportional = proportional ? 1 : 0,
                massInvariant = massInvariant ? 1 : 0
            });
        }
    }
}


#region System
[UpdateBefore(typeof(BuildPhysicsWorld))]
public class ForceFieldSystem : JobComponentSystem
{
    [BurstCompile]
    public struct ForceFieldJob : IJobForEach<Translation, Rotation, PhysicsMass, PhysicsVelocity, ForceField>
    {
        public float dt;

        public void Execute([ReadOnly]ref Translation pos,
                            [ReadOnly]ref Rotation rot,
                            [ReadOnly]ref PhysicsMass bodyMass,
                            ref PhysicsVelocity bodyVelocity,
                            [ReadOnly]ref ForceField forceField)
        {
            if ((forceField.enabled == 0) || (forceField.strength == 0))
                return;

            // Don't do anything if in eye
            float3 dir = float3.zero;
            dir = (forceField.center - pos.Value);
            if (!math.any(dir))
                return;

            // If force field around axis then project dir onto axis
            float3 axis = float3.zero;
            if (forceField.axis != -1)
            {
                axis[forceField.axis] = 1f;
                dir -= axis * math.dot(dir, axis);
            }

            float strength = forceField.strength;
            float dist2 = math.lengthsq(dir);

            // Kill strength if in deadzone
            float dz2 = forceField.deadzone * forceField.deadzone;
            if (dz2 > dist2)
                strength = 0;

            // If out of center and proportional divide by distance squared
            if (forceField.proportional != 0)
                strength = (dist2 > 1e-4f) ? strength / dist2 : 0;

            // Multiple through mass if want all objects moving equally
            dir = math.normalizesafe(dir);
            float mass = math.rcp(bodyMass.InverseMass);
            if (forceField.massInvariant != 0) mass = 1f;
            strength *= mass * dt;
            bodyVelocity.Linear += strength * dir;

            // If want a rotational force field add extra twist deltas
            if ((forceField.axis != -1) && (forceField.rotation != 0))
            {
                bodyVelocity.Linear += forceField.rotation * strength * dir;
                dir = math.cross(axis, -dir);
                bodyVelocity.Linear += forceField.rotation * strength * dir;
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var job = new ForceFieldJob { dt = Time.fixedDeltaTime };
        return job.Schedule(this, inputDeps);
    }
}
#endregion

