using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Streaming.SceneManagement.SceneState
{
    // Component to control the UI in the sample
    public class StateUI : MonoBehaviour
    {
        [SerializeField] VisualTreeAsset rowTemplate;

        public static StateUI Singleton;

        private List<Row> rows;
        private VisualElement root;

        private static readonly Color UnloadedColor = new Color(0f, 0f, 1f);
        private static readonly Color InProgressColor = new Color(1f, 1f, 0f);
        private static readonly Color LoadedMetaColor = new Color(0.6f, 1f, 0.6f);
        private static readonly Color LoadedColor = new Color(0f, 1f, 0f);
        private static readonly Color ErrorColor = new Color(1f, 0.5f, 0.5f);

        private bool initialized;
        private LoadingAction lastClickedAction = LoadingAction.None;
        private int lastClickedRow;
        private bool clicked = false;

        // Look up table to choose the color of the loading state based in its value
        private static readonly Color[] ButtonColorPerState =
        {
            UnloadedColor, // Unloaded
            LoadedMetaColor, // LoadedSectionEntities
            InProgressColor, // Loading
            LoadedColor, // LoadedSuccessfully
            InProgressColor, // Unloading

            ErrorColor, // LoadingSceneHeaderFailed
            ErrorColor, // LoadingSectionFailed
        };

        // which loading actions are active/valid for which states
        private static readonly int[] AvailableLoadingActionsPerState =
        {
            (int)LoadingAction.LoadAll | (int)LoadingAction.LoadMeta, // Unloaded
            (int)LoadingAction.LoadAll | (int)LoadingAction.UnloadAll, // LoadedSectionEntities
            (int)LoadingAction.UnloadAll, // Loading
            (int)LoadingAction.UnloadAll | (int)LoadingAction.UnloadEntities, // LoadedSuccessfully
            (int)LoadingAction.LoadAll | (int)LoadingAction.LoadMeta, // Unloading

            (int)LoadingAction.UnloadAll , // LoadingSceneHeaderFailed
            (int)LoadingAction.UnloadAll, // LoadingSectionFailed
        };

        void Start()
        {
            var uiDocument = GetComponent<UIDocument>();
            root = uiDocument.rootVisualElement.Q<VisualElement>("scenes");
            rows = new List<Row>();
            Singleton = this;
        }

        public void UpdateSceneRows(NativeArray<SceneReference> scenes)
        {
            if (!initialized)
            {
                for (var index = 0; index < scenes.Length; index++)
                {
                    var scene = scenes[index];
                    var visualEntry = rowTemplate.Instantiate();
                    var row = new Row
                    {
                        SceneName = visualEntry.Q<Label>("scene-name"),
                        StreamingState = visualEntry.Q<Label>("loading-status"),
                        ActionButton0 = visualEntry.Q<Button>("action-button0"),
                        ActionButton1 = visualEntry.Q<Button>("action-button1"),
                        ActionButton2 = visualEntry.Q<Button>("action-button2"),
                        ActionButton3 = visualEntry.Q<Button>("action-button3")
                    };
                    row.SceneName.text = scene.SceneName.ToString();

                    row.ActionButton0.text = "Load All";
                    row.ActionButton1.text = "Load Section Entities";
                    row.ActionButton2.text = "Unload All";
                    row.ActionButton3.text = "Unload Content";

                    var idx = index; // captured variable
                    row.ActionButton0.clicked += () => OnActionClick(idx, LoadingAction.LoadAll);
                    row.ActionButton1.clicked += () => OnActionClick(idx, LoadingAction.LoadMeta);
                    row.ActionButton2.clicked += () => OnActionClick(idx, LoadingAction.UnloadAll);
                    row.ActionButton3.clicked += () => OnActionClick(idx, LoadingAction.UnloadEntities);

                    rows.Add(row);
                    root.Add(visualEntry);
                }

                initialized = true;
            }

            foreach (var scene in scenes)
            {
                string sceneName = scene.SceneName.ToString();
                var row = rows.Find((row => row.SceneName.text.Equals(sceneName)));
                if (row != null)
                {
                    var state = scene.StreamingState;
                    row.StreamingState.text = state.ToString();
                    row.StreamingState.style.color = ButtonColorPerState[(uint)state];

                    var loadingActions = AvailableLoadingActionsPerState[(uint)state];
                    row.ActionButton0.SetEnabled((loadingActions & (int)LoadingAction.LoadAll) > 0);
                    row.ActionButton1.SetEnabled((loadingActions & (int)LoadingAction.LoadMeta) > 0);
                    row.ActionButton2.SetEnabled((loadingActions & (int)LoadingAction.UnloadAll) > 0);
                    row.ActionButton3.SetEnabled((loadingActions & (int)LoadingAction.UnloadEntities) > 0);
                }
            }
        }

        public bool GetAction(out int sceneIndex, out LoadingAction action)
        {
            sceneIndex = lastClickedRow;
            action = lastClickedAction;

            var temp = clicked;
            clicked = false;
            return temp;
        }

        private void OnActionClick(int sceneIndex, LoadingAction action)
        {
            lastClickedRow = sceneIndex;
            lastClickedAction = action;
            clicked = true;
        }

        // Class used to store the UI elements per scene
        public class Row
        {
            public Label SceneName;
            public Label StreamingState;
            public Button ActionButton0;
            public Button ActionButton1;
            public Button ActionButton2;
            public Button ActionButton3;
        }
    }
}
