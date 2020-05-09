using Unity.Entities;
using Unity.Transforms;

// ReSharper disable once InconsistentNaming
public class MoveUpSystem_HybridComponent : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.WithAll<MoveUp_HybridComponent>().ForEach((ref Translation translation) =>
        {
            translation.Value.y += Time.DeltaTime;
        });
    }
}
