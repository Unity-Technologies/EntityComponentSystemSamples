using Unity.Entities;
using UnityEngine;

public class PlayerIdAuthoringComponent : MonoBehaviour
{
    public class Baker : Baker<PlayerIdAuthoringComponent>
    {
        public override void Bake(PlayerIdAuthoringComponent authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlayerIdComponentData());
        }
    }

}
