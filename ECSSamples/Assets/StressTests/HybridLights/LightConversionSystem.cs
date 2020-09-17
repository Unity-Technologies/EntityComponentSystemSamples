using UnityEngine;

public class LightConversionSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.WithAll<LightConversionOptIn>().ForEach((Light light) =>
        {
            AddHybridComponent(light);
        });
    }
}
