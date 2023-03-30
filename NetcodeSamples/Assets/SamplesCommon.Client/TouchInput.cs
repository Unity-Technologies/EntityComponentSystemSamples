using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Mathematics;

namespace Unity.NetCode.Samples.Common
{
    public class TouchInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public enum KeyCode
        {
            Left = 0,
            Right,
            Up,
            Down,
            Space,
            LeftStick,
            RightStick,
            NumKeys
        }
        public enum StickCode
        {
            LeftStick = 0,
            RightStick,
            NumSticks
        }

        private static bool[] ActiveKeys = new bool[(int) KeyCode.NumKeys];
        private static float2[] StickOffsets = new float2[(int) StickCode.NumSticks];

        public KeyCode Key;
        StickCode Stick;

        public static bool GetKey(KeyCode code)
        {
            return ActiveKeys[(int) code];
        }
        public static float2 GetStick(StickCode code)
        {
            return StickOffsets[(int) code];
        }

        void Start()
        {
#if !UNITY_ANDROID && !UNITY_IOS
            gameObject.SetActive(false);
#endif
            ActiveKeys[(int) Key] = false;
            Stick = StickCode.NumSticks;
            if (Key == KeyCode.LeftStick)
                Stick = StickCode.LeftStick;
            if (Key == KeyCode.RightStick)
                Stick = StickCode.RightStick;
            if (Stick != StickCode.NumSticks)
                StickOffsets[(int)Stick] = new float2(0,0);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            ActiveKeys[(int) Key] = true;
            if (Stick != StickCode.NumSticks)
            {
                var child = gameObject.transform.GetChild(0).gameObject;
                var rectTrans = child.transform as RectTransform;

                rectTrans.position = eventData.position;
                child.SetActive(true);
                eventData.dragging = true;
                OnDrag(eventData);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ActiveKeys[(int) Key] = false;
            if (Stick != StickCode.NumSticks)
                gameObject.transform.GetChild(0).gameObject.SetActive(false);
        }
        public void OnDrag(PointerEventData eventData)
        {
            // Only handle the drag logic on the left/right sticks (this is also called when slightly dragging on a button)
            if (Stick == StickCode.NumSticks)
                return;

            if (eventData.dragging)
            {
                var child = gameObject.transform.GetChild(0).gameObject;
                var rectTrans = child.transform as RectTransform;

                Vector3 delta = new Vector3(eventData.position.x, eventData.position.y, rectTrans.position.z) - rectTrans.position;

                var maxLength = rectTrans.sizeDelta.x/2;
                if (delta.sqrMagnitude > maxLength*maxLength)
                {
                    delta.Normalize();
                    delta *= maxLength;
                }

                var stick = child.transform.GetChild(0).gameObject;
                var stickRectTrans = stick.transform as RectTransform;

                stickRectTrans.localPosition = delta;
                StickOffsets[(int)Stick] = new float2(delta.x/maxLength,delta.y/maxLength);
            }
        }
    }
}
