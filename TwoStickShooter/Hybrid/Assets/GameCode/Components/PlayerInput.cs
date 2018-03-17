using Unity.Mathematics;
using UnityEngine;

namespace TwoStickHybridExample
{

    public class PlayerInput : MonoBehaviour
    {
        [HideInInspector] public float2 Move;
        [HideInInspector] public float2 Shoot;
        [HideInInspector] public float FireCooldown;

        public bool Fire => FireCooldown <= 0.0 && math.length(Shoot) > 0.5f;
    }
}
