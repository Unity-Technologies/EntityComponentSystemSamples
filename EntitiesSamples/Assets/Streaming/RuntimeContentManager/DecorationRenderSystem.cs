using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Streaming.RuntimeContentManager
{
    // render entities that are loaded and should render
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial struct DecorationRenderSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (transform, dec) in
                     SystemAPI.Query<RefRW<LocalToWorld>, RefRO<DecorationVisualComponentData>>())
            {
                if (dec.ValueRO.loaded && dec.ValueRO.shouldRender)
                {
                    Graphics.DrawMesh(dec.ValueRO.mesh.Result, transform.ValueRO.Value, dec.ValueRO.material.Result, 0);
                }
            }
        }
    }
}
