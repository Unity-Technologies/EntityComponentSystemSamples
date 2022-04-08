using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Unity.PerformanceTesting;
using Unity.PerformanceTesting.Data;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using SampleGroup = Unity.PerformanceTesting.Data.SampleGroup;

namespace Editor.PerformanceTestViewer
{
    public class PerformanceTestViewer : EditorWindow, IHasCustomMenu
    {
        private const float PlotAlpha = 0.4f;
        private static Color ResultsColor => EditorGUIUtility.isProSkin ? ResultsColorDark : ResultsColorBright;
        private static Color ResultsColorText
        {
            get
            {
                var c = ResultsColor;
                c.a = 1;
                return c;
            }
        }
        private static Color BaselineColor => EditorGUIUtility.isProSkin ? BaselineColorDark : BaselineColorBright;
        private static Color BaselineColorText
        {
            get
            {
                var c = BaselineColor;
                c.a = 1;
                return c;
            }
        }

        // Colors are carefully chosen to be color-blind friendly.
        private static readonly Color ResultsColorBright = new Color(1, 194/255f, 10/255f, PlotAlpha);
        private static readonly Color ResultsColorDark = new Color(254/255f, 254/255f, 98/255f, PlotAlpha);
        private static readonly Color BaselineColorBright = new Color(12/255f, 123/255f, 220/255f, PlotAlpha);
        private static readonly Color BaselineColorDark = new Color(211/255f, 95/255f, 183/255f, PlotAlpha);
        private static readonly Color RegressionColor = Color.red;
        private static readonly Color ImprovementColor = Color.green;

        private enum Category {
            Regressions = 0,
            Improvements = 1,
            Inconclusive = 2,
            All = 3
        }

        struct HistogramData
        {
            public double Min;
            public double Max;
            public SampleGroup X;
            public SampleGroup Y;
        }

        struct ChangeResult
        {
            public string Name;
            public StatisticsUtility.WelchTestResult Test;
            public HistogramData Histogram;

            /// <summary>
            /// The maximum regression percentage in the interval relative to the base value.
            /// </summary>
            public double LowerChangePercentage => Test.IntervalLower / Test.MeanY;
            public double UpperChangePercentage => Test.IntervalUpper / Test.MeanY;
        }

#pragma warning disable 0649
        [SerializeField]
        private UnityEngine.Object _readMeAsset;
#pragma warning restore 0649

        private Run _baselineData;
        private Run _comparisonData;
        private Run _tmpResults;
        private string _testFilter;
        private readonly List<ChangeResult> _regressions = new List<ChangeResult>();
        private readonly List<ChangeResult> _filteredRegressions = new List<ChangeResult>();
        private readonly List<ChangeResult> _improvements = new List<ChangeResult>();
        private readonly List<ChangeResult> _filteredImprovements = new List<ChangeResult>();
        private readonly List<ChangeResult> _inconclusive = new List<ChangeResult>();
        private readonly List<ChangeResult> _filteredInconclusive = new List<ChangeResult>();
        private readonly List<ChangeResult> _allResults = new List<ChangeResult>();
        private readonly List<ChangeResult> _filteredAll = new List<ChangeResult>();

        private ListView _resultsList;
        private Label _dataStatusLabel;
        private Label _regressionCountLabel;
        private Label _improvementCountLabel;
        private Label _inconclusiveCountLabel;
        private Label _totalCountLabel;
        private Label _baselineOverviewLabel;
        private Label _newResultsOverviewLabel;
        private Button _recordBaselineButton;
        private Button _recordComparisonButton;
        private FileSystemWatcher _fileWatcher;
        private bool _isRecordingNewBaseline;
        private bool _isRecordingNewComparison;

        private List<ChangeResult> _currentData;
        private List<ChangeResult> _currentSource;

        private static string BaselineDataPath =>
            Path.Combine(Application.persistentDataPath, "PerformanceTestResults_baseline.json");
        private static string ComparisonDataPath =>
            Path.Combine(Application.persistentDataPath, "PerformanceTestResults_comparison.json");
        private static string ResultsPath =>
            Path.Combine(Application.persistentDataPath, "PerformanceTestResults.json");
        private const string HelpText = "Use the window's menu (top right) for more options and to open the ReadMe. Please read the disclaimer in there.";

        [MenuItem("Window/Analysis/Performance Test Viewer")]
        public static void ShowWindow()
        {
            var window = GetWindow<PerformanceTestViewer>();
            window.titleContent = new GUIContent("Performance Test Viewer");
            window.Show();
        }

        static string FormatPercentage(double p)
            => (p > 0 ? "+" : "") + (p * 100).ToString("F2", CultureInfo.InvariantCulture) + '%';

        static string FormatRelativeInterval(ChangeResult r) =>
            $"[{FormatPercentage(r.LowerChangePercentage)}, {FormatPercentage(r.UpperChangePercentage)}]";

        static string FormatAbsoluteChange(double p, ChangeResult r)
            => (p > 0 ? "-" : "") + p.ToString("F2", CultureInfo.InvariantCulture) + FormatUnit(r.Histogram.X.Unit);

        static string FormatAbsoluteInterval(ChangeResult r) =>
            $"[{FormatAbsoluteChange(r.Test.IntervalLower, r)}, {FormatAbsoluteChange(r.Test.IntervalUpper, r)}]";

