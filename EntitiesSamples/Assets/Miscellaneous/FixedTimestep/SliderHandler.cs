using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Miscellaneous.FixedTimestep
{
    public class SliderHandler : MonoBehaviour
    {
        public Text sliderValueText;

        public void OnSliderChange()
        {
            float fixedFps = GetComponent<Slider>().value;

            // WARNING: accessing World.DefaultGameObjectInjectionWorld is a broken pattern in non-trivial projects.
            // GameObject interaction with ECS should generally go in the other direction: rather than having
            // GameObjects access ECS data and code, ECS systems should access GameObjects.

            var fixedSimulationGroup = World.DefaultGameObjectInjectionWorld
                ?.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
            if (fixedSimulationGroup != null)
            {
                // The group timestep can be set at runtime:
                fixedSimulationGroup.Timestep = 1.0f / fixedFps;
                // The current timestep can also be retrieved:
                sliderValueText.text = $"{(int)(1.0f / fixedSimulationGroup.Timestep)} updates/sec";
            }
        }
    }
}
