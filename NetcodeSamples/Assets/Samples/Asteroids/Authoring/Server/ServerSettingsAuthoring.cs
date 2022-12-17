#if UNITY_EDITOR
using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class ServerSettingsAuthoring : MonoBehaviour
{
    [RegisterBinding(typeof(ServerSettings), "levelData")]
    public LevelComponent levelData;

    class ServerSettingsBaker : Baker<ServerSettingsAuthoring>
    {
        public override void Bake(ServerSettingsAuthoring authoring)
        {
            ServerSettings component = default(ServerSettings);
            component.levelData = authoring.levelData;
            AddComponent(component);
        }
    }
}
#endif
