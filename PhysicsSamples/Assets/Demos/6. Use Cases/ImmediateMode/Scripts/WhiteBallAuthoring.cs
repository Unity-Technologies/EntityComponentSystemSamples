using Unity.Entities;
using UnityEngine;

public struct WhiteBall : IComponentData {}

public class WhiteBallAuthoring : MonoBehaviour
{
    class WhiteBallAuthoringBaker : Baker<WhiteBallAuthoring>
    {
        public override void Bake(WhiteBallAuthoring authoring)
        {
            AddComponent<WhiteBall>();
        }
    }
}