        void ToggleBaselineRecording()
        {
            if (_isRecordingNewBaseline)
                StopBaselineRecording();
            else
            {
                StopComparisonRecording();
                _isRecordingNewBaseline = true;
                _recordBaselineButton.text = "Recording...";
            }
        }

        void ToggleComparisonRecording()
        {
            if (_isRecordingNewBaseline)
                StopComparisonRecording();
            else
            {
                StopBaselineRecording();
                _isRecordingNewComparison = true;
                _recordComparisonButton.text = "Recording...";
            }
        }

        void StopComparisonRecording()
        {
            _isRecordingNewComparison = false;
            _recordComparisonButton.text = "Record new comparison data";
        }

        void StopBaselineRecording()
        {
            _isRecordingNewBaseline = false;
            _recordBaselineButton.text = "Record new baseline";
        }

        void UpdateFilter(string s, bool reset=false)
        {
            s = s ?? "";

            bool Filter(ChangeResult r) => r.Name.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0;

            if (!reset && s.StartsWith(_testFilter, StringComparison.Ordinal))
            {
                _currentData.RemoveAll(t => !Filter(t));
            }
            else
            {
                _currentData.Clear();
                _currentData.AddRange(_currentSource.Where(Filter));
            }

            _testFilter = s;
            _resultsList.itemsSource = _currentData;
        }

        static string CleanupTestName(string name)
        {
            var separator = name.LastIndexOf('.');
            if (separator < 0)
                return name;
            return name.Substring(separator + 1);
        }

        class ListItem : VisualElement
        {
            public int Index;
        }

        private void DrawDistribution(IMGUIContainer self, ListItem item)
        {
            var rect = self.contentRect;
            var histogram = _currentData[item.Index].Histogram;
            GUI.Box(rect, "", GUI.skin.textArea);

            // add some spaces on the side
            double min = histogram.Min * 0.98f;
            double max = histogram.Max * 1.02f;

            var point = new Rect(0, 0, 3, 3);
            var offset = new Vector2(rect.xMin, rect.center.y);
            float width = rect.width;

            foreach (var sx in histogram.X.Samples)
            {
                float x = width * (float) ((sx - min) / (max - min));
                float y = 5 * UnityEngine.Random.value + 3;
                point.center = offset + new Vector2(x, y);
                EditorGUI.DrawRect(point, ResultsColor);
            }

            foreach (var sy in histogram.Y.Samples)
            {
                float x = width * (float) ((sy - min) / (max - min));
                float y = 5 * UnityEngine.Random.value - 3;
                point.center = offset + new Vector2(x, y);
                EditorGUI.DrawRect(point, BaselineColor);
            }
        }

        VisualElement CreateListItem()
        {
            var container = new ListItem
            {
                style =
                {
                    borderBottomWidth = 10,
                    borderTopWidth = 10,
                    borderLeftWidth = 4,
                    borderRightWidth = 4
                }
            };
            var topBar = new VisualElement {
                style =
                {
                    height = EditorGUIUtility.singleLineHeight,
                    flexDirection = FlexDirection.RowReverse,
                    flexGrow = 0,
                },
            };
            topBar.Add(new Label
            {
                name = "interval",
                tooltip = "This is the 95% confidence interval for the change in means, relative to the baseline mean.",
                style = { flexGrow = 0, minWidth = 80, marginLeft = 5}
            });
            topBar.Add(new Label { name = "name", style =
            {
                flexBasis = 0,
                flexGrow = 1, unityFont = EditorStyles.boldFont,
                overflow =  Overflow.Hidden
            }});
            container.Add(topBar);
            var plot = new IMGUIContainer
            {
                style = {flexGrow = 1},
            };
            plot.onGUIHandler = () => DrawDistribution(plot, container);
            plot.tooltip = "This is a plot of the distribution of the results. The metric you are tracking is on the horizontal axis (usually this is time, lower values are to the left). Each sample is represented by a point. The y-axis doesn't have a meaning. The vertical jitter is merely to aid legibility.";
            plot.AddManipulator(new ContextualMenuManipulator((ContextualMenuPopulateEvent evt) =>
            {
                evt.menu.AppendAction("Copy samples as CSV", (a) =>
                {
                    var idx = container.Index;
                    var d = _currentData[idx];
                    CsvExportUtility.CopySamplesCsv(d.Histogram.X, d.Histogram.Y);
                } );
            }));
            container.Add(plot);
            var bottomBar = new VisualElement {
                style =
                {
                    height = EditorGUIUtility.singleLineHeight,
                    flexDirection = FlexDirection.Row,
                    flexGrow = 0
                },
            };
            bottomBar.Add(new Label
            {
                name = "min",
                style = {flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft},
                tooltip = "Minimum value within the two samples."
            });
            bottomBar.Add(new Label
            {
                name = "middle",
                style = {flexGrow = 1, unityTextAlign = TextAnchor.MiddleCenter},
            });
            bottomBar.Add(new Label
            {
                name = "max",
                style = {flexGrow = 1, unityTextAlign = TextAnchor.MiddleRight},
                tooltip = "Maximum value within the two samples."
            });
            container.Add(bottomBar);
            return container;
        }

