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
public class ConveyorBeltAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float Speed = 0.0f;
    public bool IsLinear = true;
    public Vector3 LocalDirection = Vector3.forward;

    private float _Offset = 0.0f;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new ConveyorBelt
        {
            Speed = IsLinear ? Speed : math.radians(Speed),
            IsAngular = !IsLinear,
            LocalDirection = LocalDirection.normalized,
        });
    }

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

public struct ConveyorBelt : IComponentData
{
    public float3 LocalDirection;
    public float Speed;
    public bool IsAngular;
}


// A system which configures the simulation step to modify contact jacobains in various ways
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(StepPhysicsWorld))]
public class ConveyorBeltSystem : SystemBase
{
    StepPhysicsWorld m_StepPhysicsWorld;
    SimulationCallbacks.Callback m_PreparationCallback;
    SimulationCallbacks.Callback m_JacobianModificationCallback;

    protected override void OnCreate()
    {
        m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();

        m_PreparationCallback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
        {
            var job = new SetConveyorBeltFlagJob
            {
                ConveyorBelts = GetComponentDataFromEntity<ConveyorBelt>(true)
            }.Schedule(simulation, ref world, inDeps);
            return job;
        };

        m_JacobianModificationCallback = (ref ISimulation simulation, ref PhysicsWorld world, JobHandle inDeps) =>
        {
            var job = new SetConveyorBeltSpeedJob
            {
                ConveyorBelts = GetComponentDataFromEntity<ConveyorBelt>(true),
                Bodies = world.Bodies,
            }.Schedule(simulation, ref world, inDeps);
            return job;
        };

        RequireForUpdate(GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(ConveyorBelt) }
        }));
    }

    // This job reads the modify component and sets some data on the contact, to get propagated to the jacobian
    // for processing in our jacobian modifier job. This is necessary because some flags require extra data to
    // be allocated along with the jacobian (e.g., SurfaceVelocity data typically does not exist).
    [BurstCompile]
    struct SetConveyorBeltFlagJob : IContactsJob
    {
        [ReadOnly]
        public ComponentDataFromEntity<ConveyorBelt> ConveyorBelts;

        public void Execute(ref ModifiableContactHeader manifold, ref ModifiableContactPoint contact)
        {
            if (ConveyorBelts.HasComponent(manifold.EntityA) || ConveyorBelts.HasComponent(manifold.EntityB))
            {
                manifold.JacobianFlags |= JacobianFlags.EnableSurfaceVelocity;
            }
        }
    }

    [BurstCompile]
    struct SetConveyorBeltSpeedJob : IJacobiansJob
    {
        [ReadOnly] public ComponentDataFromEntity<ConveyorBelt> ConveyorBelts;
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

    protected override void OnUpdate()
    {
        if (m_StepPhysicsWorld.Simulation.Type == SimulationType.NoPhysics) return;

        m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateContacts, m_PreparationCallback, Dependency);
        m_StepPhysicsWorld.EnqueueCallback(SimulationCallbacks.Phase.PostCreateContactJacobians, m_JacobianModificationCallback, Dependency);
    }
}
