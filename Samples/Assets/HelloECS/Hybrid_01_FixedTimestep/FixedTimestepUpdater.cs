using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Samples.FixedTimestepSystem
{
    // NOTE: Updating a manually-created system in FixedUpdate() as demonstrated below
    // is intended as a short-term workaround; the entire `SimulationSystemGroup` will
    // eventually use a fixed timestep by default.
    public class FixedTimestepUpdater : MonoBehaviour
    {
        private FixedRateSpawnerSystem spawnerSystem;
        public Slider fixedTimestepSlider;

        private Text sliderLabelText;
        private void Start()
        {
            sliderLabelText = fixedTimestepSlider.GetComponentInChildren<Text>();
        }

        private void FixedUpdate()
        {
            if (spawnerSystem == null)
            {
                spawnerSystem = World.Active.GetOrCreateSystem<FixedRateSpawnerSystem>();
            }
            Time.fixedDeltaTime = fixedTimestepSlider.value;
            sliderLabelText.text = $"Fixed Timestep: {fixedTimestepSlider.value*1000} ms";
            spawnerSystem.Update();
        }
    }
}
