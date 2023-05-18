using Unity.Entities;
using UnityEngine;

public class VelocityAuthoringComponent : MonoBehaviour
{
    public class Baker : Baker<VelocityAuthoringComponent>
    {
        public override void Bake(VelocityAuthoringComponent authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Velocity());
        }
    }
}
