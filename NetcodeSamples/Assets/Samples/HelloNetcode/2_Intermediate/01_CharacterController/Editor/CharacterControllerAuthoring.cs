using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public class CharacterControllerAuthoring : MonoBehaviour
    {
        public CharacterControllerConfigAuthoring ControllerConfig;
    }

    class CharacterControllerBaker : Baker<CharacterControllerAuthoring>
    {
        public override void Bake(CharacterControllerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new CharacterControllerPlayerInput());
            AddComponent(entity, new Character
            {
                ControllerConfig = GetEntity(authoring.ControllerConfig.gameObject, TransformUsageFlags.Dynamic)
            });
        }
    }
}
