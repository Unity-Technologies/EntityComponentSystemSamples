using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;

namespace Samples.HelloNetcode
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(CharacterControllerCameraSystem))]
    public partial class HitMarkSystem : SystemBase
    {
        struct HitInformation
        {
            public float Age;
            public GameObject Go;
            public Entity Entity;
            public float3 HitPosition;
        }

        GameObject m_ServerHitPrefab;
        Canvas m_Canvas;

        List<HitInformation> m_HitInformations = new List<HitInformation>();
        RectTransform m_CanvasRect;
        GameObject m_ClientHitPrefab;

        protected override void OnCreate()
        {
            RequireForUpdate<EnableHitScanWeapons>();
        }
        bool FindGameObjects()
        {
            m_Canvas = Object.FindFirstObjectByType<Canvas>();
            if (m_Canvas == null)
                return false;
            m_CanvasRect = m_Canvas.GetComponent<RectTransform>();
            var hitMarkSpawner = Object.FindFirstObjectByType<HitMarkSpawner>();
            if (hitMarkSpawner == null)
                return false;
            m_ServerHitPrefab = hitMarkSpawner.ServerMarkPrefab;
            m_ClientHitPrefab = hitMarkSpawner.ClientMarkPrefab;
            return true;
        }

        protected override void OnUpdate()
        {
            if (m_Canvas == null && !FindGameObjects())
            {
                Debug.LogError("Could not find the game objects required for the HitMarkSystem");
                Enabled = false;
                return;
            }
            var localToWorldFromEntity = SystemAPI.GetComponentLookup<LocalToWorld>();
            foreach (var hitMarker in SystemAPI.Query<RefRW<ServerHitMarker>>())
            {
                var hitMarkerValueRw = hitMarker.ValueRW;
                if (!HasHitMarkYet(hitMarkerValueRw.ServerHitTick, hitMarkerValueRw.AppliedClientTick))
                {
                    continue;
                }
                SpawnHitMark(hitMarkerValueRw.Victim, hitMarkerValueRw.HitPoint, m_ServerHitPrefab);
                hitMarker.ValueRW.AppliedClientTick = hitMarkerValueRw.ServerHitTick;
            }

            foreach (var hitMarker in SystemAPI.Query<RefRW<ClientHitMarker>>())
            {
                var hitMarkerValueRw = hitMarker.ValueRW;
                if (!HasHitMarkYet(hitMarkerValueRw.ClientHitTick, hitMarkerValueRw.AppliedClientTick))
                {
                    continue;
                }
                SpawnHitMark(hitMarkerValueRw.Victim, hitMarkerValueRw.HitPoint, m_ClientHitPrefab);
                hitMarker.ValueRW.AppliedClientTick = hitMarkerValueRw.ClientHitTick;
            }

            var camera = Camera.main;
            int objsToDestroy = 0;
            for (var index = 0; index < m_HitInformations.Count; index++)
            {
                var hitInformation = m_HitInformations[index];

                float3 worldPos = hitInformation.HitPosition;
                if (localToWorldFromEntity.HasComponent(hitInformation.Entity))
                {
                    var localToWorld = localToWorldFromEntity[hitInformation.Entity];
                    worldPos = math.mul(localToWorld.Value, new float4(worldPos, 1)).xyz;
                }
                else
                {
                    hitInformation.Age = 1;
                }

                var pos = camera.WorldToViewportPoint(worldPos);

                var canvasRectSize = m_CanvasRect.sizeDelta;
                hitInformation.Go.transform.localPosition = new Vector2(pos.x * canvasRectSize.x, pos.y * canvasRectSize.y) - canvasRectSize * 0.5f;
                hitInformation.Age += UnityEngine.Time.deltaTime;
                if (hitInformation.Age > 1)
                {
                    objsToDestroy++;
                }
                m_HitInformations[index] = hitInformation;
            }

            if (objsToDestroy > 0)
            {
                for (var i = 0; i < objsToDestroy; i++)
                {
                    Object.Destroy(m_HitInformations[i].Go);
                }
                m_HitInformations.RemoveRange(0, objsToDestroy);
            }
        }

        void SpawnHitMark(Entity hitEntity, float3 hitPoint, GameObject prefab)
        {
            var go = Object.Instantiate(prefab, m_Canvas.transform, true);
            var hitInformation = new HitInformation
            {
                Go = go,
                Entity = hitEntity,
                HitPosition = hitPoint,
            };
            m_HitInformations.Add(hitInformation);
        }

        static bool HasHitMarkYet(NetworkTick hitTick, NetworkTick appliedTick)
        {
            return hitTick.IsValid && (!appliedTick.IsValid || hitTick.IsNewerThan(appliedTick));
        }
    }
}
