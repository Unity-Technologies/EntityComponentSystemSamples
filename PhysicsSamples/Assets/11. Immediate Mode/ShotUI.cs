using Unity.Mathematics;
using UnityEngine;
using Slider = UnityEngine.UI.Slider;

namespace ImmediateMode
{
    public class ShotUI : MonoBehaviour
    {
        public Slider RotateSlider;
        public Slider StrengthSlider;

        public static ShotUI Singleton;

        private float3 sliderVelocity;
        private bool velocityChanged = false;
        private bool clicked = false;

        void Start()
        {
            Singleton = this;
            OnSliderValueChanged();
        }

        public bool GetClick()
        {
            var temp = clicked;
            clicked = false;
            return temp;
        }

        public bool GetSliderVelocity(out float3 sliderVelocity)
        {
            sliderVelocity = this.sliderVelocity;
            var temp = velocityChanged;
            velocityChanged = false;
            return temp;
        }

        public void OnSliderValueChanged()
        {
            float angle = RotateSlider.value - 90;
            float strength = StrengthSlider.value;
            sliderVelocity = strength * math.forward(quaternion.AxisAngle(math.up(), math.radians(angle)));
            velocityChanged = true;
        }

        public void OnButtonClick()
        {
            clicked = true;
        }
    }
}
