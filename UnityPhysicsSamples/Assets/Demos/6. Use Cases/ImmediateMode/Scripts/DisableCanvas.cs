using UnityEngine;

public class DisableCanvas : MonoBehaviour {

    public GameObject scriptNeedingUI_GO;
    
	// Use this for initialization
	void Start ()
    {
		if( scriptNeedingUI_GO )
        {
            var component = scriptNeedingUI_GO.GetComponent("ProjectIntoFutureOnCue");
            gameObject.SetActive( component && (component as MonoBehaviour).enabled );
        }
	}
	
	// Update is called once per frame
	void Update ()
    {
		
	}
}
