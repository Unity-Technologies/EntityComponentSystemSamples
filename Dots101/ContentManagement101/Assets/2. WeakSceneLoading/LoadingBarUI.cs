using UnityEngine;
using UnityEngine.UIElements;

namespace ContentManagement.Sample
{
    public class LoadingBarUI : MonoBehaviour
    {
        private ProgressBar m_ProgressBar;
        public ProgressBar ProgressBar
        {
            get
            {
                if (m_ProgressBar == null)
                {
                    var document = GetComponent<UIDocument>();
                    var root = document.rootVisualElement;
                    m_ProgressBar = root.Query<ProgressBar>("progress-bar");
                }

                return m_ProgressBar;
            }
        }
    }
}