// Used in: 5. Modify/5m. Collider Modifications/ for the Tile prefab (which is used in ModifyGridSpawner)
// The purpose of this component is to keep track of trigger events on a single tile. These trigger events are evaluated
// in the TileTriggerSystem. Since we don't want to spawn infinite colliders (the later reaction to the trigger event),
// we limit the number of times a tile trigger can spawn a collider (MaxTriggerCount).
using Unity.Entities;
using UnityEngine;

public struct TileTriggerCounter : IComponentData
{
    public int MaxTriggerCount;
    public int TriggerCount;
}

public class TileTriggerAuthoring : MonoBehaviour
{
    public int MaxTriggerCount = 3;
    public int TriggerCount = 0;

    class TileTriggerBaker : Baker<TileTriggerAuthoring>
    {
        public override void Bake(TileTriggerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new TileTriggerCounter()
            {
                MaxTriggerCount = authoring.MaxTriggerCount,
                TriggerCount = authoring.TriggerCount,
            });
        }
    }
}
