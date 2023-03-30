using UnityEngine;

namespace Tutorials.Jobs.Step1
{
    public class Target : MonoBehaviour
    {
        public Vector3 Direction;

        public void Update()
        {
            transform.localPosition += Direction * Time.deltaTime;
        }
    }
}