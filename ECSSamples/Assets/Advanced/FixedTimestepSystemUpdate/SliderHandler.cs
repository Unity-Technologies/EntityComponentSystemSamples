using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

public class SliderHandler : MonoBehaviour
{
    public Text sliderValueText;

    public void OnSliderChange()
    {
        float fixedFps = GetComponent<Slider>().value;
        var fixedSimulationGroup = World.DefaultGameObjectInjectionWorld?.GetExistingSystem<FixedStepSimulationSystemGroup>();
        if (fixedSimulationGroup != null)
        {
            // The group timestep can be set at runtime:
            fixedSimulationGroup.Timestep = 1.0f / fixedFps;
            // The current timestep can also be retrieved:
            sliderValueText.text = $"{(int) (1.0f / fixedSimulationGroup.Timestep)} updates/sec";
        }
    }
}
