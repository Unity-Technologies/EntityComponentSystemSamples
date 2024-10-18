using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Physics.Authoring;
using Unity.Transforms;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(ConveyorBeltAuthoring)), CanEditMultipleObjects]
public class ConveyorBeltAuthoringEditor : Editor
{
    SerializedProperty m_Speed;
    SerializedProperty m_IsLinear;
    SerializedProperty m_LocalDirection;

    void OnEnable()
    {
        m_Speed = serializedObject.FindProperty("Speed");
        m_IsLinear = serializedObject.FindProperty("IsLinear");
        m_LocalDirection = serializedObject.FindProperty("LocalDirection");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(m_Speed);
        EditorGUILayout.PropertyField(m_IsLinear);
        using (new EditorGUI.DisabledGroupScope(!m_IsLinear.boolValue))
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(m_LocalDirection);
            EditorGUI.indentLevel--;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif

// Displays conveyor belt data in Editor.
[RequireComponent(typeof(PhysicsBodyAuthoring))]
public class ConveyorBeltAuthoring : MonoBehaviour
{
    public float Speed = 0.0f;
    public bool IsLinear = true;
    public Vector3 LocalDirection = Vector3.forward;

    private float _Offset = 0.0f;

    public void OnDrawGizmos()
    {
        float speed = Speed;
        if (!IsLinear)
        {
            speed = math.radians(speed);
        }

        if (DisplayConveyorBeltSystem.ComputeDebugDisplayData(Math.DecomposeRigidBodyTransform(transform.localToWorldMatrix), speed, LocalDirection,
            UnityEngine.Time.deltaTime, IsLinear, ref _Offset,
            out RigidTransform worldDrawingTransform, out float3 boxSize))
        {
            var originalColor = Gizmos.color;
            var originalMatrix = Gizmos.matrix;

            Gizmos.color = Color.blue;

            Matrix4x4 newMatrix = new Matrix4x4();
            newMatrix.SetTRS(worldDrawingTransform.pos, worldDrawingTransform.rot, Vector3.one);
            Gizmos.matrix = newMatrix;

            Gizmos.DrawWireCube(Vector3.zero, boxSize);

            Gizmos.color = originalColor;
            Gizmos.matrix = originalMatrix;
        }
    }
}

class ConveyorBeltBaker : Baker<ConveyorBeltAuthoring>
{
    public override void Bake(ConveyorBeltAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new ConveyorBelt
        {
            Speed = authoring.IsLinear ? authoring.Speed : math.radians(authoring.Speed),
            IsAngular = !authoring.IsLinear,
            LocalDirection = authoring.LocalDirection.normalized,
        });

        AddComponent(entity, new ConveyorBeltDebugDisplayData
        {
            Offset = 0.0f
        });
    }
}

public struct ConveyorBelt : IComponentData
{
    public float3 LocalDirection;
    public float Speed;
    public bool IsAngular;
}

public struct ConveyorBeltDebugDisplayData : IComponentData
{
    public float Offset;
}

// Displays conveyor belt data at Runtime
[UpdateInGroup(typeof(PhysicsSimulationGroup))]
public partial struct DisplayConveyorBeltSystem : ISystem
{
    private EntityQuery m_ConveyorBeltQuery;

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

            worldDrawingTransform = math.mul(localToWorld, new RigidTransform(quaternion.identity, offset * localDirection));
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

#if UNITY_EDITOR
    [BurstCompile]
    public partial struct DisplayConveyorBeltJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(in LocalToWorld localToWorld, in ConveyorBelt conveyorBelt, ref ConveyorBeltDebugDisplayData debugDisplayData)
        {
            if (ComputeDebugDisplayData(Math.DecomposeRigidBodyTransform(localToWorld.Value), conveyorBelt.Speed, conveyorBelt.LocalDirection,
                DeltaTime, !conveyorBelt.IsAngular, ref debugDisplayData.Offset,
                out RigidTransform worldDrawingTransform, out float3 boxSize))
            {
                PhysicsDebugDisplaySystem.Box(boxSize, worldDrawingTransform.pos, worldDrawingTransform.rot, Unity.DebugDisplay.ColorIndex.Blue);
            }
        }
    }
#endif

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp).WithAll<ConveyorBelt, ConveyorBeltDebugDisplayData>();
        m_ConveyorBeltQuery = state.GetEntityQuery(builder);
        state.RequireForUpdate(m_ConveyorBeltQuery);
        builder.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
#if UNITY_EDITOR
        // Properly chain up dependencies
        {
            if (!SystemAPI.TryGetSingleton<PhysicsDebugDisplayData>(out _))
            {
                var singletonEntity = state.EntityManager.CreateEntity();
                state.EntityManager.AddComponentData(singletonEntity, new PhysicsDebugDisplayData());
            }
            SystemAPI.GetSingletonRW<PhysicsDebugDisplayData>();
        }

        state.Dependency = new DisplayConveyorBeltJob
        {
            DeltaTime = SystemAPI.Time.fixedDeltaTime
        }.Schedule(state.Dependency);
