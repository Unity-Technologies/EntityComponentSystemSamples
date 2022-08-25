using System;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using UnityEngine;
using MeshCollider = Unity.Physics.MeshCollider;

public struct ModifyNarrowphaseContacts : IComponentData
{
    public Entity surfaceEntity;
    public float3 surfaceNormal;
}

[RequireComponent(typeof(PhysicsBodyAuthoring))]
[DisallowMultipleComponent]
public class ModifyNarrowphaseContactsBehaviour : MonoBehaviour, IConvertGameObjectToEntity
{
    // SurfaceUpNormal used for non-mesh surfaces.
    // For mesh surface we get the normal from the individual polygon
    public Vector3 SurfaceUpNormal = Vector3.up;

    void OnEnable() {}

    void IConvertGameObjectToEntity.Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (enabled)
        {
            var component = new ModifyNarrowphaseContacts
            {
                surfaceEntity = entity,
                surfaceNormal = transform.rotation * SurfaceUpNormal
            };
            dstManager.AddComponentData(entity, component);
        }
    }
}

// A system which configures the simulation step to rotate certain contact normals
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(BuildPhysicsWorld)), UpdateBefore(typeof(StepPhysicsWorld))]
public partial class ModifyNarrowphaseContactsSystem : SystemBase
{
    StepPhysicsWorld m_StepPhysicsWorld;
    BuildPhysicsWorld m_BuildPhysicsWorld;

    protected override void OnCreate()
    {
        m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
        m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();

        RequireForUpdate(GetEntityQuery(new ComponentType[] { typeof(ModifyNarrowphaseContacts) }));
    }

    protected override void OnUpdate()
    {
        if (m_StepPhysicsWorld.Simulation.Type == SimulationType.NoPhysics) return;

        var modifier = GetSingleton<ModifyNarrowphaseContacts>();

        var surfaceNormal = modifier.surfaceNormal;
        var surfaceEntity = modifier.surfaceEntity;

        SimulationCallbacks.Callback callback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
        {
            return new ModifyNormalsJob
            {
                SurfaceEntity = surfaceEntity,
                SurfaceNormal = surfaceNormal,
                CollisionWorld = world.CollisionWorld,
            }.Schedule(simulation, ref world, inDeps);
        };
        m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateContacts, callback);
    }

    [BurstCompile]
    struct ModifyNormalsJob : IContactsJob
    {
        public Entity SurfaceEntity;
        public float3 SurfaceNormal;
        [ReadOnly] public CollisionWorld CollisionWorld;
        float distanceScale;

        public void Execute(ref ModifiableContactHeader contactHeader, ref ModifiableContactPoint contactPoint)
        {
            bool isBodyA = (contactHeader.EntityA == SurfaceEntity);
            bool isBodyB = (contactHeader.EntityB == SurfaceEntity);
            if (isBodyA || isBodyB)
            {
                if (contactPoint.Index == 0)
                {
                    // if we have a mesh surface we can get the surface normal from the plane of the polygon
                    var rbIdx = CollisionWorld.GetRigidBodyIndex(SurfaceEntity);
                    var body = CollisionWorld.Bodies[rbIdx];
                    if (body.Collider.Value.CollisionType == CollisionType.Composite)
                    {
                        unsafe
                        {
                            body.Collider.Value.GetLeaf(isBodyA ? contactHeader.ColliderKeyA : contactHeader.ColliderKeyB, out ChildCollider leafCollider);
                            if (leafCollider.Collider->Type == ColliderType.Triangle || leafCollider.Collider->Type == ColliderType.Quad)
                            {
                                PolygonCollider* polygonCollider = (PolygonCollider*)leafCollider.Collider;
                                // Potential optimization: If TransformFromChild has no rotation just use body.WorldFromBody.rot
                                // This is likely if you only have a MeshCollider with no hierarchy.
                                quaternion rotation = math.mul(body.WorldFromBody.rot, leafCollider.TransformFromChild.rot);
                                float3 surfaceNormal = math.rotate(rotation, polygonCollider->Planes[0].Normal);
                                distanceScale = math.dot(surfaceNormal, contactHeader.Normal);
                                contactHeader.Normal = surfaceNormal;
                            }
                        }
                    }
                }
                contactPoint.Distance *= distanceScale;
            }
        }
    }
}
