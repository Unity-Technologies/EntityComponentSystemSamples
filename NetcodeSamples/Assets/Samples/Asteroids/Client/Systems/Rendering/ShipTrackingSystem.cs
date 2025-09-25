using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Burst;
using Unity.NetCode;

namespace Asteroids.Client
{
    [RequireMatchingQueriesForUpdate]
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateBefore(typeof(ParticleEmitterSystem))]
    [BurstCompile]
    public partial struct ShipThrustParticleSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new ShipThrustParticle();
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }
        [BurstCompile]
        partial struct ShipThrustParticle : IJobEntity
        {
            public void Execute(ref ParticleEmitterComponentData emitter, in ShipStateComponentData state)
            {
                emitter.active = state.State;
            }
        }
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class ShipTrackingSystem : SystemBase
    {
        EntityQuery m_LevelGroup;
        NativeArray<int> m_Teleport;
        NativeArray<float2> m_RenderOffset;

        protected override void OnCreate()
        {
            m_Teleport = new NativeArray<int>(1, Allocator.Persistent);
            m_Teleport[0] = 1;
            m_RenderOffset = new NativeArray<float2>(2, Allocator.Persistent);
            m_LevelGroup = GetEntityQuery(ComponentType.ReadWrite<LevelComponent>());
            RequireForUpdate(m_LevelGroup);
        }

        protected override void OnDestroy()
        {
            m_Teleport.Dispose();
            m_RenderOffset.Dispose();
        }

        partial struct ShipTrackingJob : IJobEntity
        {
            public float deltaTime;
            public int screenHeight;
            public NativeList<LevelComponent> level;
            public NativeArray<int> teleport;
            public NativeArray<float2> renderOffset;
            [ReadOnly] public ComponentLookup<LocalTransform> shipTransform;
            public int screenWidthHalf;
            public int screenHeightHalf;
            public Entity localPlayerShip;

            public void Execute()
            {
                const float mapEdgePaddingPercent = .2f;
                float mapEdgeCameraPadding = screenHeight * mapEdgePaddingPercent;
                int mapWidth = level[0].levelHeight;
                int mapHeight = level[0].levelHeight;
                int nextTeleport = 1;

                if (shipTransform.HasComponent(localPlayerShip))
                {
                    float3 desiredCamPos = shipTransform[localPlayerShip].Position;

                    desiredCamPos.x = math.clamp(desiredCamPos.x, -mapEdgeCameraPadding + screenWidthHalf, mapWidth + mapEdgeCameraPadding - screenWidthHalf);
                    desiredCamPos.y = math.clamp(desiredCamPos.y, -mapEdgeCameraPadding + screenHeightHalf, mapHeight + mapEdgeCameraPadding - screenHeightHalf);

                    desiredCamPos.x -= screenWidthHalf;
                    desiredCamPos.y -= screenHeightHalf;

                    renderOffset[1] = desiredCamPos.xy;
                    nextTeleport = 0;
                }

                var offset = renderOffset[0];
                var target = renderOffset[1];
                float maxPxPerSec = 500;
                if (math.any(offset != target))
                {
                    if (teleport[0] != 0)
                        offset = target;
                    else
                    {
                        float2 delta = (target - offset);
                        float deltaLen = math.length(delta);
                        float maxDiff = maxPxPerSec * deltaTime;
                        if (deltaLen > maxDiff || deltaLen < -maxDiff)
                            delta *= maxDiff / deltaLen;
                        offset += delta;
                    }

                    renderOffset[0] = offset;
                }

                teleport[0] = nextTeleport;
            }
        }


        override protected void OnUpdate()
        {
            JobHandle levelHandle;
            Entity localPlayerShip = Entity.Null;
            foreach (var (_, entity) in SystemAPI.Query<GhostOwnerIsLocal>().WithAll<ShipCommandData>().WithEntityAccess())
            {
                if (localPlayerShip != Entity.Null) throw new Exception("Sanity check failed!");
                localPlayerShip = entity;
            }
            // SystemAPI.TryGetSingletonEntity<ShipCommandData>(out var localPlayerShip); // can't use this with single world host

            var shipTransform = GetComponentLookup<LocalTransform>(true);

            var screenHeight = Screen.height;
            var screenWidthHalf = Screen.width/2;
            var screenHeightHalf = Screen.height/2;

            var level = m_LevelGroup.ToComponentDataListAsync<LevelComponent>(World.UpdateAllocator.ToAllocator,
                out levelHandle);
            var teleport = m_Teleport;

            var renderOffset = m_RenderOffset;
            var curOffset = renderOffset[0];
            var camera = Camera.main;
            camera.orthographicSize = screenHeightHalf;
            camera.transform.position = new Vector3(curOffset.x + screenWidthHalf, curOffset.y + screenHeightHalf, -0.5f);

            var deltaTime = SystemAPI.Time.DeltaTime;

            var trackJob = new ShipTrackingJob()
            {
                deltaTime = deltaTime,
                level = level,
                localPlayerShip = localPlayerShip,
                renderOffset = renderOffset,
                screenHeight = screenHeight,
                screenHeightHalf = screenHeightHalf,
                screenWidthHalf = screenWidthHalf,
                shipTransform = shipTransform,
                teleport = teleport,
            }.Schedule(JobHandle.CombineDependencies(Dependency, levelHandle));

            // The one frame latency for updating hte camera position can cause stutter, so do a sync update of the offset of now
            trackJob.Complete();
            curOffset = renderOffset[0];
            camera.transform.position = new Vector3(curOffset.x + screenWidthHalf, curOffset.y + screenHeightHalf, -0.5f);
        }
    }
}