#endif
    }
}

// A system which configures the simulation step to modify contact jacobians in various ways

[UpdateInGroup(typeof(PhysicsSimulationGroup))]
[UpdateAfter(typeof(PhysicsCreateContactsGroup))]
[UpdateBefore(typeof(PhysicsCreateJacobiansGroup))]
public partial struct PrepareConveyorBeltSystem : ISystem
{
    private ComponentLookup<ConveyorBelt> m_ConveyorBeltData;

    // This job reads the modify component and sets some data on the contact, to get propagated to the jacobian
    // for processing in our jacobian modifier job. This is necessary because some flags require extra data to
    // be allocated along with the jacobian (e.g., SurfaceVelocity data typically does not exist).
    [BurstCompile]
    struct SetConveyorBeltFlagJob : IContactsJob
    {
        [ReadOnly]
        public ComponentLookup<ConveyorBelt> ConveyorBelts;

        public void Execute(ref ModifiableContactHeader manifold, ref ModifiableContactPoint contact)
        {
            if (ConveyorBelts.HasComponent(manifold.EntityA) || ConveyorBelts.HasComponent(manifold.EntityB))
            {
                manifold.JacobianFlags |= JacobianFlags.EnableSurfaceVelocity;
            }
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadOnly<ConveyorBelt>()));
        m_ConveyorBeltData = state.GetComponentLookup<ConveyorBelt>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        m_ConveyorBeltData.Update(ref state);
        SimulationSingleton simulation = SystemAPI.GetSingleton<SimulationSingleton>();
        if (simulation.Type == SimulationType.NoPhysics)
        {
            return;
        }

        state.Dependency = new SetConveyorBeltFlagJob
        {
            ConveyorBelts = m_ConveyorBeltData
        }.Schedule(simulation, ref SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRW.PhysicsWorld, state.Dependency);
    }
}


[UpdateInGroup(typeof(PhysicsSimulationGroup))]
[UpdateBefore(typeof(PhysicsSolveAndIntegrateGroup))]
[UpdateAfter(typeof(PhysicsCreateJacobiansGroup))]
public partial struct ConveyorBeltSystem : ISystem
{
    private ComponentLookup<ConveyorBelt> m_ConveyorBeltData;

    [BurstCompile]
    struct SetConveyorBeltSpeedJob : IJacobiansJob
    {
        [ReadOnly] public ComponentLookup<ConveyorBelt> ConveyorBelts;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<RigidBody> Bodies;

        // Don't do anything for triggers
        public void Execute(ref ModifiableJacobianHeader h, ref ModifiableTriggerJacobian j) {}

        public void Execute(ref ModifiableJacobianHeader jacHeader, ref ModifiableContactJacobian contactJacobian)
        {
            if (!jacHeader.HasSurfaceVelocity) return;

            float3 linearVelocity = float3.zero;
            float3 angularVelocity = float3.zero;

            // Get the surface velocities if available
            for (int i = 0; i < 2; i++)
            {
                var entity = (i == 0) ? jacHeader.EntityA : jacHeader.EntityB;
                if (!ConveyorBelts.HasComponent(entity)) continue;

                var index = (i == 0) ? jacHeader.BodyIndexA : jacHeader.BodyIndexB;
                var rotation = Bodies[index].WorldFromBody.rot;
                var belt = ConveyorBelts[entity];

                if (belt.IsAngular)
                {
                    // assuming rotation is around contact normal.
                    var av = contactJacobian.Normal * belt.Speed;

                    // calculate linear velocity at point, assuming rotating around body pivot
                    var otherIndex = (i == 0) ? jacHeader.BodyIndexB : jacHeader.BodyIndexA;
                    var offset = Bodies[otherIndex].WorldFromBody.pos - Bodies[index].WorldFromBody.pos;
                    var lv = math.cross(av, offset);

                    angularVelocity += av;
                    linearVelocity += lv;
                }
                else
                {
                    linearVelocity += math.rotate(rotation, belt.LocalDirection) * belt.Speed;
                }
            }

            // Add the extra velocities
            jacHeader.SurfaceVelocity = new SurfaceVelocity
            {
                LinearVelocity = jacHeader.SurfaceVelocity.LinearVelocity + linearVelocity,
                AngularVelocity = jacHeader.SurfaceVelocity.AngularVelocity + angularVelocity,
            };
        }
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate(state.GetEntityQuery(ComponentType.ReadOnly<ConveyorBelt>()));
        m_ConveyorBeltData = state.GetComponentLookup<ConveyorBelt>(true);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        m_ConveyorBeltData.Update(ref state);
        SimulationSingleton simulation = SystemAPI.GetSingleton<SimulationSingleton>();
        if (simulation.Type == SimulationType.NoPhysics)
        {
            return;
        }

        var world = SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRW.PhysicsWorld;

        state.Dependency = new SetConveyorBeltSpeedJob
        {
            ConveyorBelts = m_ConveyorBeltData,
            Bodies = world.Bodies
        }.Schedule(simulation, ref world, state.Dependency);
    }
}
