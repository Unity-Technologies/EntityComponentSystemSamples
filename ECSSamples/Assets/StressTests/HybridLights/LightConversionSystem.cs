using UnityEngine;

public class LightConversionSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.WithAll<LightConversionOptIn>().ForEach((Light light) =>
        {
            var entity = GetPrimaryEntity(light);
            DstEntityManager.AddComponentObject(entity, light);
        });
    }
}
