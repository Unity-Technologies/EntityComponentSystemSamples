using System.Collections;
using System.Collections.Generic;
using TwoStickClassicExample;
using UnityEngine;

namespace TwoStickClassicExample
{
    [RequireComponent(typeof(Transform2D))]
    public class MoveSpeed : MonoBehaviour
    {

        public float Speed;
        
        void Update ()
        {
            var transform = GetComponent<Transform2D>();
            transform.Position += transform.Heading * Speed * Time.deltaTime;
        }
    }
}