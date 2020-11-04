using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.GraphicsIntegration;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

// Camera Utility to smoothly track a specified target from a specified location
// Camera location and target are interpolated each frame to remove overly sharp transitions
public class CameraSmoothTrack : MonoBehaviour, IConvertGameObjectToEntity
{
#pragma warning disable 649
    public GameObject Target;
    public GameObject LookTo;
    [Range(0, 1)] public float LookToInterpolateFactor = 0.9f;

    public GameObject LookFrom;
    [Range(0, 1)] public float LookFromInterpolateFactor = 0.9f;
#pragma warning restore 649

    void OnValidate()
    {
        LookToInterpolateFactor = math.clamp(LookToInterpolateFactor, 0f, 1f);
        LookFromInterpolateFactor = math.clamp(LookFromInterpolateFactor, 0f, 1f);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new CameraSmoothTrackSettings
        {
            Target = conversionSystem.GetPrimaryEntity(Target),
            LookTo = conversionSystem.GetPrimaryEntity(LookTo),
            LookToInteroplateFactor = LookToInterpolateFactor,
            LookFrom = conversionSystem.GetPrimaryEntity(LookFrom),
            LookFromInterpolateFactor = LookFromInterpolateFactor
        });
    }
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

[UpdateAfter(typeof(TransformSystemGroup))]
class SmoothlyTrackCameraTarget : SystemBase
{
    struct Initialized : ISystemStateComponentData {}

    BuildPhysicsWorld m_BuildPhysicsWorld;
    RecordMostRecentFixedTime m_RecordMostRecentFixedTime;

    protected override void OnCreate()
    {
        base.OnCreate();
        m_BuildPhysicsWorld = World.GetExistingSystem<BuildPhysicsWorld>();
        m_RecordMostRecentFixedTime = World.GetExistingSystem<RecordMostRecentFixedTime>();
    }

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

        PhysicsWorld world = m_BuildPhysicsWorld.PhysicsWorld;

        var timeAhead = (float)(Time.ElapsedTime - m_RecordMostRecentFixedTime.MostRecentElapsedTime);

        Entities
            .WithName("SmoothlyTrackCameraTargetsJob")
            .WithoutBurst()
            .WithAll<Initialized>()
            .WithReadOnly(world)
            .ForEach((CameraSmoothTrack monoBehaviour, ref CameraSmoothTrackSettings cameraSmoothTrack, in LocalToWorld localToWorld) =>
            {
                var worldPosition = (float3)monoBehaviour.transform.position;

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

                monoBehaviour.transform.SetPositionAndRotation(newPositionFrom, newRotation);
                cameraSmoothTrack.OldPositionTo = newPositionTo;
            }).Run();
    }
}
