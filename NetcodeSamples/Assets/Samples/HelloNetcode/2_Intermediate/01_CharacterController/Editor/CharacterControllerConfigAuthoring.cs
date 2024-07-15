using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Samples.HelloNetcode
{
    public class CharacterControllerConfigAuthoring : MonoBehaviour
    {
        [FormerlySerializedAs("Speed")] public float MoveSpeed = 5;
        public float JumpSpeed = 5;
        public float Gravity = 9.82f;
    }

    class CharacterControllerConfigBaker : Baker<CharacterControllerConfigAuthoring>
    {
        public override void Bake(CharacterControllerConfigAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new CharacterControllerConfig{MoveSpeed = authoring.MoveSpeed, JumpSpeed = authoring.JumpSpeed, Gravity = authoring.Gravity});
        }
    }
}
