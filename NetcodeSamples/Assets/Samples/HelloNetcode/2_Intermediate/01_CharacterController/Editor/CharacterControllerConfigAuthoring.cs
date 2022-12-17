using Unity.Entities;
using UnityEngine;
namespace Samples.HelloNetcode
{
    public class CharacterControllerConfigAuthoring : MonoBehaviour
    {
        public float Speed = 5;
        public float JumpSpeed = 5;
        public float Gravity = 9.82f;
    }

    class CharacterControllerConfigBaker : Baker<CharacterControllerConfigAuthoring>
    {
        public override void Bake(CharacterControllerConfigAuthoring authoring)
        {
            AddComponent(new CharacterControllerConfig{Speed = authoring.Speed, JumpSpeed = authoring.JumpSpeed, Gravity = authoring.Gravity});
        }
    }
}
