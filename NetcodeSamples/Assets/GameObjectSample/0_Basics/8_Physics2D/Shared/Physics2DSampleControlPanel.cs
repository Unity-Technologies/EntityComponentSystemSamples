#if NETCODE_GAMEOBJECT_PHYSICS2D_EXPERIMENTAL
using Unity.Entities;
using Unity.Netcode;
using UnityEngine;

namespace GameObjectSample.Physics2DSample
{
    // IMGUI panel: per-group prediction modes, corrections, network conditions and the
    // pyramid. F1 toggles it. Most controls only act on worlds in this process.
    public class Physics2DSampleControlPanel : MonoBehaviour
    {
        const KeyCode k_ToggleKey = KeyCode.F1;
        const int k_WindowId = 0x2D2D;
        const float k_ScatterStrength = 6f;

        static readonly BodyMode[] k_BodyModes = { BodyMode.Predicted, BodyMode.Interpolated };

        static readonly Rect k_OpenButton = new Rect(12, 12, 140, 24);

        static Physics2DSampleControlPanel s_Instance;

        // Built lazily: GUI.skin is only valid inside OnGUI.
        static GUIStyle s_WrappedLabel;
        static GUIStyle s_WrappedToggle;

        static void EnsureStyles()
        {
            if (s_WrappedLabel != null)
                return;
            s_WrappedLabel = new GUIStyle(GUI.skin.label) { wordWrap = true, richText = true };
            s_WrappedToggle = new GUIStyle(GUI.skin.toggle) { wordWrap = true };
        }

        static void WrappedLabel(string text) => GUILayout.Label(text, s_WrappedLabel);

        static bool WrappedToggle(bool value, string label) => GUILayout.Toggle(value, label, s_WrappedToggle);

        bool m_Open = true;
        Rect m_Window = new Rect(12, 12, 400, 700);
        Vector2 m_Scroll;

        int m_PacketDelayMs;
        int m_PacketJitterMs;
        int m_PacketDropPercent;
        bool m_RandomLagSpikes;
        float m_LagSpikeDuration = 1f;
        float m_SpikeIntervalMin = 3f;
        float m_SpikeIntervalMax = 8f;
        float m_LagSpikeEndsAt = -1f;
        float m_NextSpikeTime;
        bool m_SimulatorLive;
        bool m_SimulatorChecked;

        int m_PendingRows = -1;

        // Read by the player's input so a click on the panel doesn't also shoot.
        public static bool IsPointerOverPanel
        {
            get
            {
                if (s_Instance == null)
                    return false;
                var mouse = Input.mousePosition;
                var guiPoint = new Vector2(mouse.x, Screen.height - mouse.y);
                return (s_Instance.m_Open ? s_Instance.m_Window : k_OpenButton).Contains(guiPoint);
            }
        }

        void Awake() => s_Instance = this;

        // The sample's default network conditions, applied once the client connects; session only, never written back
        // to prefs.
        const int k_DefaultPacketDelayMs = 35;
        const int k_DefaultPacketJitterMs = 5;

        void Start() => SetConditions(k_DefaultPacketDelayMs, k_DefaultPacketJitterMs, 0);

        void OnDestroy()
        {
            if (s_Instance == this)
                s_Instance = null;
        }

        void Update()
        {
            if (Input.GetKeyDown(k_ToggleKey))
                m_Open = !m_Open;

            if (!m_SimulatorChecked && TryCheckSimulator(out m_SimulatorLive))
            {
                m_SimulatorChecked = true;
                if (m_SimulatorLive)
                    PushSimulatorSettings();
            }

            UpdateLagSpikes();
            ApplyPredictionModes();
        }

        static bool IsSwitchableClientGhost(GhostObject ghost) =>
            ghost != null && ghost.IsSpawned && ghost.IsClient && !ghost.World.IsHost();

        static void ApplyPredictionModes()
        {
            foreach (var player in Physics2DSample.Players)
            {
                if (!IsSwitchableClientGhost(player.Ghost))
                    continue;
                var mode = player.IsOwnedLocally ? Physics2DSample.LocalPlayerMode : Physics2DSample.RemotePlayerMode;
                ApplyMode(player.Ghost, mode);
            }

            foreach (var box in Physics2DSample.Boxes)
            {
                if (IsSwitchableClientGhost(box.Ghost))
                    ApplyMode(box.Ghost, Physics2DSample.BoxMode);
            }
        }

