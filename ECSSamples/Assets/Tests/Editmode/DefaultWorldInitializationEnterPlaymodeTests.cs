using NUnit.Framework;
using System.Collections;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace EnterPlayModeTests
{
    public class DefaultWorldInitializationEnterPlayModeTests : BaseEnterPlayModeTest
    {
        [SerializeField]
        bool m_ExpectingDomainReload;

        [SerializeField]
        int m_DefaultWorldsCount;
        [SerializeField]
        int m_EditorWorldsCount;
        [SerializeField]
        int m_FirstEnterWorldsCount;
        [SerializeField]
        int m_SecondEnterWorldsCount;

        static readonly EnterPlayModeOptions[] k_TestEnterPlayModeOptions =
        {
            EnterPlayModeOptions.None,
            EnterPlayModeOptions.DisableDomainReload,
            EnterPlayModeOptions.DisableSceneReload,
            EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload
        };

        [SetUp]
        public void RegisterDelegates()
        {
            DefaultWorldInitialization.DefaultWorldInitialized += OnDefaultWorldInitialized;
            DefaultWorldInitialization.DefaultWorldDestroyed += OnDefaultWorldCleanup;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [TearDown]
        public void UnregisterDelegates()
        {
            DefaultWorldInitialization.DefaultWorldInitialized -= OnDefaultWorldInitialized;
            DefaultWorldInitialization.DefaultWorldDestroyed -= OnDefaultWorldCleanup;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        [UnityTest]
        public IEnumerator DefaultBootstrappedWorld_OnSecondEnterPlayMode_ProducesOneWorld([ValueSource(nameof(TestEnterPlayModeOptions))] EnterPlayModeOptions enterPlayModeOptions)
        {
            // Set editor settings
            EditorSettings.enterPlayModeOptionsEnabled = enterPlayModeOptions != EnterPlayModeOptions.None;
            EditorSettings.enterPlayModeOptions = enterPlayModeOptions;
            m_ExpectingDomainReload = !enterPlayModeOptions.HasFlag(EnterPlayModeOptions.DisableDomainReload);

            DisableSetupAndTearDown();

            // Make sure we have an Editor world initialized
            AddVerificationScript();
            Assert.NotNull(World.DefaultGameObjectInjectionWorld);

            // Enter play mode
            Debug.Log("Before EnterPlayMode");
            yield return new EnterPlayMode(m_ExpectingDomainReload);
            m_DefaultWorldsCount = GetDefaultWorldsCount();
            m_EditorWorldsCount = GetEditorWorldsCount();
            m_FirstEnterWorldsCount = World.All.Count;
            Debug.Log("Before ExitPlayMode");
            yield return new ExitPlayMode();

            // Validate Game World in Play Mode
            Assert.AreEqual(1, m_DefaultWorldsCount);
            // Validate no Editor World in Play Mode
            Assert.AreEqual(0, m_EditorWorldsCount);

            Debug.Log("Before EnterPlayMode");
            yield return new EnterPlayMode(m_ExpectingDomainReload);
            m_DefaultWorldsCount = GetDefaultWorldsCount();
            m_EditorWorldsCount = GetEditorWorldsCount();
            m_SecondEnterWorldsCount = World.All.Count;
            Debug.Log("Before ExitPlayMode");
            yield return new ExitPlayMode();

            EnableSetupAndTearDown();

            Assert.AreEqual(1, m_DefaultWorldsCount);
            Assert.AreEqual(0, m_EditorWorldsCount);
            Assert.AreEqual(m_FirstEnterWorldsCount, m_SecondEnterWorldsCount);
        }

        [UnityTest]
        public IEnumerator DefaultBootstrappedWorld_OnDomainReload_RecreatesEditorWorld()
        {
            DisableSetupAndTearDown();

            // Make sure we have an Editor world initialized
            DefaultWorldInitialization.DefaultLazyEditModeInitialize();
            Assert.NotNull(World.DefaultGameObjectInjectionWorld);
            Assert.NotZero(GetEditorWorldsCount());

            AddVerificationScript();

            // Request domain reload
            EditorUtility.RequestScriptReload();
            yield return new RecompileScripts(false);
            Assert.AreEqual(0, GetDefaultWorldsCount()); // No game world in the edit mode
            Assert.AreEqual(1, GetEditorWorldsCount()); // Lazy init should be done by DefaultWorldInitializationVerificationScript script

            EnableSetupAndTearDown();
        }

        static int GetDefaultWorldsCount()
        {
            return GetWorldsCount("Default World");
        }

        static int GetEditorWorldsCount()
        {
            return GetWorldsCount("Editor World");
        }

        static int GetWorldsCount(string worldName)
        {
            var count = 0;
            foreach (var world in World.All)
            {
                if (world.Name == worldName)
                    count++;
            }

            return count;
        }

        static void AddVerificationScript()
        {
            var go = new GameObject("DefaultWorldInitializationVerificationScript");
            go.AddComponent<DefaultWorldInitializationVerificationScript>();
        }

        static bool s_AllowDisableChecks = true;

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            s_AllowDisableChecks = state != PlayModeStateChange.ExitingPlayMode;
        }

        static void OnDefaultWorldInitialized(World w)
        {
            Debug.Log("OnDefaultWorldInitialized");
            Assert.NotNull(w);

            var script = UnityEngine.Object.FindObjectOfType<DefaultWorldInitializationVerificationScript>();
            // The assertion verifies that the script is not enabled when the world is lazily initialized by the script itself
            // This mimics the behavior of the GameObjectEntity.
            Assert.That(script == null || !script.WasEnabled);
        }

        static void OnDefaultWorldCleanup()
        {
            Debug.Log("OnDefaultWorldCleanup");
            if (!s_AllowDisableChecks)
                return;

            // The correct order for Enter PlayMode is:
            // 1. *OnDisable
            // 2. *Editor World destroyed
            // -- domain reload
            // 3. *Game World created
            // 4. *OnEnable

            // The correct order for Exit PlayMode is:
            // 1. OnDisable
            // 2. Game World destroyed
            // 3. *Editor World created on demand
            // 4. *OnEnable

            // The correct order for Domain Reload is:
            // 1. *OnDisable
            // 2. *Editor World destroyed
            // 3. *Editor World created on demand
            // 4. *OnEnable

            // Items marked with * are validated by tests here.

            // The assertion verifies that World is cleaned after all default OnDisable were called.
            var script = UnityEngine.Object.FindObjectOfType<DefaultWorldInitializationVerificationScript>();
            Assert.IsFalse(script.WasEnabled);
        }
    }
}
