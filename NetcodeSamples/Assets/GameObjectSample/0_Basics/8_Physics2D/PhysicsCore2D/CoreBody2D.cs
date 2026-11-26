#if NETCODE_GAMEOBJECT_PHYSICS2D_EXPERIMENTAL && GAMEOBJECTSAMPLE_MODULE_PHYSICSCORE2D
using System.Collections.Generic;
using Unity.Entities;
using Unity.Netcode;
using Unity.U2D.Physics;
using UnityEngine;

namespace GameObjectSample.Physics2DSample
{
    // ISampleBody2D over a PhysicsCore2D body. PhysicsCore2D has no component, so this creates a box body when the
    // ghost spawns, in the netcode-managed world for the ghost's netcode world, and hands it to GhostPhysicsCore2DBody.
    // Netcode steps that world per prediction tick and keeps the body and the Transform in sync.
    [RequireComponent(typeof(GhostPhysicsCore2DBody))]
    public partial class CoreBody2D : GhostBehaviour, ISampleBody2D
    {
        [SerializeField] float m_Density = 1f;
        [SerializeField] float m_Friction = 0.6f;
        [SerializeField] bool m_FreezeRotation;
        [SerializeField, Tooltip("Reuse contact manifolds when the body barely moves. Reused manifolds carry solver " +
            "state the rollback can't restore, so re-predictions can resolve contacts differently than the server; " +
            "turn off to always rebuild contacts from the current poses. Must be set before the body spawns.")] 
        bool m_ContactRecycling = true;

        PhysicsBody m_Body;
        bool m_IsBox;

        // Velocities set before the deferred body exists (for example by the projectile launcher straight after
        // spawning); the body is created with them.
        Vector2 m_PendingLinearVelocity;
        float m_PendingAngularVelocity;

        struct PendingBody
        {
            public CoreBody2D Body;
            public int GhostId;
            public long Sequence;
        }

        // The solver iterates constraints in body-creation order, so a client creating bodies in snapshot-arrival
        // order solves in a different order than the server and multi-contact impulses come out differently
        // (PhysicsCore2D has no way to dictate ids or solve order).
        // Spawned bodies are therefore queued and created once per tick sorted by ghost id - ids are allocated in
        // server spawn order, so both worlds hand the solver the same ordering. Limits: predicted spawns (ghost id
        // 0) keep their local order until classified, and the first despawn desynchronizes Box2D's id freelist, so
        // this holds exactly only while the body set is stable.
        // This is a workaround for PhysicsCore2D not letting callers control solver order.
        // TODO: remove once solver ordering is investigated, either with an engine API for a caller-supplied solve order
        // or by moving ordered body creation into the package.
        static readonly List<PendingBody> s_PendingBodies = new List<PendingBody>();
        static long s_NextPendingSequence;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_PendingBodies.Clear();
            s_NextPendingSequence = 0;
        }

        public override void OnGhostSpawn()
        {
            // Not in Awake: GhostObject spawns from its own Awake, which runs before this one.
            m_IsBox = GetComponent<PlayerController2D>() == null;

            var ghostId = Ghost.GhostInfo.ghostId;
            s_PendingBodies.Add(new PendingBody { Body = this, GhostId = ghostId, Sequence = s_NextPendingSequence++ });

            if (m_IsBox)
                Physics2DSample.Boxes.Add(this);
        }

        // Called from DeferredBody2DBuildSystem once per tick per world, before the physics step; see s_PendingBodies.
        internal static void BuildPendingBodies(World world)
        {
            if (s_PendingBodies.Count == 0)
                return;
            s_PendingBodies.Sort((a, b) => a.GhostId != b.GhostId ? a.GhostId.CompareTo(b.GhostId) : a.Sequence.CompareTo(b.Sequence));
            foreach (var pending in s_PendingBodies)
            {
                if (pending.Body != null && pending.Body.Ghost != null && pending.Body.Ghost.IsSpawned &&
                    pending.Body.Ghost.World == world)
                {
                    pending.Body.CreateDeferredBody();
                }
            }
            s_PendingBodies.RemoveAll(pending => pending.Body == null || pending.Body.Ghost == null ||
                !pending.Body.Ghost.IsSpawned || pending.Body.m_Body.isValid);
        }

        void CreateDeferredBody()
        {
            var physicsWorld = GhostPhysicsCore2DWorlds.GetOrCreateWorld(Ghost.World);
            transform.GetPositionAndRotation(out var position, out var rotation);
            var bodyDefinition = PhysicsBodyDefinition.defaultDefinition;
            bodyDefinition.type = PhysicsBody.BodyType.Dynamic;
            bodyDefinition.position = position;
            bodyDefinition.rotation = PhysicsRotate.FromRadians(PhysicsMath.ToRotation2D(rotation));
            if (m_FreezeRotation)
                bodyDefinition.constraints = PhysicsBody.BodyConstraints.Rotation;
            bodyDefinition.contactRecyclingAllowed = m_ContactRecycling;
            bodyDefinition.linearVelocity = m_PendingLinearVelocity;
            bodyDefinition.angularVelocity = m_PendingAngularVelocity;
            m_Body = physicsWorld.CreateBody(bodyDefinition);

            var shapeDefinition = PhysicsShapeDefinition.defaultDefinition;
            shapeDefinition.density = m_Density;
            var material = shapeDefinition.surfaceMaterial;
            material.friction = m_Friction;
            shapeDefinition.surfaceMaterial = material;
            var scale = transform.lossyScale;
            m_Body.CreateShape(PolygonGeometry.CreateBox(new Vector2(scale.x, scale.y)), shapeDefinition);

            GetComponent<GhostPhysicsCore2DBody>().AttachBody(m_Body);
        }

        public override void OnGhostDespawn() => Release();

        public override void OnDestroy()
        {
            Release();
            base.OnDestroy();
        }

        void Release()
        {
            Physics2DSample.Boxes.Remove(this);
            if (m_Body.isValid)
            {
                GetComponent<GhostPhysicsCore2DBody>().DetachBody();
                m_Body.Destroy();
            }
            m_Body = default;
        }

        // Netcode makes interpolated ghosts kinematic on the client, so the dynamic checks make the writes no-ops there.
        public Vector2 LinearVelocity
        {
            get => m_Body.isValid ? m_Body.linearVelocity : m_PendingLinearVelocity;
            set
            {
                if (!m_Body.isValid)
                    m_PendingLinearVelocity = value;
                else if (m_Body.type == PhysicsBody.BodyType.Dynamic)
                    m_Body.linearVelocity = value;
            }
        }

        public float AngularVelocity
        {
            get => m_Body.isValid ? m_Body.angularVelocity : m_PendingAngularVelocity;
            set
            {
                if (!m_Body.isValid)
                    m_PendingAngularVelocity = value;
                else if (m_Body.type == PhysicsBody.BodyType.Dynamic)
                    m_Body.angularVelocity = value;
            }
        }

        public void AddImpulse(Vector2 impulse)
        {
            if (m_Body.isValid && m_Body.type == PhysicsBody.BodyType.Dynamic)
                m_Body.ApplyLinearImpulseToCenter(impulse);
        }
    }

    // Creates the queued ghost bodies in ghost-id order before each physics step; see CoreBody2D.s_PendingBodies.
    // A ghost therefore exists without a body until the next tick; every body accessor already guards isValid.
    [UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    partial class DeferredBody2DBuildSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!Physics2DSample.IsActive)
                return;
            CoreBody2D.BuildPendingBodies(World);
        }
    }
}
#endif
