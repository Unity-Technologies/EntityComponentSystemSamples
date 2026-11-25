#if NETCODE_GAMEOBJECT_PHYSICS2D_EXPERIMENTAL
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace GameObjectSample.Physics2DSample
{
    // Server side: stacks box ghosts into a pyramid with its bottom row centred on this Transform.
    // Rows is the scaling knob: n rows is n(n+1)/2 boxes.
    public class BoxPyramidSpawner : MonoBehaviour
    {
        public const int MaxRows = 40;

        [SerializeField] GameObject m_BoxPrefab;
        [SerializeField, Range(1, MaxRows)] int m_Rows = 6;
        [SerializeField, Tooltip("Gap left between boxes when spawned, so they settle instead of starting in contact.")]
        float m_Gap = 0.02f;

        readonly List<GameObject> m_Boxes = new List<GameObject>();

        // The panel reaches the spawner through this. Only works with the server in this process.
        public static BoxPyramidSpawner Instance { get; private set; }

        public int Rows
        {
            get => m_Rows;
            set => m_Rows = Mathf.Clamp(value, 1, MaxRows);
        }

        public int SpawnedCount => m_Boxes.Count;

        public static int BoxCountForRows(int rows) => rows * (rows + 1) / 2;

        void OnEnable() => Instance = this;

        void OnDisable()
        {
            if (Instance == this)
                Instance = null;
        }

        void Start()
        {
            if (Netcode.IsServerRole)
                Respawn();
        }

        public void Respawn()
        {
            if (!Netcode.IsServerRole || !StartChecks.Passed)
                return;

            Clear();
            // The prefab's scale is the box size; the bodies read it when they spawn.
            var boxSize = m_BoxPrefab.transform.localScale.y;
            var step = boxSize + m_Gap;
            var origin = transform.position + Vector3.up * (boxSize * 0.5f + m_Gap);
            for (var row = 0; row < m_Rows; ++row)
            {
                var count = m_Rows - row;
                var startX = -(count - 1) * step * 0.5f;
                for (var i = 0; i < count; ++i)
                {
                    var position = origin + new Vector3(startX + i * step, row * step, 0f);
                    m_Boxes.Add(Instantiate(m_BoxPrefab, position, Quaternion.identity));
                }
            }
        }

        public void Clear()
        {
            foreach (var box in m_Boxes)
            {
                if (box != null)
                    Destroy(box);
            }
            m_Boxes.Clear();
        }

        // Kicks every box upwards, in directions seeded from the tick.
        public void Scatter(float strength)
        {
            Random.InitState((int)Netcode.Time.ServerTick.TickIndexForValidTick);
            foreach (var box in m_Boxes)
            {
                if (box == null)
                    continue;
                var direction = new Vector2(Random.Range(-1f, 1f), Random.Range(0.5f, 1.5f)).normalized;
                box.GetComponent<ISampleBody2D>()?.AddImpulse(direction * strength);
            }
        }
    }
}
#endif