        static void ApplyMode(GhostObject ghost, BodyMode mode)
        {
            var wantPredicted = mode == BodyMode.Predicted;
            if (wantPredicted == ghost.CanWriteState)
                return;
            if (wantPredicted)
                ghost.ConvertToPredicted();
            else
                ghost.ConvertToInterpolated();
        }

        void OnGUI()
        {
            // StartChecks shows what to fix instead.
            if (!StartChecks.Passed)
                return;

            EnsureStyles();
            if (!m_Open)
            {
                if (GUI.Button(k_OpenButton, "Sample panel (F1)"))
                    m_Open = true;
                return;
            }
            m_Window = GUILayout.Window(k_WindowId, m_Window, DrawWindow, "2D physics sample");
        }

        void DrawWindow(int id)
        {
            // No horizontal bar: long text wraps instead (WrappedLabel / WrappedToggle).
            m_Scroll = GUILayout.BeginScrollView(m_Scroll, false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
            if (Netcode.IsHostRole)
            {
                Header("Host");
                WrappedLabel(   "You are running as a Host in a Single World. Prediction, Correction and the Network "+
                                "Simulator settings ony apply to pure client instances. Run a second client via "+
                                "'Multiplayer Play Mode' to try out different settings and simulated network conditions.");
            }
            else
            {
                DrawPredictionSection();
                DrawCorrectionsSection();
                DrawSimulatorSection();
            }
            DrawBoxesSection();
            GUILayout.EndScrollView();
            if (GUILayout.Button("Close (F1)"))
                m_Open = false;
            GUI.DragWindow();
        }

        static void Header(string title)
        {
            GUILayout.Space(8);
            GUILayout.Label($"<b>{title}</b>", new GUIStyle(GUI.skin.label) { richText = true });
        }

        static bool CanSwitchPrediction => Netcode.IsActive && Netcode.IsClientRole && !Netcode.IsHostRole;

        static void DrawPredictionSection()
        {
            Header("Prediction mode (this client)");
            if (!CanSwitchPrediction)
            {
                WrappedLabel("Available once this instance is connected as a client.");
                return;
            }

            Physics2DSample.LocalPlayerMode = ModeRow("Local player", Physics2DSample.LocalPlayerMode);
            Physics2DSample.RemotePlayerMode = ModeRow("Remote players", Physics2DSample.RemotePlayerMode);
            Physics2DSample.BoxMode = ModeRow("Boxes + shots", Physics2DSample.BoxMode);

            GUILayout.Label("Presets");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("All predicted"))
                SetModes(BodyMode.Predicted, BodyMode.Predicted, BodyMode.Predicted);
            if (GUILayout.Button("All interpolated"))
                SetModes(BodyMode.Interpolated, BodyMode.Interpolated, BodyMode.Interpolated);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Owner predicted"))
                SetModes(BodyMode.Predicted, BodyMode.Interpolated, BodyMode.Interpolated);
            if (GUILayout.Button("Players predicted, boxes interp."))
                SetModes(BodyMode.Predicted, BodyMode.Predicted, BodyMode.Interpolated);
            GUILayout.EndHorizontal();
        }

        static void SetModes(BodyMode local, BodyMode remote, BodyMode boxes)
        {
            Physics2DSample.LocalPlayerMode = local;
            Physics2DSample.RemotePlayerMode = remote;
            Physics2DSample.BoxMode = boxes;
        }

