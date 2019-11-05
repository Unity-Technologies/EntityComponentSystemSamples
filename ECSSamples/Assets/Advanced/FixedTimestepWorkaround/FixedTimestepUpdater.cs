using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Samples.FixedTimestepSystem
{
    // NOTE: Updating a manually-created system in FixedUpdate() as demonstrated below
    // is intended as a short-term workaround; the entire `SimulationSystemGroup` will
    // eventually use a fixed timestep by default.
    [AddComponentMenu("DOTS Samples/FixedTimestepWorkaround/Fixed Timestep Updater")]
    public class FixedTimestepUpdater : MonoBehaviour
    {
        FixedRateSpawnerSystem spawnerSystem;
        public Slider fixedTimestepSlider;

        Text sliderLabelText;

        void Start()
        {
            sliderLabelText = fixedTimestepSlider.GetComponentInChildren<Text>();
        }

        void FixedUpdate()
        {
            if (spawnerSystem == null)
            {
                spawnerSystem = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<FixedRateSpawnerSystem>();
            }
            Time.fixedDeltaTime = fixedTimestepSlider.value;
            sliderLabelText.text = $"Fixed Timestep: {fixedTimestepSlider.value*1000} ms";
            spawnerSystem.Update();
        }
    }
}
