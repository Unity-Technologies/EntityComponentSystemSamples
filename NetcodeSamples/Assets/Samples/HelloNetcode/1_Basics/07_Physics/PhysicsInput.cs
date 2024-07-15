using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.NetCode;
using Unity.NetCode.Samples.Common;

namespace Samples.HelloNetcode
{
    [UpdateInGroup(typeof(HelloNetcodeInputSystemGroup))]
    public partial class SamplePhysicsInputSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<EnablePhysics>();
            RequireForUpdate<PhysicsPlayerInput>();
            RequireForUpdate<NetworkId>();
        }

        protected override void OnUpdate()
        {
            if (!SystemAPI.TryGetSingletonEntity<PhysicsPlayerInput>(out var localInputEntity))
                return;

            var input = default(PhysicsPlayerInput);

            // Note the tick the client is currently simulating, this is attached to the
            // command and is sent to the server where it is taken into account when the
            // command is deployed.
            input.Tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;

            if (UnityEngine.Input.GetKey("left") || TouchInput.GetKey(TouchInput.KeyCode.Left))
                input.Horizontal -= 1;
            if (UnityEngine.Input.GetKey("right") || TouchInput.GetKey(TouchInput.KeyCode.Right))
                input.Horizontal += 1;
            if (UnityEngine.Input.GetKey("down") || TouchInput.GetKey(TouchInput.KeyCode.Down))
                input.Vertical -= 1;
            if (UnityEngine.Input.GetKey("up") || TouchInput.GetKey(TouchInput.KeyCode.Up))
                input.Vertical += 1;

            // Commands need to be sent every frame they are sampled even if there is no keypress which needs
            // to be sent to the server (all values are 0). The commands do get ghost snapshot information
            // embedded into them, which is why they can't be skipped when there is no input present to send.
            var inputBuffer = EntityManager.GetBuffer<PhysicsPlayerInput>(localInputEntity);
            inputBuffer.AddCommandData(input);
        }
    }

    // The input processing but run in the PredictedPhysicsSystemGroup instead of the
    // PredictionSystemGroup like usually. This ensure the simulation is correctly
    // built and stepped for each tick as the prediction runs.
    [UpdateInGroup(typeof(PhysicsSystemGroup))]
    [UpdateBefore(typeof(PhysicsInitializeGroup))]
    public partial class PhysicsInputSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<EnablePhysics>();
            RequireForUpdate<PhysicsPlayerInput>();
        }

        protected override void OnUpdate()
        {
            var tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;

            // How fast the physics entity is allowed to move affects how it looks when it
            // collides with other physical entities. Dependent also on the physics step
            // framerate frequency.
            float speed = 3f;
            Entities.WithAll<Simulate>().ForEach((DynamicBuffer<PhysicsPlayerInput> inputBuffer, ref PhysicsVelocity vel) =>
            {
                inputBuffer.GetDataAtTick(tick, out var input);
                float3 dir = default;
                if (input.Horizontal > 0)
                    dir.x += 1;
                if (input.Horizontal < 0)
                    dir.x -= 1;
                if (input.Vertical > 0)
                    dir.z += 1;
                if (input.Vertical < 0)
                    dir.z -= 1;
                if (math.lengthsq(dir) > 0.5)
                {
                    dir = math.normalize(dir);
                    dir *= speed;
                }

                vel.Linear.x = dir.x;
                vel.Linear.z = dir.z;
            }).ScheduleParallel();
        }
    }
}
