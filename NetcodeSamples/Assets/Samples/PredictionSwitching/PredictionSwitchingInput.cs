using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.NetCode.Samples.Common;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

[GhostComponent(OwnerSendType = SendToOwnerType.SendToNonOwner)]
public struct PredictionSwitchingInput : ICommandData
{
    [GhostField] public NetworkTick Tick{get; set;}
    [GhostField] public int horizontal;
    [GhostField] public int vertical;

    public FixedString512Bytes ToFixedString() => $"h:{horizontal},v:{vertical}";
}

[UpdateInGroup(typeof(GhostInputSystemGroup))]
public partial class PredictionSwitchingSampleInputSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<PredictionSwitchingSettings>();
        RequireForUpdate<NetworkStreamInGame>();
    }

    protected override void OnUpdate()
    {
        var tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
        var connection = SystemAPI.GetSingletonEntity<CommandTarget>();
        Entities
            .WithoutBurst()
            .WithAll<GhostOwnerIsLocal>()
            .ForEach((Entity entity, DynamicBuffer<PredictionSwitchingInput> inputBuffer) => {
                var input = default(PredictionSwitchingInput);
                input.Tick = tick;
                if (UnityEngine.Input.GetKey("left") || TouchInput.GetKey(TouchInput.KeyCode.Left))
                    input.horizontal -= 1;
                if (UnityEngine.Input.GetKey("right") || TouchInput.GetKey(TouchInput.KeyCode.Right))
                    input.horizontal += 1;
                if (UnityEngine.Input.GetKey("down") || TouchInput.GetKey(TouchInput.KeyCode.Down))
                    input.vertical -= 1;
                if (UnityEngine.Input.GetKey("up") || TouchInput.GetKey(TouchInput.KeyCode.Up))
                    input.vertical += 1;
                inputBuffer.AddCommandData(input);
                if (EntityManager.GetComponentData<CommandTarget>(connection).targetEntity == Entity.Null)
                {
                    EntityManager.SetComponentData(connection, new CommandTarget{targetEntity = entity});
                }
        }).Run();
    }
}

[UpdateInGroup(typeof(GhostInputSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ThinClientSimulation)]
public partial class PredictionSwitchingThinInputSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<NetworkStreamInGame>();
        RequireForUpdate<PredictionSwitchingSettings>();
    }

    protected override void OnUpdate()
    {
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        if (!networkTime.ServerTick.IsValid)
            return;

        var inputTargetTick = networkTime.ServerTick;
        if (!SystemAPI.TryGetSingletonBuffer<PredictionSwitchingInput>(out var input))
        {
            if (SystemAPI.TryGetSingletonRW<CommandTarget>(out var commandTarget))
            {
                if (commandTarget.ValueRO.targetEntity == Entity.Null)
                    commandTarget.ValueRW.targetEntity = EntityManager.CreateEntity(ComponentType.ReadOnly<PredictionSwitchingInput>(), ComponentType.ReadOnly<CommandTarget>());
                input = EntityManager.AddBuffer<PredictionSwitchingInput>(commandTarget.ValueRW.targetEntity); // Returns GetBuffer if the buffer already exists.
            }
        }

        var horizontal = (int) (networkTime.ServerTick.TickIndexForValidTick % 400);
        if (horizontal < 100)
            horizontal = 0;
        else if (horizontal < 200)
            horizontal = -1;
        else if (horizontal < 300)
            horizontal = 0;
        else horizontal = 1;
        input.AddCommandData(new PredictionSwitchingInput {Tick = inputTargetTick, horizontal = horizontal, vertical = 0});
    }
}

[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateBefore(typeof(PhysicsInitializeGroup))]
public partial class PredictionSwitchingApplyInputSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<PredictionSwitchingSettings>();
        RequireForUpdate<NetworkTime>();
    }

    protected override void OnUpdate()
    {
        var tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
        var speed = SystemAPI.GetSingleton<PredictionSwitchingSettings>().PlayerSpeed;

        Entities
            .WithAll<Simulate>()
            .ForEach((DynamicBuffer<PredictionSwitchingInput> inputBuffer, ref PhysicsVelocity vel) => {
            inputBuffer.GetDataAtTick(tick, out var input);
            float3 dir = default;
            if (input.horizontal > 0)
                dir.x += 1;
            if (input.horizontal < 0)
                dir.x -= 1;
            if (input.vertical > 0)
                dir.z += 1;
            if (input.vertical < 0)
                dir.z -= 1;
            if (math.lengthsq(dir) > 0.5)
            {
                dir = math.normalize(dir);
                dir *= speed;
            }
            vel.Linear.x = dir.x;
            vel.Linear.z = dir.z;
        }).Schedule();
    }
}

[UpdateAfter(typeof(TransformSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial class PredictionSwitchingCameraFollowSystem : SystemBase
{
    Transform m_CameraTransform;
    float3 m_CameraOffset;

    protected override void OnCreate()
    {
        RequireForUpdate<PredictionSwitchingSettings>();
        RequireForUpdate<NetworkStreamInGame>();
    }

    protected override void OnUpdate()
    {
        if (!m_CameraTransform)
        {
            var camera = Camera.main;
            if (camera)
            {
                m_CameraTransform = camera.transform;
                m_CameraOffset = m_CameraTransform.position;
            }
            else return;
        }

        Entities
            .WithoutBurst()
            .WithAll<GhostOwnerIsLocal>()
            .WithAll<PredictionSwitchingInput>()
            .ForEach((ref LocalToWorld trans) => {
            m_CameraTransform.transform.position = trans.Position + m_CameraOffset;
        }).Run();
    }
}
