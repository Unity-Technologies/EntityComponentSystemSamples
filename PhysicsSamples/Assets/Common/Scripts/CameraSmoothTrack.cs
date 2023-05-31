using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.GraphicsIntegration;
using Unity.Transforms;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

namespace Common.Scripts
{
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
                    .WithAll<CharacterController.CameraTargetProxy>()
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
                var proxyComponent = _CameraProxyQuery.GetSingleton<CharacterController.CameraTargetProxy>();

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
                World.DefaultGameObjectInjectionWorld.EntityManager.RemoveComponent<CharacterController.CameraTargetProxy>(cameraEntity);
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
    [BurstCompile]
    partial class SmoothlyTrackCameraTarget : SystemBase
    {
        struct Initialized : ICleanupComponentData {}

        [BurstCompile]
        protected override void OnUpdate()
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            foreach (var(cameraSmoothTrack, localToWorld, entity)
                     in SystemAPI.Query<RefRW<CameraSmoothTrackSettings>, RefRO<LocalToWorld>>().WithEntityAccess().WithNone<Initialized>())
            {
                commandBuffer.AddComponent<Initialized>(entity);
                cameraSmoothTrack.ValueRW.OldPositionTo = SystemAPI.HasComponent<LocalToWorld>(cameraSmoothTrack.ValueRW.LookTo)
                    ? SystemAPI.GetComponent<LocalToWorld>(cameraSmoothTrack.ValueRW.LookTo).Position
                    : localToWorld.ValueRO.Position + new float3(0f, 0f, 1f);
            }
            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            PhysicsWorld world = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

            var mostRecentTime = SystemAPI.GetSingletonBuffer<MostRecentFixedTime>();
            var timeAhead = (float)(SystemAPI.Time.ElapsedTime - mostRecentTime[0].ElapsedTime);

            foreach (var(mainCamera, cameraSmoothTrack, localToWorld)
                     in SystemAPI.Query<MainCamera, RefRW<CameraSmoothTrackSettings>, RefRO<LocalToWorld>>().WithAll<Initialized>())
            {
                var worldPosition = (float3)mainCamera.Transform.position;

                float3 newPositionFrom = SystemAPI.HasComponent<LocalToWorld>(cameraSmoothTrack.ValueRW.LookFrom)
                    ? SystemAPI.GetComponent<LocalToWorld>(cameraSmoothTrack.ValueRW.LookFrom).Position
                    : worldPosition;

                float3 newPositionTo = SystemAPI.HasComponent<LocalToWorld>(cameraSmoothTrack.ValueRW.LookTo)
                    ? SystemAPI.GetComponent<LocalToWorld>(cameraSmoothTrack.ValueRW.LookTo).Position
                    : worldPosition + localToWorld.ValueRO.Forward;

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

                if (cameraSmoothTrack.ValueRW.Target != Entity.Null)
                {
                    // add velocity
                    float3 lv = world.GetLinearVelocity(world.GetRigidBodyIndex(cameraSmoothTrack.ValueRW.Target));
                    lv *= timeAhead;
                    newPositionFrom += lv;
                    newPositionTo += lv;
                }

                newPositionFrom = math.lerp(worldPosition, newPositionFrom, cameraSmoothTrack.ValueRW.LookFromInterpolateFactor);
                newPositionTo = math.lerp(cameraSmoothTrack.ValueRW.OldPositionTo, newPositionTo, cameraSmoothTrack.ValueRW.LookToInteroplateFactor);

                float3 newForward = newPositionTo - newPositionFrom;
                newForward = math.normalizesafe(newForward);
                quaternion newRotation = quaternion.LookRotation(newForward, math.up());

                mainCamera.Transform.SetPositionAndRotation(newPositionFrom, newRotation);
                cameraSmoothTrack.ValueRW.OldPositionTo = newPositionTo;
            }
        }
    }
}
