#if NETCODE_GAMEOBJECT_PHYSICS2D_EXPERIMENTAL
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace GameObjectSample.Physics2DSample
{
    public struct Physics2DPlayerInput : IInputComponentData
    {
        public float Horizontal;
        public InputEvent Jump;

        // Only read when Shoot is set: where the shot spawns and its initial velocity. They travel with the input so
        // a client in another process can shoot too.
        public InputEvent Shoot;
        public float2 ShootOrigin;
        public float2 ShootVelocity;
    }

    // Moves the player's body by setting its velocity, never its position, so the physics step resolves contacts.
    // Needs one ISampleBody2D on the same GameObject.
    public partial class PlayerController2D : GhostBehaviour
    {
        // Drags shorter than this, in world units, cancel the shot instead of firing it.
        const float k_MinShotDrag = 0.25f;

        // Launch speed, in meters per second, per world unit of aim arrow.
        const float k_ShotSpeedPerArrowLength = 10f;

        [SerializeField] float m_MoveSpeed = 6f;
        [SerializeField] float m_JumpSpeed = 8f;

        GhostInput<Physics2DPlayerInput> m_Input;

        // The last shot, sent with every input. Shoot is an InputEvent and survives until its tick is sent, but
        // plain fields are overwritten by each GatherInput, which can run several times in one tick.
        float2 m_ShootOrigin;
        float2 m_ShootVelocity;

        // Drag shot: pressing sets the arrow's tip, dragging back sets its tail, and releasing spawns the shot at the
        // tail and fires it along the arrow, at k_ShotSpeedPerArrowLength times the arrow's length up to
        // ProjectileLauncher2D.MaxShotSpeed. The arrow always reaches the cursor; the part beyond the cap is faded.
        bool m_IsAiming;
        Vector2 m_AimStart;
        ShotAimArrow2D m_AimArrow;

        ISampleBody2D m_Body;

        public ISampleBody2D Body => m_Body;

        public override void OnGhostSpawn()
        {
            // Not in Awake: GhostObject spawns from its own Awake, which runs before this one.
            m_Body = GetComponent<ISampleBody2D>();
            Physics2DSample.Players.Add(this);

            // The server sets the owner after Instantiate, so PlayerSpawner2D names its copies.
            if (IsClient)
                name = IsOwnedLocally ? "Player (local)" : $"Player {Ghost.OwnerNetworkId.Value} (remote)";
        }

        public override void OnGhostDespawn() => Physics2DSample.Players.Remove(this);

        public override void OnDestroy()
        {
            Physics2DSample.Players.Remove(this);
            if (m_AimArrow != null)
                Destroy(m_AimArrow.gameObject);
            base.OnDestroy();
        }

        // TODO: replace with the package API once GhostBehaviour exposes local ownership (see TODO-next@inputImprovements in GhostBehaviour.cs).
        public bool IsOwnedLocally
        {
            get
            {
                if (!IsClient || !Ghost.HasOwner)
                    return false;
                var local = Ghost.World.LocalConnection;
                return local.IsValid() && Ghost.OwnerNetworkId.Value == local.NetworkId.Value;
            }
        }

        public override void PredictionGatherInput(PredictionGatherInputContext context)
        {
            var input = m_Input.PrepareToCapture();
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                input.Horizontal -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                input.Horizontal += 1f;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                input.Jump.Set();
            if (UpdateDragShot())
                input.Shoot.Set();
            input.ShootOrigin = m_ShootOrigin;
            input.ShootVelocity = m_ShootVelocity;
            m_Input.Capture(input);
        }

        // Returns true on the frame a shot is released.
        bool UpdateDragShot()
        {
            if (!TryGetMouseWorldPoint(out var mouse))
                return false;

            if (Input.GetMouseButtonDown(0) && !Physics2DSampleControlPanel.IsPointerOverPanel)
            {
                m_IsAiming = true;
                m_AimStart = mouse;
            }
            if (!m_IsAiming)
                return false;

            var arrow = m_AimStart - mouse;
            if (Input.GetMouseButton(0))
            {
                if (m_AimArrow == null)
                    m_AimArrow = ShotAimArrow2D.Create();
                m_AimArrow.Show(mouse, m_AimStart, ProjectileLauncher2D.MaxShotSpeed / k_ShotSpeedPerArrowLength);
                return false;
            }

            m_IsAiming = false;
            if (m_AimArrow != null)
                m_AimArrow.Hide();
            if (arrow.magnitude < k_MinShotDrag)
                return false;
            var velocity = Vector2.ClampMagnitude(arrow * k_ShotSpeedPerArrowLength, ProjectileLauncher2D.MaxShotSpeed);
            m_ShootOrigin = new float2(mouse.x, mouse.y);
            m_ShootVelocity = new float2(velocity.x, velocity.y);
            return true;
        }

        static bool TryGetMouseWorldPoint(out Vector2 point)
        {
            point = default;
            var camera = Camera.main;
            if (camera == null)
                return false;
            var mouse = Input.mousePosition;
            point = camera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, -camera.transform.position.z));
            return true;
        }

        // PredictionPhysicsUpdate, not PredictionUpdate: it runs before netcode's per-tick physics step, so the
        // velocity set here is simulated in the same tick.
        public override void PredictionPhysicsUpdate(PredictionPhysicsUpdateContext context)
        {
            if (m_Body == null) 
                return;
            var input = m_Input.GetForCurrentTick();
            var velocity = m_Body.LinearVelocity;
            velocity.x = input.Horizontal * m_MoveSpeed;

            // Crude grounded check, good enough for a sample: only jump when not already moving vertically.
            if (input.Jump.IsSet && Mathf.Abs(velocity.y) < 0.05f)
                velocity.y = m_JumpSpeed;

            m_Body.LinearVelocity = velocity;

            // The server has no rollback, so this runs once per shot.
            if (input.Shoot.IsSet && IsServer && ProjectileLauncher2D.Instance != null)
                ProjectileLauncher2D.Instance.Launch(input.ShootOrigin, input.ShootVelocity);
        }
    }
}
#endif
