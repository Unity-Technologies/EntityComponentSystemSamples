using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.GraphicsIntegration;
using Unity.Transforms;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

// Camera Utility to smoothly track a specified target from a specified location
// Camera location and target are interpolated each frame to remove overly sharp transitions
public class CameraSmoothTrack : MonoBehaviour
{
#pragma warning disable 649
    public GameObject Target;
    public GameObject LookTo;
    [Range(0, 1)] public float LookToInterpolateFactor = 0.9f;

    public GameObject LookFrom;
    [Range(0, 1)] public float LookFromInterpolateFactor = 0.9f;
#pragma warning restore 649

    EntityQuery _CameraProxyQuery;

    void OnValidate()
    {
        LookToInterpolateFactor = math.clamp(LookToInterpolateFactor, 0f, 1f);
        LookFromInterpolateFactor = math.clamp(LookFromInterpolateFactor, 0f, 1f);
    }

    void Start()
    {
        _CameraProxyQuery = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(
            new EntityQueryBuilder(Allocator.Temp)
                .WithAll<CameraTargetProxy>()
                .WithNone<MainCamera, CameraSmoothTrackSettings>());
    }

    void OnDestroy()
    {
        if (World.DefaultGameObjectInjectionWorld?.IsCreated == true &&
            World.DefaultGameObjectInjectionWorld.EntityManager.IsQueryValid(_CameraProxyQuery))
            _CameraProxyQuery.Dispose();
    }

    void Update()
    {
        if (World.DefaultGameObjectInjectionWorld?.IsCreated == true &&
            World.DefaultGameObjectInjectionWorld.EntityManager.IsQueryValid(_CameraProxyQuery) &&
            !_CameraProxyQuery.IsEmpty)
        {
            var cameraEntity = _CameraProxyQuery.GetSingletonEntity();
            var proxyComponent = _CameraProxyQuery.GetSingleton<CameraTargetProxy>();

            World.DefaultGameObjectInjectionWorld.EntityManager.AddComponentData(cameraEntity, new MainCamera()
            {
                Transform = transform
            });
            World.DefaultGameObjectInjectionWorld.EntityManager.AddComponentData(cameraEntity, new CameraSmoothTrackSettings
            {
                Target = proxyComponent.Target,
                LookTo = proxyComponent.LookTo,
                LookToInteroplateFactor = LookToInterpolateFactor,
                LookFrom = proxyComponent.LookFrom,
                LookFromInterpolateFactor = LookFromInterpolateFactor
            });
            World.DefaultGameObjectInjectionWorld.EntityManager.RemoveComponent<CameraTargetProxy>(cameraEntity);
        }
    }
}

public class MainCamera : IComponentData
{
    public Transform Transform;
}

struct CameraSmoothTrackSettings : IComponentData
{
    public Entity Target;
    public Entity LookTo;
    public float LookToInteroplateFactor;
    public Entity LookFrom;
    public float LookFromInterpolateFactor;
    public float3 OldPositionTo;
}

[RequireMatchingQueriesForUpdate]
[UpdateAfter(typeof(TransformSystemGroup))]
partial class SmoothlyTrackCameraTarget : SystemBase
{
    struct Initialized : ICleanupComponentData {}

    protected override void OnUpdate()
    {
        var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
        Entities
            .WithName("InitializeCameraOldPositionsJob")
            .WithBurst()
            .WithNone<Initialized>()
            .ForEach((Entity entity, ref CameraSmoothTrackSettings cameraSmoothTrack, in LocalToWorld localToWorld) =>
            {
                commandBuffer.AddComponent<Initialized>(entity);
                cameraSmoothTrack.OldPositionTo = HasComponent<LocalToWorld>(cameraSmoothTrack.LookTo)
                    ? GetComponent<LocalToWorld>(cameraSmoothTrack.LookTo).Position
                    : localToWorld.Position + new float3(0f, 0f, 1f);
            }).Run();
        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();

        PhysicsWorld world = GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

        var mostRecentTime = GetBuffer<MostRecentFixedTime>(GetSingletonEntity<MostRecentFixedTime>());
        var timeAhead = (float)(SystemAPI.Time.ElapsedTime - mostRecentTime[0].ElapsedTime);

        Entities
            .WithName("SmoothlyTrackCameraTargetsJob")
            .WithoutBurst()
            .WithAll<Initialized>()
            .WithReadOnly(world)
            .ForEach((MainCamera mainCamera, ref CameraSmoothTrackSettings cameraSmoothTrack, in LocalToWorld localToWorld) =>
            {
                var worldPosition = (float3)mainCamera.Transform.position;

                float3 newPositionFrom = HasComponent<LocalToWorld>(cameraSmoothTrack.LookFrom)
                    ? GetComponent<LocalToWorld>(cameraSmoothTrack.LookFrom).Position
                    : worldPosition;

                float3 newPositionTo = HasComponent<LocalToWorld>(cameraSmoothTrack.LookTo)
                    ? GetComponent<LocalToWorld>(cameraSmoothTrack.LookTo).Position
                    : worldPosition + localToWorld.Forward;

                // check barrier
                var rayInput = new RaycastInput
                {
                    Start = newPositionFrom,
                    End = newPositionTo,
                    Filter = CollisionFilter.Default
                };

                if (world.CastRay(rayInput, out RaycastHit rayResult))
                {
                    newPositionFrom = rayResult.Position;
                }

                if (cameraSmoothTrack.Target != Entity.Null)
                {
                    // add velocity
                    float3 lv = world.GetLinearVelocity(world.GetRigidBodyIndex(cameraSmoothTrack.Target));
                    lv *= timeAhead;
                    newPositionFrom += lv;
                    newPositionTo += lv;
                }

                newPositionFrom = math.lerp(worldPosition, newPositionFrom, cameraSmoothTrack.LookFromInterpolateFactor);
                newPositionTo = math.lerp(cameraSmoothTrack.OldPositionTo, newPositionTo, cameraSmoothTrack.LookToInteroplateFactor);

                float3 newForward = newPositionTo - newPositionFrom;
                newForward = math.normalizesafe(newForward);
                quaternion newRotation = quaternion.LookRotation(newForward, math.up());

                mainCamera.Transform.SetPositionAndRotation(newPositionFrom, newRotation);
                cameraSmoothTrack.OldPositionTo = newPositionTo;
            }).Run();
    }
}
