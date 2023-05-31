using UnityEngine;

namespace Streaming.RuntimeContentManager
{
    public class CameraMovement : MonoBehaviour
    {
        public float speed = 200;

        void Update()
        {
            Camera.main.transform.Translate(0, 0, Time.deltaTime * speed);
            if (Camera.main.transform.position.z > 800)
            {
                Camera.main.transform.position = new Vector3(0, 0, -800);
            }
        }
    }
}
