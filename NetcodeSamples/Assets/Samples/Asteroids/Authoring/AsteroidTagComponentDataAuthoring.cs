using Unity.Entities;
using UnityEngine;

[DisallowMultipleComponent]
public class AsteroidTagComponentDataAuthoring : MonoBehaviour
{
    class AsteroidTagComponentDataBaker : Baker<AsteroidTagComponentDataAuthoring>
    {
        public override void Bake(AsteroidTagComponentDataAuthoring authoring)
        {
            AsteroidTagComponentData component = default(AsteroidTagComponentData);
            AddComponent(component);
        }
    }
}
