using UnityEngine;
using Unity.Entities;
using UnityEngine.UI;

public class ScoreTrackerAuthoring : MonoBehaviour
{
    class Baker : Baker<ScoreTrackerAuthoring>
    {
        public override void Bake(ScoreTrackerAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.ManualOverride); // So it's not included in the relevancy radius check, as we default Score to always be relevant by default.
            AddComponent(entity, new AsteroidScore());
        }
    }
}