        private void BindListItem(VisualElement c, int idx)
        {
            (c as ListItem).Index = idx;
            var r = _currentData[idx];
            var name = c.Q<Label>("name");
            name.text = CleanupTestName(r.Name);
            name.tooltip = r.Name;
            c.Q<Label>("interval").text = FormatRelativeInterval(r);
            double min = r.Histogram.Min;
            double max = r.Histogram.Max;
            int unitLevel = DeconstructUnit(r.Histogram.X.Unit, out bool isMemory);
            int newLevel = AdjustUnit(unitLevel, max, isMemory);
            for (int i = unitLevel - newLevel; i > 0; i--)
            {
                min *= 1000;
                max *= 1000;
            }
            for (int i = newLevel - unitLevel; i > 0; i--)
            {
                min /= 1000;
                max /= 1000;
            }

            var unit = ConstructUnit(newLevel, isMemory);
            var unitName = FormatUnit(unit);
            c.Q<Label>("min").text = min.ToString("F2") + unitName;
            c.Q<Label>("middle").text = ((min + max) / 2).ToString("F2") + unitName;
            c.Q<Label>("max").text = max.ToString("F2") + unitName;
        }

        static int AdjustUnit(int unit, double value, bool isForMemory)
        {
            double factor = isForMemory ? 1024 : 1000;
            while (value < 0.1 && unit > 0)
            {
                value *= factor;
                unit -= 1;
            }

            while (value >= 1000 && unit < 3)
            {
                value /= factor;
                unit += 1;
            }
            return unit;
        }

        static SampleUnit ConstructUnit(int unit, bool isMemoryMeasure)
        {
            switch (unit)
            {
                case 0: return isMemoryMeasure ? SampleUnit.Byte : SampleUnit.Nanosecond;
                case 1: return isMemoryMeasure ? SampleUnit.Kilobyte : SampleUnit.Microsecond;
                case 2: return isMemoryMeasure ? SampleUnit.Megabyte : SampleUnit.Millisecond;
                case 3: return isMemoryMeasure ? SampleUnit.Gigabyte : SampleUnit.Second;
                default: throw new ArgumentException(nameof(unit), nameof(unit));
            }
        }

