using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Modify
{
    // A system which configures the simulation step to rotate certain contact normals
    [UpdateInGroup(typeof(PhysicsSimulationGroup))]
    [UpdateAfter(typeof(PhysicsCreateContactsGroup)), UpdateBefore(typeof(PhysicsCreateJacobiansGroup))]
    public partial struct NarrowphaseContactsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NarrowphaseContacts>();
            state.RequireForUpdate<SimulationSingleton>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var simulationSingleton = SystemAPI.GetSingletonRW<SimulationSingleton>().ValueRW;
            if (simulationSingleton.Type == SimulationType.NoPhysics)
            {
                return;
            }

            var modifier = SystemAPI.GetSingleton<NarrowphaseContacts>();
            var surfaceNormal = modifier.surfaceNormal;
            var surfaceEntity = modifier.surfaceEntity;
            var world = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

            state.Dependency = new ModifyNormalsJob
            {
                SurfaceEntity = surfaceEntity,
                SurfaceNormal = surfaceNormal,
                CollisionWorld = world.CollisionWorld
            }.Schedule(simulationSingleton, ref world, state.Dependency);
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
                                body.Collider.Value.GetLeaf(
                                    isBodyA ? contactHeader.ColliderKeyA : contactHeader.ColliderKeyB,
                                    out ChildCollider leafCollider);
                                if (leafCollider.Collider->Type == ColliderType.Triangle ||
                                    leafCollider.Collider->Type == ColliderType.Quad)
                                {
                                    PolygonCollider* polygonCollider = (PolygonCollider*)leafCollider.Collider;
                                    // Potential optimization: If TransformFromChild has no rotation just use body.WorldFromBody.rot
                                    // This is likely if you only have a MeshCollider with no hierarchy.
                                    quaternion rotation = math.mul(body.WorldFromBody.rot,
                                        leafCollider.TransformFromChild.rot);
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
}
