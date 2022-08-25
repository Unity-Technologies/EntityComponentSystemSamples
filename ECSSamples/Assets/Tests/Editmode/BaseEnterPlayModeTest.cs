using NUnit.Framework;
using System.Collections;
using UnityEditor;
using UnityEngine;

public class BaseEnterPlayModeTest
{
    [SerializeField]
    bool m_OldEnterPlayModeOptionsEnabled;
    [SerializeField]
    EnterPlayModeOptions m_OldEnterPlayModeOptions;

    [SerializeField]
    protected bool m_IsInitialized;
    [SerializeField]
    protected bool m_IsSetupAndTearDownDisabled;

    protected void EnableSetupAndTearDown()
    {
        m_IsSetupAndTearDownDisabled = false;
    }

    protected void DisableSetupAndTearDown()
    {
        m_IsSetupAndTearDownDisabled = true;
    }

    [OneTimeSetUp]
    public void BaseOneTimeSetUp()
    {
        if (m_IsSetupAndTearDownDisabled)
            return;

        if (!m_IsInitialized)
        {
            m_OldEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            m_OldEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
            m_IsInitialized = true;
        }
    }

    [TearDown]
    public void BaseTearDown()
    {
        if (m_IsSetupAndTearDownDisabled)
            return;

        EditorSettings.enterPlayModeOptionsEnabled = m_OldEnterPlayModeOptionsEnabled;
        EditorSettings.enterPlayModeOptions = m_OldEnterPlayModeOptions;
    }

    public class SceneResetTestCaseSource : IEnumerable
    {
        public IEnumerator GetEnumerator()
        {
            yield return new TestCaseData(EnterPlayModeOptions.None).Returns(null);
            yield return new TestCaseData(EnterPlayModeOptions.DisableSceneReload).Returns(null);
        }
    }

    public static readonly EnterPlayModeOptions[] TestEnterPlayModeOptions =
    {
        EnterPlayModeOptions.None,
        EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload
    };
}
