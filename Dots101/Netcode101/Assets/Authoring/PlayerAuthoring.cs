using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Rendering;
using UnityEngine;

namespace KickBall
{
    public class PlayerAuthoring : MonoBehaviour
    {
        class Baker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
                AddComponent<Player>(entity);
                AddComponent<Color>(entity);
            }
        }
    }

    public struct Player : IComponentData
    {
    }

    // this attribute means the value will be passed to _BaseColor of the shader
    [MaterialProperty("_BaseColor")]
    public struct Color : IComponentData
    {
        [GhostField] public float4 Value;
    }
}