        static BodyMode ModeRow(string label, BodyMode current)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(110));
            foreach (var mode in k_BodyModes)
            {
                if (GUILayout.Toggle(current == mode, mode.ToString(), GUI.skin.button))
                    current = mode;
            }
            GUILayout.EndHorizontal();
            return current;
        }

        static void DrawCorrectionsSection()
        {
            Header("Corrections (this client)");
            if (!CanSwitchPrediction)
            {
                WrappedLabel("Available once this instance is connected as a client.");
                return;
            }

            var rollbackAll = AlwaysRollbackAllPredictedGhosts;
            var wantRollbackAll = WrappedToggle(rollbackAll,
                "Rollback all predicted ghosts together (off: only the ghosts in a snapshot roll back, so replays mix timelines)");
            if (wantRollbackAll != rollbackAll)
                AlwaysRollbackAllPredictedGhosts = wantRollbackAll;
        }

        static bool AlwaysRollbackAllPredictedGhosts
        {
            get
            {
                foreach (var world in ClientServerBootstrap.AllClientWorldsEnumerator())
                {
                    if (!world.IsCreated || world.IsThinClient() || world.IsHost())
                        continue;
                    using var query = world.EntityManager.CreateEntityQuery(typeof(ClientTickRate));
                    if (!query.IsEmptyIgnoreFilter)
                        return query.GetSingleton<ClientTickRate>().AlwaysRollbackAllPredictedGhosts;
                }
                return false;
            }
            set
            {
                foreach (var world in ClientServerBootstrap.AllClientWorldsEnumerator())
                {
                    if (!world.IsCreated || world.IsThinClient() || world.IsHost())
                        continue;
                    using var query = world.EntityManager.CreateEntityQuery(typeof(ClientTickRate));
                    if (query.IsEmptyIgnoreFilter)
                        continue;
                    var tickRate = query.GetSingleton<ClientTickRate>();
                    tickRate.AlwaysRollbackAllPredictedGhosts = value;
                    query.SetSingleton(tickRate);
                }
            }
        }

        void DrawSimulatorSection()
        {
            Header("Network simulator (this client)");
            if (!m_SimulatorLive)
            {
                WrappedLabel("No client driver here has the simulator. Enable 'Client Network Simulator' in " +
                    "Multiplayer PlayMode Tools and re-enter play mode.");
                return;
            }

            var changed = false;
            changed |= IntSlider("Delay", "ms", ref m_PacketDelayMs, 0, 500);
            changed |= IntSlider("Jitter", "ms", ref m_PacketJitterMs, 0, 200);
            changed |= IntSlider("Packet loss", "%", ref m_PacketDropPercent, 0, 100);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clean"))
                changed |= SetConditions(0, 0, 0);
            if (GUILayout.Button("Broadband"))
                changed |= SetConditions(35, 5, 0);
            if (GUILayout.Button("Bad wifi"))
                changed |= SetConditions(150, 40, 5);
            GUILayout.EndHorizontal();

            if (GUILayout.Button($"Lag spike ({m_LagSpikeDuration:0.#}s)"))
            {
                m_LagSpikeEndsAt = Time.unscaledTime + m_LagSpikeDuration;
                changed = true;
            }

            var randomSpikes = GUILayout.Toggle(m_RandomLagSpikes, "Random lag spikes");
            if (randomSpikes != m_RandomLagSpikes)
            {
                m_RandomLagSpikes = randomSpikes;
                if (m_RandomLagSpikes)
                    ScheduleNextSpike();
            }
            if (m_RandomLagSpikes)
            {
                FloatSlider("  Spike duration", "s", ref m_LagSpikeDuration, 0.1f, 3f);
                FloatSlider("  Interval min", "s", ref m_SpikeIntervalMin, 0.5f, 30f);
                FloatSlider("  Interval max", "s", ref m_SpikeIntervalMax, 0.5f, 60f);
                if (m_SpikeIntervalMax < m_SpikeIntervalMin)
                    m_SpikeIntervalMax = m_SpikeIntervalMin;
            }

            if (changed)
                PushSimulatorSettings();
        }

        bool SetConditions(int delayMs, int jitterMs, int dropPercent)
        {
            m_PacketDelayMs = delayMs;
            m_PacketJitterMs = jitterMs;
            m_PacketDropPercent = dropPercent;
            return true;
        }

        void UpdateLagSpikes()
        {
            if (m_LagSpikeEndsAt > 0f && Time.unscaledTime >= m_LagSpikeEndsAt)
            {
                m_LagSpikeEndsAt = -1f;
                PushSimulatorSettings();
            }

            if (!m_RandomLagSpikes || !m_SimulatorLive || m_LagSpikeEndsAt > 0f || Time.unscaledTime < m_NextSpikeTime)
                return;

            m_LagSpikeEndsAt = Time.unscaledTime + m_LagSpikeDuration;
            ScheduleNextSpike();
            PushSimulatorSettings();
        }

        void ScheduleNextSpike()
        {
            m_NextSpikeTime = Time.unscaledTime + m_LagSpikeDuration + UnityEngine.Random.Range(m_SpikeIntervalMin, m_SpikeIntervalMax);
        }

        // Session only: never written back to the PlayMode Tools prefs.
        void PushSimulatorSettings()
        {
#if UNITY_EDITOR || NETCODE_DEBUG
            var parameters = NetworkSimulatorSettings.ClientSimulatorParameters;
            parameters.PacketDelayMs = m_PacketDelayMs;
            parameters.PacketJitterMs = m_PacketJitterMs;
            parameters.PacketDropPercentage = m_LagSpikeEndsAt > 0f ? 100 : m_PacketDropPercent;

            foreach (var world in ClientServerBootstrap.AllClientWorldsEnumerator())
            {
                if (!world.IsCreated)
                    continue;
                using var query = world.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                if (query.IsEmptyIgnoreFilter)
                    continue;
                query.CompleteDependency();
                var netStream = query.GetSingletonRW<NetworkStreamDriver>();
                NetworkSimulatorSettings.RefreshSimulationPipelineParametersLive(in parameters, ref netStream.ValueRW.DriverStore);
            }
#endif
        }

        // Returns false until a client world is connected. From then on the answer can't change without re-entering
        // play mode, so the caller checks once.
        static bool TryCheckSimulator(out bool simulatorEnabled)
        {
            simulatorEnabled = false;
#if UNITY_EDITOR || NETCODE_DEBUG
            var anyConnected = false;
            foreach (var world in ClientServerBootstrap.AllClientWorldsEnumerator())
            {
                if (!world.IsCreated)
                    continue;
                using var connectionQuery = world.EntityManager.CreateEntityQuery(typeof(NetworkStreamConnection));
                using var driverQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
                if (connectionQuery.IsEmptyIgnoreFilter || driverQuery.IsEmptyIgnoreFilter)
                    continue;
                anyConnected = true;
                driverQuery.CompleteDependency();
                ref var store = ref driverQuery.GetSingletonRW<NetworkStreamDriver>().ValueRW.DriverStore;
                for (var i = store.FirstDriver; i < store.LastDriver; ++i)
                {
                    if (store.GetDriverInstanceRO(i).simulatorEnabled)
                    {
                        simulatorEnabled = true;
                        return true;
                    }
                }
            }
            return anyConnected;
#else
            return true;
#endif
        }

        void DrawBoxesSection()
        {
            Header("Boxes (server)");
            var spawner = BoxPyramidSpawner.Instance;
            if (spawner == null || !Netcode.IsServerRole)
            {
                WrappedLabel("The pyramid is spawned by the server. Change it from the server/host instance.");
                return;
            }

            if (m_PendingRows < 0)
                m_PendingRows = spawner.Rows;
            IntSlider("Rows", "", ref m_PendingRows, 1, BoxPyramidSpawner.MaxRows);
            GUILayout.Label($"{BoxPyramidSpawner.BoxCountForRows(m_PendingRows)} boxes (currently {spawner.SpawnedCount})");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Respawn"))
            {
                spawner.Rows = m_PendingRows;
                spawner.Respawn();
            }
            if (GUILayout.Button("Scatter"))
                spawner.Scatter(k_ScatterStrength);
            GUILayout.EndHorizontal();
        }

        static bool IntSlider(string label, string unit, ref int value, int min, int max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {value}{unit}", GUILayout.Width(140));
            var next = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, min, max));
            GUILayout.EndHorizontal();
            if (next == value)
                return false;
            value = next;
            return true;
        }

        static void FloatSlider(string label, string unit, ref float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {value:0.0##}{unit}", GUILayout.Width(140));
            value = GUILayout.HorizontalSlider(value, min, max);
            GUILayout.EndHorizontal();
        }
    }
}
#endif
