using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.DotsUISample
{
    //  Shows the quest title and the collectables required to complete the quest
    public class QuestScreen : UIScreen
    {
        private QuestData questData;
        private VisualElement checklistPanel;
        private Label[] collectableLabels;

        private const string k_QuestTitleUssClassName = "quest-title";
        private const string k_QuestCollectableUssClassName = "quest-collectable";
        private const string k_QuestCollectableCompletedUssClassName = "quest-collectable-completed";
        
        public static QuestScreen Instantiate(VisualElement parentElement)
        {
            var screen = ScriptableObject.CreateInstance<QuestScreen>();
            screen.RootElement = parentElement;
            
            screen.checklistPanel = screen.RootElement.Q<VisualElement>("quest__checklist-panel");
            
            screen.Hide();
            return screen;
        }
        
        public void SetQuestData(QuestData questData, DynamicBuffer<CollectableCount> buf, CollectablesData collectables)
        {
            this.questData = questData;
            Label titleLabel = new Label(this.questData.Title.ToUpper());
            titleLabel.AddToClassList(k_QuestTitleUssClassName);
            checklistPanel.Add(titleLabel);
            collectableLabels = new Label[this.questData.Items.Length];
            for (int i = 0; i < this.questData.Items.Length; i++)
            {
                collectableLabels[i] = new Label();
                collectableLabels[i].AddToClassList(k_QuestCollectableUssClassName);
                checklistPanel.Add(collectableLabels[i]);
            }
            
            UpdateMessage(buf, collectables, false);

            Show();
        }

        // todo ideally we would avoid / minimize string allocations
        public void UpdateMessage(DynamicBuffer<CollectableCount> buf, CollectablesData collectables, bool hasAllItems)
        {
            if (hasAllItems)
            {
                collectableLabels[0].text = questData.CompletionText.ToUpper();
                collectableLabels[0].RemoveFromClassList(k_QuestCollectableCompletedUssClassName);
                for (int i = 1; i < collectableLabels.Length; i++)
                {
                    collectableLabels[i].text = "";
                }

                return;
            }
            
            for (int i = 0; i < questData.Items.Length; i++)
            {
                var name = collectables.Collectables[i].Name.ToUpper();   
                var currentCount = buf[i].Count;
                var targetCount = questData.Items[i].GoalCount;
                
                collectableLabels[i].text = $"{name}  ({currentCount}/{targetCount})";
                
                if (currentCount >= targetCount)
                {
                    collectableLabels[i].AddToClassList(k_QuestCollectableCompletedUssClassName);
                }
            }
        }
    }
}