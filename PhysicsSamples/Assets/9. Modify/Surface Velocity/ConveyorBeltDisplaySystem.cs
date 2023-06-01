using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Modify
{
    // Displays conveyor belt data in Runtime, where it is impossible to do so using OnDrawGizmos().
    [UpdateInGroup(typeof(PhysicsSimulationGroup))]
    public partial struct ConveyorBeltDisplaySystem : ISystem
    {
        private EntityQuery conveyorBeltQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            conveyorBeltQuery = SystemAPI.QueryBuilder().WithAll<ConveyorBelt, ConveyorBeltDebugDisplayData>().Build();
            state.RequireForUpdate(conveyorBeltQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Properly chain up dependencies
            {
                if (!SystemAPI.TryGetSingleton<PhysicsDebugDisplayData>(out _))
                {
                    var singletonEntity = state.EntityManager.CreateEntity();
                    state.EntityManager.AddComponentData(singletonEntity, new PhysicsDebugDisplayData());
                }

                SystemAPI.GetSingletonRW<PhysicsDebugDisplayData>();
            }

            new DisplayConveyorBeltJob
            {
                DeltaTime = SystemAPI.Time.fixedDeltaTime
            }.Schedule();
        }

        // Returns true if drawing should be done.
        // Expecting speed in radians/s in case of isLinear == false
        public static bool ComputeDebugDisplayData(in RigidTransform localToWorld, float speed, float3 localDirection,
            float deltaTime, bool isLinear, ref float offset,
            out RigidTransform worldDrawingTransform, out float3 boxSize)
        {
            worldDrawingTransform = RigidTransform.identity;
            boxSize = float3.zero;

            if (speed == 0.0f)
            {
                return false;
            }

            if (isLinear && math.all(localDirection == float3.zero))
            {
                return false;
            }

            offset += deltaTime * speed;

            if (isLinear)
            {
                if (math.abs(offset) > math.abs(speed))
                {
                    offset = 0.0f;
                }

                worldDrawingTransform = math.mul(localToWorld,
                    new RigidTransform(quaternion.identity, offset * localDirection));
                boxSize = new float3(1.0f);
            }
            else
            {
                if (math.abs(offset) > 2 * math.PI)
                {
                    offset = 0.0f;
                }

                var axis = math.all(localDirection == float3.zero) ? math.up() : math.normalize(localDirection);
                var localFromOffset = new RigidTransform(quaternion.AxisAngle(axis, offset), float3.zero);

                worldDrawingTransform = math.mul(localToWorld, localFromOffset);
                boxSize = new float3(2.0f);
            }

            return true;
        }

        [BurstCompile]
        public partial struct DisplayConveyorBeltJob : IJobEntity
        {
            public float DeltaTime;

            public void Execute(in LocalToWorld localToWorld, in ConveyorBelt conveyorBelt,
                ref ConveyorBeltDebugDisplayData debugDisplayData)
            {
                if (ComputeDebugDisplayData(Math.DecomposeRigidBodyTransform(localToWorld.Value), conveyorBelt.Speed,
                    conveyorBelt.LocalDirection,
                    DeltaTime, !conveyorBelt.IsAngular, ref debugDisplayData.Offset,
                    out RigidTransform worldDrawingTransform, out float3 boxSize))
                {
                    PhysicsDebugDisplaySystem.Box(boxSize, worldDrawingTransform.pos, worldDrawingTransform.rot,
                        Unity.DebugDisplay.ColorIndex.Blue);
                }
            }
        }
    }
}
