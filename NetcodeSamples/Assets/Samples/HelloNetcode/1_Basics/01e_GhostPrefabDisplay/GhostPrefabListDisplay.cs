using Unity.Collections;
using Unity.Entities;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Samples.HelloNetcode
{
    struct GhostPrefabCleanup : ICleanupComponentData
    {
        public GhostType GhostType;
    }

    public class GhostPrefabListDisplay : MonoBehaviour
    {
        public RectTransform content;

        public void AddGhostPrefab(string worldName, Entity ghostPrefab, GhostType ghostType, string debugName)
        {
            // The ghost type might have been missing before, so first try removing that element from the list
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var prefabText = content.GetChild(i).GetComponent<Text>();
                if (prefabText != null && prefabText.text.Contains(ghostType.ToString()) && prefabText.text.Contains("MISSING") && prefabText.text.Contains(worldName))
                    Destroy(content.GetChild(i).gameObject);
            }

            var item = new GameObject("GhostPrefab");

            var text = item.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 11;
            text.color = new Color(0.196f, 0.196f, 0.196f);
            text.text = $"{worldName} - {ghostPrefab} - {ghostType} - {debugName}";

            var le = item.AddComponent<LayoutElement>();
            le.minHeight = 20;
            le.preferredHeight = 20;

            item.transform.SetParent(content, worldPositionStays: false);
        }

        public void RemoveGhostPrefab(string worldName, GhostType ghostType)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var prefabText = content.GetChild(i).GetComponent<Text>();
                if (prefabText != null && prefabText.text.Contains(ghostType.ToString()) && prefabText.text.Contains(worldName))
                    Destroy(content.GetChild(i).gameObject);
            }
        }

        public void AddMissingCollectionPrefab(string worldName, GhostType ghostType)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                var prefabText = content.GetChild(i).GetComponent<Text>();
                if (prefabText != null && prefabText.text.Contains(ghostType.ToString()) && prefabText.text.Contains(worldName))
                    return;
            }

            var item = new GameObject("GhostPrefab");

            var text = item.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 11;
            text.color = Color.crimson;
            text.text = $"{worldName} - MISSING - {ghostType}";

            var le = item.AddComponent<LayoutElement>();
            le.minHeight = 20;
            le.preferredHeight = 20;

            item.transform.SetParent(content, worldPositionStays: false);
        }

        public void OnToggleDisplay(Toggle value)
        {
            content.root.GetChild(0).gameObject.SetActive(value.isOn);
        }

        public void Clear()
        {
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation |  WorldSystemFilterFlags.ClientSimulation)]
    public partial class GhostPrefabListSystem : SystemBase
    {
        GhostPrefabListDisplay m_PrefabDisplay;
        double m_PrefabCollectionTimer;

        protected override void OnCreate()
        {
            m_PrefabDisplay = GameObject.FindAnyObjectByType<GhostPrefabListDisplay>();
        }

        protected override void OnUpdate()
        {
            if (m_PrefabDisplay == null)
            {
                Enabled = false;
                return;
            }

            var ecb = new EntityCommandBuffer(Allocator.Temp);
#if NETCODE_DEBUG
            var debugNamesLookup = SystemAPI.GetComponentLookup<PrefabDebugName>();
#endif
            foreach (var (ghostType, entity) in SystemAPI.Query<RefRO<GhostType>>().WithChangeFilter<GhostType>().WithAll<Prefab>().WithOptions(EntityQueryOptions.IncludePrefab).WithEntityAccess())
            {
                var debugName = World.EntityManager.GetName(entity);
#if NETCODE_DEBUG
                if (string.IsNullOrEmpty(debugName) && debugNamesLookup.HasComponent(entity))
                {
                    var prefabDebugName = debugNamesLookup[entity];
                    debugName = prefabDebugName.PrefabName.ConvertToString();
                }
#endif
                m_PrefabDisplay.AddGhostPrefab(World.Name, entity, ghostType.ValueRO, debugName);
                ecb.AddComponent(entity, new GhostPrefabCleanup {GhostType = ghostType.ValueRO});
                Debug.Log($"[{World.Name}] New prefab detected {entity} {ghostType.ValueRO}");
            }
            foreach (var (ghostPrefabCleanup, entity) in SystemAPI.Query<RefRO<GhostPrefabCleanup>>().WithNone<GhostType>().WithOptions(EntityQueryOptions.IncludePrefab).WithEntityAccess())
            {
                m_PrefabDisplay.RemoveGhostPrefab(World.Name, ghostPrefabCleanup.ValueRO.GhostType);
                ecb.RemoveComponent<GhostPrefabCleanup>(entity);
            }

            if (World.IsClient() && SystemAPI.Time.ElapsedTime > m_PrefabCollectionTimer)
            {
                foreach (var prefabCollection in SystemAPI.Query<DynamicBuffer<GhostCollectionPrefab>>())
                {
                    for (int i = 0; i < prefabCollection.Length; i++)
                    {
                        if (prefabCollection[i].GhostPrefab == Entity.Null)
                            m_PrefabDisplay.AddMissingCollectionPrefab(World.Name, prefabCollection[i].GhostType);
                    }
                }

                m_PrefabCollectionTimer = SystemAPI.Time.ElapsedTime + 1.0F;
            }

            ecb.Playback(EntityManager);
        }
    }
}