        static int DeconstructUnit(SampleUnit unit, out bool isMemoryMeasure)
        {
            isMemoryMeasure = false;
            switch (unit)
            {
                case SampleUnit.Nanosecond: return 0;
                case SampleUnit.Microsecond: return 1;
                case SampleUnit.Millisecond: return 2;
                case SampleUnit.Second: return 3;
                case SampleUnit.Byte: isMemoryMeasure = true; return 0;
                case SampleUnit.Kilobyte: isMemoryMeasure = true; return 1;
                case SampleUnit.Megabyte: isMemoryMeasure = true; return 2;
                case SampleUnit.Gigabyte: isMemoryMeasure = true; return 3;
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit), unit, null);
            }
        }

        static string FormatUnit(SampleUnit unit)
        {
            switch (unit)
            {
                case SampleUnit.Nanosecond: return "ns";
                case SampleUnit.Microsecond: return "μs";
                case SampleUnit.Millisecond: return "ms";
                case SampleUnit.Second: return "s";
                case SampleUnit.Byte: return "b";
                case SampleUnit.Kilobyte: return "kb";
                case SampleUnit.Megabyte: return "mb";
                case SampleUnit.Gigabyte: return "gb";
                default:
                    throw new ArgumentOutOfRangeException(nameof(unit), unit, null);
            }
        }

        private void SearchFilterChanged(ChangeEvent<string> evt)
        {
            UpdateFilter(evt.newValue);
        }

        private Category _selectedCategory;
        private void SetCategorySelection(Category category)
        {
            _selectedCategory = category;
            _categorySelection.SetValueWithoutNotify(category);
            switch (category)
            {
                case Category.Regressions:
                {
                    _currentData = _filteredRegressions;
                    _currentSource = _regressions;
                    break;
                }
                case Category.Improvements:
                {
                    _currentData = _filteredImprovements;
                    _currentSource = _improvements;
                    break;
                }
                case Category.Inconclusive:
                {
                    _currentData = _filteredInconclusive;
                    _currentSource = _inconclusive;
                    break;
                }
                case Category.All:
                {
                    _currentData = _filteredAll;
                    _currentSource = _allResults;
                    break;
                }
            }
            UpdateFilter(_testFilter, true);
        }

        private void HandleCategorySelection(ChangeEvent<Enum> evt)
        {
            SetCategorySelection((Category) evt.newValue);
        }

        private static Color GetDefaultHighlightColor()
        {
            var c = (EditorGUIUtility.isProSkin ? 42 : 240) / 255f;
            return new Color(c, c, c);
        }

        private EnumField _categorySelection;
        private void OnEnable()
        {
            _isRecordingNewBaseline = false;
            _isRecordingNewComparison = false;
            rootVisualElement.Add(new HelpBox(HelpText, HelpBoxMessageType.Info)
            {
                style =
                {
                    marginLeft = 3,
                    marginRight = 3,
                    marginBottom = 3,
                    marginTop = 3
                }
            });

            {
                var comparisonBar = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        marginLeft = 6,
                        marginRight = 6,
                    }
                };
                rootVisualElement.Add(comparisonBar);

                var baseLineBar = new VisualElement {style =
                {
                    flexGrow = 1,
                    flexBasis = 1,
                    flexDirection = FlexDirection.Column,
                    marginLeft = 3,
                    marginRight = 3
                }};
                baseLineBar.AddToClassList("unity-help-box");
                baseLineBar.Add(new Label
                {
                    text = "Baseline",
                    style =
                    {
                        color = BaselineColorText,
                        alignSelf = Align.FlexStart,
                        unityTextAlign = TextAnchor.MiddleLeft,
                        unityFontStyleAndWeight = FontStyle.Bold,
                    },
                    tooltip = "This is the baseline that any new results will be compared to."
                });
                baseLineBar.Add(_baselineOverviewLabel = new Label
                {
                    style =
                    {
                        alignSelf = Align.FlexStart,
                        whiteSpace = WhiteSpace.Normal,
                    }
                });
                baseLineBar.Add(new VisualElement { style = {flexGrow = 1}});
                baseLineBar.Add(_recordBaselineButton = new Button(ToggleBaselineRecording)
                {
                    text = "Record new baseline",
                    style =
                    {
                        alignSelf = Align.Stretch
                    },
                    tooltip = "Click this before running a set of performance tests to automatically set them as the new baseline."
                });
                comparisonBar.Add(baseLineBar);

                var newResultsBar = new VisualElement {
                    style =
                    {
                        flexGrow = 1,
                        flexBasis = 1,
                        flexDirection = FlexDirection.Column,
                        marginLeft = 3,
                        marginRight = 3
                    }
                };
                newResultsBar.AddToClassList("unity-help-box");
                newResultsBar.Add(new Label
                {
                    text = "New results",
                    style =
                    {
                        color = ResultsColorText,
                        alignSelf = Align.FlexStart,
                        unityTextAlign = TextAnchor.MiddleLeft,
                        unityFontStyleAndWeight = FontStyle.Bold
                    },
                    tooltip = "These are the results captured from the last time you ran performance tests."
                });
                newResultsBar.Add(_newResultsOverviewLabel = new Label
                {
                    style =
                    {
                        alignSelf = Align.FlexStart,
                        whiteSpace = WhiteSpace.Normal,
                    }
                });
                newResultsBar.Add(new VisualElement { style = {flexGrow = 1}});
                newResultsBar.Add(_recordComparisonButton = new Button(ToggleComparisonRecording)
                {
                    text = "Record new comparison data",
                    style = {
                        alignSelf = Align.Stretch
                    },
                    tooltip = "Click this before running a set of performance tests to automatically set them as the new comparison."
                });
                comparisonBar.Add(newResultsBar);
            }

            rootVisualElement.Add(new Label
            {
                text = "Results",
                style = {
                    fontSize = 12,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginTop = 15
                }
            });

            {
                var toolbar = new VisualElement {style = {height = 22, flexDirection = FlexDirection.Row}};
                var toolbarSearchField = new ToolbarSearchField();
                toolbarSearchField.RegisterValueChangedCallback(SearchFilterChanged);
                toolbar.Add(toolbarSearchField);

                toolbar.Add(new VisualElement {style = {flexGrow = 1}});

                var m = _categorySelection = new EnumField(_selectedCategory)
                {
                    style = { width = 110 }
                };
                _categorySelection.RegisterValueChangedCallback(HandleCategorySelection);
                toolbar.Add(m);

                toolbar.Add(new VisualElement {style = {width = 5}});

                _regressionCountLabel = new Label
                {
                    style = {unityTextAlign = TextAnchor.MiddleCenter, color = RegressionColor}
                };
                _regressionCountLabel.RegisterCallback<ClickEvent>(a=> { SetCategorySelection(Category.Regressions); });
                AddMouseOverHighlight(_regressionCountLabel);
                toolbar.Add(_regressionCountLabel);

                toolbar.Add(new Label(" | ") {style = {unityTextAlign = TextAnchor.MiddleCenter}});

                _improvementCountLabel = new Label
                {
                    style = {unityTextAlign = TextAnchor.MiddleCenter, color = ImprovementColor}
                };
                _improvementCountLabel.RegisterCallback<ClickEvent>(a=> { SetCategorySelection(Category.Improvements); });
                AddMouseOverHighlight(_improvementCountLabel);
                toolbar.Add(_improvementCountLabel);

                toolbar.Add(new Label(" | ") {style = {unityTextAlign = TextAnchor.MiddleCenter}});
                _inconclusiveCountLabel = new Label
                {
                    style = {unityTextAlign = TextAnchor.MiddleCenter}
                };
                _inconclusiveCountLabel.RegisterCallback<ClickEvent>(a=> { SetCategorySelection(Category.Inconclusive); });
                AddMouseOverHighlight(_inconclusiveCountLabel);
                toolbar.Add(_inconclusiveCountLabel);

                toolbar.Add(new Label(" | ") {style = {unityTextAlign = TextAnchor.MiddleCenter}});
                _totalCountLabel = new Label
                {
                    style = {unityTextAlign = TextAnchor.MiddleCenter}
                };
                _totalCountLabel.RegisterCallback<ClickEvent>(a=> { SetCategorySelection(Category.All); });
                AddMouseOverHighlight(_totalCountLabel);
                toolbar.Add(_totalCountLabel);

                void AddMouseOverHighlight(Label l)
                {
                    l.RegisterCallback<MouseEnterEvent>(a => { l.style.backgroundColor = GetDefaultHighlightColor(); });
                    l.RegisterCallback<MouseLeaveEvent>(a => { l.style.backgroundColor = StyleKeyword.Undefined; });
                }

                rootVisualElement.Add(toolbar);
            }

            {
                var list = new ListView(_filteredRegressions, 80, CreateListItem, BindListItem)
                {
                    selectionType = SelectionType.None
                };
                list.style.flexGrow = 1;
                rootVisualElement.Add(list);
                _resultsList = list;
            }

            {
                var bottomBar = new Toolbar
                {
                    style = {flexDirection = FlexDirection.RowReverse}
                };
                _dataStatusLabel = new Label
                {
                    style = {unityTextAlign = TextAnchor.MiddleRight},
                    text = "Test"
                };
                bottomBar.Add(_dataStatusLabel);
                rootVisualElement.Add(bottomBar);
            }

            var fullPath = Path.GetFullPath(ResultsPath);
            _fileWatcher?.Dispose();
            _fileWatcher = new FileSystemWatcher(Path.GetDirectoryName(fullPath), Path.GetFileName(fullPath))
            {
                IncludeSubdirectories = false,
            };
            _fileWatcher.Changed += TestResultsChanged;
            _fileWatcher.Created += TestResultsChanged;
            _fileWatcher.EnableRaisingEvents = true;

            _baselineData = null;
            SetCategorySelection(0);
            LoadTestResults();
        }

        private void OnDestroy()
        {
            _fileWatcher.Dispose();
        }

        private bool _reloadFiles;
        private void OnInspectorUpdate()
        {
            if (_reloadFiles)
            {
                lock (_fileWatcher)
                {
                    _reloadFiles = false;
                }
                LoadTestResults();
                Repaint();
            }
        }

        private void TestResultsChanged(object sender, FileSystemEventArgs e)
        {
            lock (_fileWatcher)
            {
                _reloadFiles = true;
            }
        }

        private static DateTime GetRecordingTime(Run r)
        {
            // For unknown reasons, the recording time is sometimes in milliseconds and sometimes in seconds.
            DateTimeOffset timeOffset;
            if (r.Date < -62135596800 || r.Date > 253402300799)
                timeOffset = DateTimeOffset.FromUnixTimeMilliseconds(r.Date);
            else
                timeOffset = DateTimeOffset.FromUnixTimeSeconds(r.Date);
            return timeOffset.DateTime.ToLocalTime();
        }

        private void UpdateData()
        {
            StopBaselineRecording();
            StopComparisonRecording();
            _allResults.Clear();
            _regressions.Clear();
            _improvements.Clear();
            _inconclusive.Clear();
            _filteredAll.Clear();
            _filteredRegressions.Clear();
            _filteredImprovements.Clear();
            _filteredInconclusive.Clear();

            if (_comparisonData != null && _baselineData != null)
            {
                CalculateRegressions(_baselineData, _comparisonData, _regressions, _improvements, _inconclusive);
                _regressions.Sort((lhs, rhs) => rhs.UpperChangePercentage.CompareTo(lhs.UpperChangePercentage));
                _improvements.Sort((lhs, rhs) => lhs.UpperChangePercentage.CompareTo(rhs.UpperChangePercentage));
                _allResults.AddRange(_regressions);
                _allResults.AddRange(_improvements);
                _allResults.AddRange(_inconclusive);
                _allResults.Sort((lhs, rhs) => String.Compare(lhs.Name, rhs.Name, StringComparison.OrdinalIgnoreCase));

                _dataStatusLabel.text = $"Baseline from {GetRecordingTime(_baselineData)}, results from {GetRecordingTime(_comparisonData)}";
            }
            else if (_comparisonData != null)
            {
                _dataStatusLabel.text = "Set the current data as a baseline and run another test";
            }
            else
            {
                _dataStatusLabel.text = "No performance data loaded - run performance tests to get started";
            }

            if (_baselineData != null)
                _baselineOverviewLabel.text = FormatResults(_baselineData);
            else
                _baselineOverviewLabel.text = "No baseline recorded! Record one to get started. Press the button below, then run some performance tests.";
            if (_comparisonData != null)
                _newResultsOverviewLabel.text = FormatResults(_comparisonData);
            else
                _newResultsOverviewLabel.text = "No comparison results recorded! Record some to compare to the baseline. Press the button below, then run some performance tests.";

            string FormatResults(Run r) =>
                $"{r.Results.Count.ToString()} entries\n{r.Player.Platform}/{r.Player.ScriptingBackend}\n{GetRecordingTime(r)}";

            UpdateFilter(_testFilter, true);

            _regressionCountLabel.text = $"> {_regressions.Count}";
            _regressionCountLabel.tooltip = $"{_regressions.Count} regressions";
            _improvementCountLabel.text = $"< {_improvements.Count}";
            _improvementCountLabel.tooltip = $"{_improvements.Count} improvements";
            _inconclusiveCountLabel.text = $"~ {_inconclusive.Count}";
            _inconclusiveCountLabel.tooltip = $"{_inconclusive.Count} inconclusive";
            _totalCountLabel.text = $"{_allResults.Count} total";
            _totalCountLabel.tooltip = $"{_allResults.Count} total";
        }

        private void LoadTestResults()
        {
            _tmpResults = LoadResults(ResultsPath);
            if (_isRecordingNewBaseline)
            {
                _baselineData = _tmpResults;
                var json = JsonConvert.SerializeObject(_baselineData);
                File.WriteAllText(BaselineDataPath, json);
                _comparisonData = LoadResults(ComparisonDataPath);
            }
            else if (_isRecordingNewComparison)
            {
                _comparisonData = _tmpResults;
                var json = JsonConvert.SerializeObject(_comparisonData);
                File.WriteAllText(ComparisonDataPath, json);
                _baselineData = LoadResults(BaselineDataPath);
            }
            else
            {
                _comparisonData = LoadResults(ComparisonDataPath);
                _baselineData = LoadResults(BaselineDataPath);
            }

            UpdateData();
        }

        // Changes that are below this threshold will be ignored
        private const double PercentageCutOffRegressionMin = 0.01;
        private const double PercentageCutOffRegressionMax = 0.1;
        private const double PercentageCutOffImprovement = 0.1;

        private static void CalculateRegressions(Run oldRun, Run newRun,
            List<ChangeResult> regressions,
            List<ChangeResult> improvements,
            List<ChangeResult> inconclusive)
        {
            var newResults = newRun.Results.ToDictionary(r => r.Name);
            foreach (var oldResult in oldRun.Results)
            {
                if (!newResults.TryGetValue(oldResult.Name, out var newResult))
                    continue;

                if (newResult.SampleGroups.Count != oldResult.SampleGroups.Count)
                {
                    Debug.LogWarning($"The sample groups for test {oldResult.Name} do not match between the two runs. This is not supported.");
                    continue;
                }

                int n = oldResult.SampleGroups.Count;
                for (int i = 0; i < n; i++)
                {
                    var newSamples = newResult.SampleGroups[i];
                    var oldSamples = oldResult.SampleGroups[i];
                    if (newSamples.Name != oldSamples.Name || newSamples.IncreaseIsBetter != oldSamples.IncreaseIsBetter)
                    {
                        Debug.LogWarning($"The sample groups for test {oldResult.Name} do not match between the two runs. This is not supported.");
                        continue;
                    }

                    var testName = oldResult.Name;
                    if (n > 1)
                        testName += "_" + oldSamples.Name;
                    HandleSampleGroups(testName, newSamples, oldSamples, regressions, improvements, inconclusive);
                }
            }
        }

        private static void HandleSampleGroups(string testName, SampleGroup newSamples, SampleGroup oldSamples,
            List<ChangeResult> regressions, List<ChangeResult> improvements, List<ChangeResult> inconclusive)
        {
            var samplesA = new List<double>();
            var samplesB = new List<double>();
            Debug.Assert(newSamples.Unit == oldSamples.Unit, "The two samples are using different units. You are the unlucky person to hit this case, so please add support for it.");

            var allSamples = newSamples.Samples.Concat(oldSamples.Samples);
            var c = new ChangeResult
            {
                Name = testName,
                Histogram = new HistogramData
                {
                    X = newSamples,
                    Y = oldSamples,
                    Max = allSamples.Max(),
                    Min = allSamples.Min()
                }
            };

            if (newSamples.Samples.Count <= 1 || oldSamples.Samples.Count <= 1)
            {
                inconclusive.Add(c);
                c.Test.p = 1;
                c.Test.IntervalCenter = 0;
                c.Test.IntervalHalfWidth = double.PositiveInfinity;
                return;
            }

            samplesA.Clear();
            samplesA.AddRange(newSamples.Samples);
            samplesB.Clear();
            samplesB.AddRange(oldSamples.Samples);
            StatisticsUtility.RemoveTopOutliers(samplesA);
            StatisticsUtility.RemoveTopOutliers(samplesB);
            var t = StatisticsUtility.WelchTTest(samplesA, samplesB, StatisticsUtility.ConfidenceLevel.Alpha1);
            c.Test = t;


            // insignificant or straddling zero?
            if (t.p > 0.05 || t.IntervalLower < 0 && 0 < t.IntervalUpper)
            {
                inconclusive.Add(c);
                return;
            }

            Debug.Assert(newSamples.IncreaseIsBetter == oldSamples.IncreaseIsBetter, $"The test {testName} was modified between runs, please collect a new baseline.");
            bool increaseIsBetter = newSamples.IncreaseIsBetter;
            double testValue = increaseIsBetter ? -t.IntervalLower : t.IntervalLower;
            var interval = (c.LowerChangePercentage, c.UpperChangePercentage);
            if (increaseIsBetter)
            {
                interval.LowerChangePercentage = -interval.LowerChangePercentage;
                interval.UpperChangePercentage = -interval.UpperChangePercentage;
            }

            if (testValue > 0)
            {
                // Check that the minimum change is at least the given percentage OR
                // the maximum change is above a certain threshold
                if (interval.LowerChangePercentage >= PercentageCutOffRegressionMin || interval.UpperChangePercentage >= PercentageCutOffRegressionMax)
                {
                    // regression
                    regressions.Add(c);
                    return;
                }
            }
            else // t.IntervalUpper < 0
            {
                // Check that the minimum improvement is at least a given value
                if (-interval.UpperChangePercentage >= PercentageCutOffImprovement)
                {
                    // improvement
                    improvements.Add(c);
                    return;
                }
            }
            inconclusive.Add(c);
        }

        private static Run LoadResults(string filePath)
        {
            if (!File.Exists(filePath))
                return null;
            var json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<Run>(json);
        }

        private void CopyChangesAsMarkdown() => MarkdownExportUtility.CopyAsMarkdown(_baselineData, _comparisonData, _regressions, _improvements, null);
        private void CopyAllAsMarkdown() => MarkdownExportUtility.CopyAsMarkdown(_baselineData, _comparisonData, _regressions, _improvements, _inconclusive);
        private void CopyComparisonAsCsv() => CsvExportUtility.CopyComparisonCsv(_allResults);
        private void CopyNewResultsAsCsv() => CsvExportUtility.CopyNewResultsCsv(_allResults);

        private void DropBaselineResults()
        {
            _baselineData = null;
            try
            {
                File.Delete(BaselineDataPath);
            }
            finally
            {
                UpdateData();
            }
        }

        private void DropComparisonResults()
        {
            _comparisonData = null;
            try
            {
                File.Delete(ComparisonDataPath);
            }
            finally
            {
                UpdateData();
            }
        }

        private void OpenReadMe()
        {
            var assetPath = AssetDatabase.GetAssetPath(_readMeAsset);
            var path = Path.GetFullPath(assetPath);
            EditorUtility.OpenWithDefaultApp(path);
        }

        void IHasCustomMenu.AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Open ReadMe"), false, OpenReadMe);
            menu.AddItem(new GUIContent("Copy performance changes as Markdown"), false, CopyChangesAsMarkdown);
            menu.AddItem(new GUIContent("Copy all results as Markdown"), false, CopyAllAsMarkdown);
            menu.AddItem(new GUIContent("Copy comparison data as CSV"), false, CopyComparisonAsCsv);
            menu.AddItem(new GUIContent("Copy new results as CSV"), false, CopyNewResultsAsCsv);
            menu.AddItem(new GUIContent("Drop baseline data"), false, DropBaselineResults);
            menu.AddItem(new GUIContent("Drop comparison data"), false, DropComparisonResults);
        }

        static class CsvExportUtility
        {
            const char Separator = ';';
            static void FormatSample(StringBuilder sb, SampleGroup sample)
            {
                sb.Append(sample.Average.ToString(CultureInfo.InvariantCulture));
                sb.Append(Separator);
                sb.Append(sample.Median.ToString(CultureInfo.InvariantCulture));
                sb.Append(Separator);
                sb.Append(sample.StandardDeviation.ToString(CultureInfo.InvariantCulture));
                sb.Append(Separator);
                sb.Append(sample.Samples.Count.ToString(CultureInfo.InvariantCulture));
            }

            static IEnumerable<string> ComparisonCsv(List<ChangeResult> results)
            {
                yield return $"Name{Separator}MeanBase{Separator}MedianBase{Separator}StdDevBase{Separator}CountBase{Separator}Mean{Separator}Median{Separator}StdDev{Separator}Count";
                var sb = new StringBuilder();
                for (int i = 0; i < results.Count; i++)
                {
                    var r = results[i];
                    sb.Append(r.Name);
                    sb.Append(Separator);
                    FormatSample(sb, r.Histogram.Y);
                    sb.Append(Separator);
                    FormatSample(sb, r.Histogram.X);
                    yield return sb.ToString();
                    sb.Clear();
                }
            }

            static IEnumerable<string> ResultsCsv(List<ChangeResult> results)
            {
                yield return $"Name{Separator}Mean{Separator}Median{Separator}StdDev{Separator}Count";
                var sb = new StringBuilder();
                for (int i = 0; i < results.Count; i++)
                {
                    var r = results[i];
                    sb.Append(r.Name);
                    sb.Append(Separator);
                    FormatSample(sb, r.Histogram.X);
                    yield return sb.ToString();
                    sb.Clear();
                }
            }

            public static void CopyComparisonCsv(List<ChangeResult> results) =>
                EditorGUIUtility.systemCopyBuffer = string.Join("\n", ComparisonCsv(results));

            public static void CopyNewResultsCsv(List<ChangeResult> results) =>
                EditorGUIUtility.systemCopyBuffer = string.Join("\n", ResultsCsv(results));

            static IEnumerable<string> SampleData(SampleGroup newData, SampleGroup oldData)
            {
                yield return $"group{Separator}value";
                foreach (var v in oldData.Samples)
                    yield return $"old{Separator}" + v.ToString(CultureInfo.InvariantCulture);
                foreach (var v in newData.Samples)
                    yield return $"new{Separator}" + v.ToString(CultureInfo.InvariantCulture);
            }
            public static void CopySamplesCsv(SampleGroup newData, SampleGroup oldData) =>
                EditorGUIUtility.systemCopyBuffer = string.Join("\n", SampleData(newData, oldData));
        }

        static class MarkdownExportUtility
        {
            struct TableHeader
            {
                public string Title;
                public bool RightAligned;
            }

            static IEnumerable<string> Table<T>(TableHeader[] header, IEnumerable<T> data) where T : IEnumerable<string>
            {
                var max = new int[header.Length];
                for (int i = 0; i < header.Length; i++)
                    max[i] = header[i].Title.Length;
                foreach (var row in data)
                {
                    int idx = 0;
                    foreach (var field in row)
                    {
                        if (field.Length > max[idx])
                            max[idx] = field.Length;
                        idx++;
                    }
                }

                var sb = new StringBuilder();
                {
                    sb.Append("|");
                    for (int i = 0; i < header.Length; i++)
                    {
                        sb.Append(' ');
                        sb.Append(header[i].Title.PadRight(max[i]));
                        sb.Append(" |");
                    }
                    yield return sb.ToString();
                }

                {
                    sb.Clear();
                    sb.Append("|");
                    for (int i = 0; i < header.Length; i++)
                    {
                        if (!header[i].RightAligned)
                            sb.Append(':');
                        sb.Append('-', max[i] + 1);
                        if (header[i].RightAligned)
                            sb.Append(':');
                        sb.Append('|');
                    }

                    yield return sb.ToString();
                }

                foreach (var row in data)
                {
                    sb.Clear();
                    sb.Append("|");
                    int idx = 0;
                    foreach (var field in row)
                    {
                        sb.Append(' ');
                        sb.Append(field.PadRight(max[idx]));
                        sb.Append(" |");
                        idx++;
                    }

                    yield return sb.ToString();
                }
            }

            static IEnumerable<string> Row(ChangeResult result)
            {
                yield return CleanupTestName(result.Name);
                yield return FormatAbsoluteInterval(result);
                yield return FormatRelativeInterval(result);
            }

            private static readonly TableHeader[] ResultsTableHeader =
            {
                new TableHeader {Title = "Test"},
                new TableHeader {Title = "95% CI (abs. change in mean)", RightAligned = true},
                new TableHeader {Title = "95% CI (rel. change in mean)", RightAligned = true},
            };
            static IEnumerable<string> ResultsTable(IEnumerable<ChangeResult> changes)
            {
                return Table(ResultsTableHeader, changes.Select(r => Row(r).ToList()));
            }

            private static readonly TableHeader[] SpecsTableHeader =
            {
                new TableHeader {Title = "Spec"},
                new TableHeader {Title = "Value"}
            };

            private static string GetVersionAndDate(Run r) =>
                $"recorded at {GetRecordingTime(r)} on {r.Player.Platform}/{r.Player.ScriptingBackend} / {r.Editor.Version} ({r.Editor.Branch}, {r.Editor.Changeset})";

            private static IEnumerable<IEnumerable<string>> SpecTable(Run baseline, Run newResults)
            {
                yield return new[] {"Baseline", GetVersionAndDate(baseline)};
                yield return new[] {"New Results", GetVersionAndDate(newResults)};
            }

            readonly struct DetailsScope : IDisposable
            {
                private readonly StringBuilder _sb;
                private readonly bool _isActive;
                public DetailsScope(StringBuilder sb, bool isActive)
                {
                    _sb = sb;
                    _isActive = isActive;
                    if (isActive)
                    {
                        _sb.AppendLine("<details>");
                        _sb.AppendLine();
                    }
                }

                public void Dispose()
                {
                    if (_isActive)
                    {
                        _sb.AppendLine();
                        _sb.AppendLine("</details>");
                    }
                }
            }

            public static void CopyAsMarkdown(Run baseline, Run newResults, List<ChangeResult> regressions, List<ChangeResult> improvements, List<ChangeResult> inconclusive)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("### Performance testing results");
                foreach (var line in Table(SpecsTableHeader, SpecTable(baseline, newResults)))
                    sb.AppendLine(line);

                sb.AppendLine();
                sb.AppendLine("Find guidance about this tool [here](https://github.com/Unity-Technologies/dots/blob/master/Samples/Assets/Editor/PerformanceTestViewer/ReadMe.md).");
                sb.AppendLine();
                sb.AppendLine("#### Regressions");
                sb.AppendLine($"found {regressions.Count} regressions");
                if (regressions.Count > 0)
                {
                    sb.AppendLine();
                    using (new DetailsScope(sb, regressions.Count > 10))
                    {
                        foreach (var line in ResultsTable(regressions))
                            sb.AppendLine(line);
                    }
                }

                sb.AppendLine();

                sb.AppendLine("#### Improvements");
                sb.AppendLine($"found {improvements.Count} improvements");

                if (improvements.Count > 0)
                {
                    sb.AppendLine();
                    using (new DetailsScope(sb, improvements.Count > 10))
                    {
                        foreach (var line in ResultsTable(improvements))
                            sb.AppendLine(line);
                    }
                }

                if (inconclusive != null)
                {
                    sb.AppendLine();

                    sb.AppendLine("#### Inconclusive results");
                    sb.AppendLine($"found {inconclusive.Count} tests without significant changes");

                    if (inconclusive.Count > 0)
                    {
                        sb.AppendLine();
                        using (new DetailsScope(sb, inconclusive.Count > 10))
                        {
                            foreach (var line in ResultsTable(inconclusive))
                                sb.AppendLine(line);
                        }
                    }
                }

                EditorGUIUtility.systemCopyBuffer = sb.ToString();
            }
        }
    }
}
