using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModeText : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var mode = SimulationMode.getCurrentMode();
        
        if (mode.type == SimulationMode.ModeType.None) GetComponent<TextMesh>().text = "MODE: Nothing";
        if (mode.type == SimulationMode.ModeType.Color) GetComponent<TextMesh>().text = "MODE: Update Color";
        if (mode.type == SimulationMode.ModeType.Position) GetComponent<TextMesh>().text = "MODE: Update Position";
        if (mode.type == SimulationMode.ModeType.PositionAndColor) GetComponent<TextMesh>().text = "MODE: Update Color and Position";
    }
}
