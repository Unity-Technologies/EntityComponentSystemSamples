using UnityEngine;

public class DisableCanvas : MonoBehaviour
{
    public GameObject scriptNeedingUI_GO;

    void Start()
    {
        if (scriptNeedingUI_GO)
        {
            var component = scriptNeedingUI_GO.GetComponent("ProjectIntoFutureOnCue");
            gameObject.SetActive(component && (component as MonoBehaviour).enabled);
        }
    }
}
