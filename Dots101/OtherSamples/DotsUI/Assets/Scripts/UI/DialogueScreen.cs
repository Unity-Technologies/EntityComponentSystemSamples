using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.DotsUISample
{
    //  displays Wizardo's dialogue
    public class DialogueScreen : UIScreen
    {
        Label m_DialogueLabel;
        Button m_AcceptButton;
        Button m_CloseButton;

        DialogueData m_Dialogue;
        int m_DialogueLineIdx;

        public bool isDone
        {
            get;
            private set;
        }

        public static DialogueScreen Instantiate(VisualElement parentElement)
        {
            var screen = ScriptableObject.CreateInstance<DialogueScreen>();
            screen.RootElement = parentElement;

            screen.m_DialogueLabel = screen.RootElement.Q<Label>("dialogue__text-label");
            screen.m_AcceptButton = screen.RootElement.Q<Button>("dialogue__accept-button");
            screen.m_CloseButton = screen.RootElement.Q<Button>("dialogue__close-button");
            
            screen.m_AcceptButton.clicked += screen.NextLine;
            screen.m_CloseButton.clicked += screen.Close;
            screen.RootElement.style.display = DisplayStyle.None;
            
            return screen;
        }

        public void SetDialogueData(DialogueData dialogue)
        {
            m_DialogueLineIdx = 0;
            isDone = false;
            m_Dialogue = dialogue;
            m_DialogueLabel.text = dialogue.Lines[0];
            m_AcceptButton.text = (m_DialogueLineIdx == dialogue.Lines.Length - 1) ? "FINISH" : "NEXT";
        }

        void NextLine()
        {
            m_DialogueLineIdx++;
            if (m_DialogueLineIdx == m_Dialogue.Lines.Length)
            {
                isDone = true;
                return;
            }
            m_AcceptButton.text = (m_DialogueLineIdx == m_Dialogue.Lines.Length - 1) ? "FINISH" : "NEXT";    
            m_DialogueLabel.text = m_Dialogue.Lines[m_DialogueLineIdx];
        }

        void Close()
        {
            isDone = true;
        }
    }
}