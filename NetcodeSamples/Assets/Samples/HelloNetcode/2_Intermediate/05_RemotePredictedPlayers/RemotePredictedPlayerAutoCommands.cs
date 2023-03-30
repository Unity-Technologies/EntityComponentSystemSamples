using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode.Samples.Common;
using Unity.Transforms;
using Unity.NetCode;

namespace Samples.HelloNetcode
{
    // Sample keypress inputs every frame and add them to the input component for
    // processing later.
    [UpdateInGroup(typeof(HelloNetcodeInputSystemGroup))]
    [AlwaysSynchronizeSystem]
    public partial class RemoteGatherAutoCommandsSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<EnableRemotePredictedPlayer>();
            RequireForUpdate<RemotePredictedPlayerInput>();
            RequireForUpdate<NetworkStreamInGame>();
        }

        protected override void OnUpdate()
        {
            bool left = UnityEngine.Input.GetKey("left") || TouchInput.GetKey(TouchInput.KeyCode.Left);
            bool right = UnityEngine.Input.GetKey("right") || TouchInput.GetKey(TouchInput.KeyCode.Right);
            bool down = UnityEngine.Input.GetKey("down") || TouchInput.GetKey(TouchInput.KeyCode.Down);
            bool up = UnityEngine.Input.GetKey("up") || TouchInput.GetKey(TouchInput.KeyCode.Up);
            bool jump = UnityEngine.Input.GetKeyDown("space");

            // When multiple players are spawned this input gathering step could all
            // of them since they have the PlayerInput, so this restricts the query to
            // only players with a ghost owner component and with an ID which matches the
            // local connection. For that we are using the GhostOwnerIsLocal tag, that is added
            // automatically to all entities owned by the users.
            Entities
                .WithName("GatherInput")
                .WithAll<GhostOwnerIsLocal>()
                .ForEach((ref RemotePredictedPlayerInput inputData) =>
                {
                    inputData = default;

                    if (jump)
                        inputData.Jump.Set();
                    if (left)
                        inputData.Horizontal -= 1;
                    if (right)
                        inputData.Horizontal += 1;
                    if (down)
                        inputData.Vertical -= 1;
                    if (up)
                        inputData.Vertical += 1;
                    // if (jump || left || right || down || up)
                    //     UnityEngine.Debug.Log($"{tick} GatherInput jump={jump} left={left} right={right} down={down} up={up}");
                }).ScheduleParallel();
        }
    }

    // Apply the inputs stored in the input component for all player entities
    [UpdateInGroup(typeof(HelloNetcodePredictedSystemGroup))]
    public partial class RemoteProcessAutoCommandsSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<EnableRemotePredictedPlayer>();
            RequireForUpdate<RemotePredictedPlayerInput>();
        }
        protected override void OnUpdate()
        {
            var movementSpeed = SystemAPI.Time.DeltaTime * 3;
            SystemAPI.TryGetSingleton<ClientServerTickRate>(out var tickRate);
            tickRate.ResolveDefaults();
            // Make the jump arc look the same regardless of simulation tick rate
            var velocityDecrementStep = 60 / tickRate.SimulationTickRate;
            var tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
            FixedString32Bytes world = World.Name;
            Entities.WithAll<Simulate>().WithName("ProcessInputForTick").ForEach(

                (ref RemotePredictedPlayerInput input, ref LocalTransform trans, ref PlayerMovement movement) =>

                {
                    if (input.Jump.IsSet)
                        movement.JumpVelocity = 10;

                    // Simple jump mechanism, when jump event is set the jump velocity is set to 10
                    // then on each tick it is decremented. It results in an input value being set either
                    // in the upward or downward direction (just like left/right movement).
                    var verticalMovement = 0f;
                    if (movement.JumpVelocity > 0)
                    {
                        movement.JumpVelocity -= velocityDecrementStep;
                        verticalMovement = 1;
                    }
                    else
                    {

                        if (trans.Position.y > 0)
                            verticalMovement = -1;

                    }
                    var moveInput = new float3(input.Horizontal, verticalMovement, input.Vertical);
                    moveInput = math.normalizesafe(moveInput) * movementSpeed;
                    // Ensure we don't go through the ground when landing (and stick to it when close)

                    if (movement.JumpVelocity <= 0 && (trans.Position.y + moveInput.y < 0 || trans.Position.y + moveInput.y < 0.05))
                        moveInput.y = trans.Position.y = 0;
                    trans.Position += new float3(moveInput.x, moveInput.y, moveInput.z);
                    if (trans.Position.y != 0)
                        UnityEngine.Debug.Log($"[{world}][{tick}] ({trans.Position.x}, {trans.Position.y}, {trans.Position.z}) velocity={movement.JumpVelocity}");

                }).ScheduleParallel();
        }
    }
}
