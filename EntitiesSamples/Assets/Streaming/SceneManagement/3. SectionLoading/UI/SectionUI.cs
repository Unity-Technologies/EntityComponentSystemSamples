using System.Collections.Generic;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.UIElements;

namespace Streaming.SceneManagement.SectionLoading
{
    public class SectionUI : MonoBehaviour
    {
        [SerializeField] VisualTreeAsset listEntryTemplate;

        public static SectionUI Singleton;

        private List<Row> rows;

        private static readonly Color UnloadedColor = new Color(0f, 0f, 1f);
        private static readonly Color InProgressColor = new Color(1f, 1f, 0f);
        private static readonly Color LoadedColor = new Color(0.5f, 1f, 0.5f);
        private static readonly Color ErrorColor = new Color(1f, 0.5f, 0.5f);

        private static readonly string ButtonLoadActionText = "Load";
        private static readonly string ButtonUnloadActionText = "Unload";
        private static readonly string ButtonNoActionActionText = "No Action";

        // Lookup table with the text for the buttons to display based on the section loading state
        private static readonly string[] ButtonTextPerState = new[]
        {
            ButtonLoadActionText, // Unloaded
            ButtonUnloadActionText, // LoadRequested
            ButtonUnloadActionText, // Loaded
            ButtonUnloadActionText, // Loading
            ButtonLoadActionText, // UnloadRequested,
            ButtonUnloadActionText, // FailedToLoad
        };

        // Lookup table with the color to use to display the section loading state based on its value
        private static readonly Color[] ButtonColorPerState = new[]
        {
            UnloadedColor, // Unloaded
            InProgressColor, // LoadRequested
            LoadedColor, // Loaded
            InProgressColor, // Loading
            InProgressColor, // UnloadRequested
            ErrorColor, // FailedToLoad
        };

        private VisualElement sceneslist;

        // Start is called before the first frame update
        void Start()
        {
            var uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;
            sceneslist = root.Q<VisualElement>("scenes");
            rows = new List<Row>();
            Singleton = this;
        }

        private bool initialized = false;

        public void CreateRows(int numSections)
        {
            if (initialized)
            {
                return;
            }

            if (rows.Count == 0)
            {
                // We need to initialize the rows
                for (var i = 0; i < numSections; i++)
                {
                    var visualEntry = listEntryTemplate.Instantiate();
                    var row = new Row
                    {
                        sceneName = visualEntry.Q<Label>("scene-name"),
                        sceneStatus = visualEntry.Q<Label>("loading-status"),
                        actionButton = visualEntry.Q<Button>("action-button"),
                    };

                    row.sceneName.text = $"Section {i}";
                    var index = i;
                    row.actionButton.clicked += () => OnActionClick(index);

                    rows.Add(row);
                    sceneslist.Add(visualEntry);
                }
            }

            initialized = true;
        }

        public void UpdateRow(int rowIndex, bool disabled, SceneSystem.SectionStreamingState sectionState)
        {
            var row = rows[rowIndex];
            row.actionButton.SetEnabled(!disabled);
            if (disabled)
            {
                row.sceneStatus.text = "This sample should be started with the subscene closed.";
                row.actionButton.text = ButtonNoActionActionText;
                row.sceneStatus.style.color = ErrorColor;
            }
            else
            {
                row.sceneStatus.text = sectionState.ToString();
                row.actionButton.text = ButtonTextPerState[(uint)sectionState];
                row.sceneStatus.style.color = ButtonColorPerState[(uint)sectionState];
            }
        }

        private int clickedRowIndex;
        private bool clicked = false;

        private void OnActionClick(int rowIndex)
        {
            clickedRowIndex = rowIndex;
            clicked = true;
        }

        public bool GetAction(out int rowIndex)
        {
            rowIndex = clickedRowIndex;

            var temp = clicked;
            clicked = false;
            return temp;
        }

        public class Row
        {
            public Label sceneName;
            public Label sceneStatus;
            public Button actionButton;
        }
    }
}
