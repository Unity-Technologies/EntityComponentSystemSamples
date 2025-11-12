using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.DotsUISample
{
    //  Shows the quest title and the collectables required to complete the quest
    public class QuestScreen : UIScreen
    {
        QuestData m_QuestData;
        VisualElement m_ChecklistPanel;
        Label[] m_CollectableLabels;

        const string k_QuestTitleUssClassName = "quest-title";
        const string k_QuestCollectableUssClassName = "quest-collectable";
        const string k_QuestCollectableCompletedUssClassName = "quest-collectable-completed";
        
        public static QuestScreen Instantiate(VisualElement parentElement)
        {
            var screen = ScriptableObject.CreateInstance<QuestScreen>();
            screen.RootElement = parentElement;
            
            screen.m_ChecklistPanel = screen.RootElement.Q<VisualElement>("quest__checklist-panel");
            
            screen.Hide();
            return screen;
        }
        
        public void SetQuestData(QuestData questData, DynamicBuffer<CollectableCount> buf, CollectablesData collectables)
        {
            m_QuestData = questData;
            Label titleLabel = new Label(this.m_QuestData.Title.ToUpper());
            titleLabel.AddToClassList(k_QuestTitleUssClassName);
            m_ChecklistPanel.Add(titleLabel);
            m_CollectableLabels = new Label[this.m_QuestData.Items.Length];
            for (int i = 0; i < this.m_QuestData.Items.Length; i++)
            {
                m_CollectableLabels[i] = new Label();
                m_CollectableLabels[i].AddToClassList(k_QuestCollectableUssClassName);
                m_ChecklistPanel.Add(m_CollectableLabels[i]);
            }
            
            UpdateMessage(buf, collectables, false);

            Show();
        }

        // todo ideally we would avoid / minimize string allocations
        public void UpdateMessage(DynamicBuffer<CollectableCount> buf, CollectablesData collectables, bool hasAllItems)
        {
            if (hasAllItems)
            {
                m_CollectableLabels[0].text = m_QuestData.CompletionText.ToUpper();
                m_CollectableLabels[0].RemoveFromClassList(k_QuestCollectableCompletedUssClassName);
                for (int i = 1; i < m_CollectableLabels.Length; i++)
                {
                    m_CollectableLabels[i].text = "";
                }

                return;
            }
            
            for (int i = 0; i < m_QuestData.Items.Length; i++)
            {
                var collectableName = collectables.Collectables[i].Name.ToUpper();   
                var currentCount = buf[i].Count;
                var targetCount = m_QuestData.Items[i].GoalCount;
                
                m_CollectableLabels[i].text = $"{collectableName}  ({currentCount}/{targetCount})";
                
                if (currentCount >= targetCount)
                {
                    m_CollectableLabels[i].AddToClassList(k_QuestCollectableCompletedUssClassName);
                }
            }
        }
    }
}