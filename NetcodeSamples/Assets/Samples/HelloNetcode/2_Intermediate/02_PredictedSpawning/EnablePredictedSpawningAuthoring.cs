using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct EnablePredictedSpawning : IComponentData { }

    public class EnablePredictedSpawningAuthoring : MonoBehaviour
    {
        class Baker : Baker<EnablePredictedSpawningAuthoring>
        {
            public override void Bake(EnablePredictedSpawningAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<EnablePredictedSpawning>(entity);
            }
        }
    }
}
