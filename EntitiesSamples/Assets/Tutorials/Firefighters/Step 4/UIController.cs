using UnityEngine;
using UnityEngine.UIElements;

public class UIController : MonoBehaviour
{
    private Label dousedLabel;
    
    private void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        dousedLabel = root.Q<Label>();
    }

    public void SetNumFiresDoused(int numFiresDoused)
    {
        dousedLabel.text = $"Number of fires doused: {numFiresDoused}";
    }
}
