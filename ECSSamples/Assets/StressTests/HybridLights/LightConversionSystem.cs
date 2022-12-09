using Unity.Entities;
using UnityEngine;

// TODO: Port sample
/*
public partial class LightConversionSystem : GameObjectConversionSystem
{
    protected override void OnUpdate()
    {
        Entities.WithAll<LightConversionOptIn>().ForEach((Light light) =>
        {
            var entity = GetPrimaryEntity(light);
            DstEntityManager.AddComponentObject(entity, light);
        }).WithStructuralChanges().Run();
    }
}
*/
