using Unity.Entities;
using UnityEngine;

namespace Samples.PredictionSwitching
{
    [DisallowMultipleComponent]
    public class PredictionSwitchingInputAuthoring : MonoBehaviour
    {
        class PredictionSwitchingInputBaking : Baker<PredictionSwitchingInputAuthoring>
        {
            public override void Bake(PredictionSwitchingInputAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddBuffer<PredictionSwitchingInput>(entity);
            }
        }
    }
}
