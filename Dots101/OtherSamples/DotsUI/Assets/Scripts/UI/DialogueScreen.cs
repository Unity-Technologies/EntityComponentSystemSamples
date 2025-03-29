using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.DotsUISample
{
    //  displays Wizardo's dialogue
    public class DialogueScreen : UIScreen
    {
        private Label dialogueLabel;
        private Button acceptButton;
        private Button closeButton;

        private DialogueData dialogue;
        private int dialogueLineIdx;

        public bool isDone
        {
            get;
            private set;
        }

        public static DialogueScreen Instantiate(VisualElement parentElement)
        {
            var screen = ScriptableObject.CreateInstance<DialogueScreen>();
            screen.RootElement = parentElement;

            screen.dialogueLabel = screen.RootElement.Q<Label>("dialogue__text-label");
            screen.acceptButton = screen.RootElement.Q<Button>("dialogue__accept-button");
            screen.closeButton = screen.RootElement.Q<Button>("dialogue__close-button");
            
            screen.acceptButton.clicked += screen.NextLine;
            screen.closeButton.clicked += screen.Close;
            screen.RootElement.style.display = DisplayStyle.None;
            
            return screen;
        }

        public void SetDialogueData(DialogueData dialogue)
        {
            dialogueLineIdx = 0;
            isDone = false;
            this.dialogue = dialogue;
            dialogueLabel.text = dialogue.Lines[0];
            acceptButton.text = (dialogueLineIdx == dialogue.Lines.Length - 1) ? "FINISH" : "NEXT";
        }

        private void NextLine()
        {
            dialogueLineIdx++;
            if (dialogueLineIdx == dialogue.Lines.Length)
            {
                isDone = true;
                return;
            }
            acceptButton.text = (dialogueLineIdx == dialogue.Lines.Length - 1) ? "FINISH" : "NEXT";    
            dialogueLabel.text = dialogue.Lines[dialogueLineIdx];
        }

        private void Close()
        {
            isDone = true;
        }
    }
}