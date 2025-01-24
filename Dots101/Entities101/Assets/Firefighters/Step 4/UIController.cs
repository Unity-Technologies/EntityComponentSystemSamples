using UnityEngine;
using UnityEngine.UIElements;

public class UIController : MonoBehaviour
{
    private Label dousedLabel;
    private Button repositionButton;

    private bool reposition = false; 
    
    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        dousedLabel = root.Q<Label>();
        repositionButton = root.Q<Button>();

        repositionButton.clicked += OnRepositionButton;
    }

    private void OnRepositionButton()
    {
        reposition = true;
    }

    public bool ShouldReposition()
    {
        var temp = reposition;
        reposition = false;
        return temp;
    }

    public void SetNumFiresDoused(int numFiresDoused)
    {
        dousedLabel.text = $"Number of fires doused: {numFiresDoused}";
    }
}
