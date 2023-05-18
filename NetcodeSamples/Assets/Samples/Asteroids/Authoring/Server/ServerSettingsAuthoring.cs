using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class ServerSettingsAuthoring : MonoBehaviour
{
    [RegisterBinding(typeof(ServerSettings), "levelData")]
    public LevelComponent levelData;

    class Baker : Baker<ServerSettingsAuthoring>
    {
        public override void Bake(ServerSettingsAuthoring authoring)
        {
            ServerSettings component = default(ServerSettings);
            component.levelData = authoring.levelData;
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, component);
        }
    }
}
