using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Physics.Authoring;

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

[RequireComponent(typeof(PhysicsBodyAuthoring))]
public class ConveyorBeltAuthoring : MonoBehaviour
{
    public float Speed = 0.0f;
    public bool IsLinear = true;
    public Vector3 LocalDirection = Vector3.forward;

    private float _Offset = 0.0f;

    public void OnDrawGizmos()
    {
        if (Speed == 0.0f) return;
        if (IsLinear && LocalDirection.Equals(Vector3.zero)) return;

        _Offset += UnityEngine.Time.deltaTime * Speed;

        var originalColor = Gizmos.color;
        var originalMatrix = Gizmos.matrix;

        Gizmos.color = Color.blue;

        // Calculate the final Physics Body runtime coordinate system which bakes out skew from non-uniform scaling in parent
        var worldFromLocalRigidTransform = Math.DecomposeRigidBodyTransform(transform.localToWorldMatrix);
        var worldFromLocal = Matrix4x4.TRS(worldFromLocalRigidTransform.pos, worldFromLocalRigidTransform.rot, Vector3.one);

        if (IsLinear)
        {
            if (Mathf.Abs(_Offset) > Mathf.Abs(Speed)) _Offset = 0.0f;

            Gizmos.matrix = worldFromLocal;
            Gizmos.DrawWireCube(_Offset * LocalDirection, Vector3.one);
        }
        else
        {
            if (Mathf.Abs(_Offset) > 360) _Offset = 0.0f;

            var axis = LocalDirection.Equals(Vector3.zero) ? Vector3.up : LocalDirection.normalized;
            var localFromOffset = Matrix4x4.Rotate(Quaternion.AngleAxis(_Offset, axis));

            Gizmos.matrix = worldFromLocal * localFromOffset;
            Gizmos.DrawWireSphere(Vector3.zero, 1.0f);
        }

        Gizmos.matrix = originalMatrix;
        Gizmos.color = originalColor;
    }
}

class ConveyorBeltBaker : Baker<ConveyorBeltAuthoring>
{
    public override void Bake(ConveyorBeltAuthoring authoring)
    {
        AddComponent(new ConveyorBelt
        {
            Speed = authoring.IsLinear ? authoring.Speed : math.radians(authoring.Speed),
            IsAngular = !authoring.IsLinear,
            LocalDirection = authoring.LocalDirection.normalized,
        });
    }
}

public struct ConveyorBelt : IComponentData
{
    public float3 LocalDirection;
    public float Speed;
    public bool IsAngular;
}

// A system which configures the simulation step to modify contact jacobians in various ways

[UpdateInGroup(typeof(PhysicsSimulationGroup))]
[UpdateAfter(typeof(PhysicsCreateContactsGroup))]
[UpdateBefore(typeof(PhysicsCreateJacobiansGroup))]
[BurstCompile]
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
    public void OnDestroy(ref SystemState state)
    {
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
[BurstCompile]
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
    public void OnDestroy(ref SystemState state)
    {
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
