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
            AddComponent(new CharacterControllerPlayerInput());
            AddComponent(new Character{ControllerConfig = GetEntity(authoring.ControllerConfig.gameObject)});
        }
    }
}
