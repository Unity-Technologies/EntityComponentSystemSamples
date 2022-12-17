using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class ClientSettingsAuthoring : MonoBehaviour
{
    [RegisterBinding(typeof(ClientSettings), "predictionRadius")]
    public int predictionRadius;
    [RegisterBinding(typeof(ClientSettings), "predictionRadiusMargin")]
    public int predictionRadiusMargin;

    class ClientSettingsBaker : Baker<ClientSettingsAuthoring>
    {
        public override void Bake(ClientSettingsAuthoring authoring)
        {
            ClientSettings component = default(ClientSettings);
            component.predictionRadius = authoring.predictionRadius;
            component.predictionRadiusMargin = authoring.predictionRadiusMargin;
            AddComponent(component);
        }
    }
}
