// NOTE: This is a per project ifdfef,
//       the samples in this project are run in both modes for testing purposes.
//       In a normal game project this ifdef is not required.

#if !UNITY_DISABLE_MANAGED_COMPONENTS
using Unity.Entities;
using UnityEngine;
using Unity.Transforms;

class SimpleMeshRenderingAuthoring : MonoBehaviour
{
    public Mesh Mesh = null;
    public Color Color = Color.white;

    class SimpleMeshRenderingAuthoringBaker : Baker<SimpleMeshRenderingAuthoring>
    {
        public override void Bake(SimpleMeshRenderingAuthoring authoring)
        {
            // Assets in subscenes can either be created during conversion and embedded in the scene
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = authoring.Color;
            // ... Or be an asset that is being referenced.

            AddComponentObject( new SimpleMeshRenderer
            {
                Mesh = authoring.Mesh,
                Material = material,
            });
        }
    }
}

public class SimpleMeshRenderer : IComponentData
{
    public Mesh     Mesh;
    public Material Material;
}

[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
partial class SimpleMeshRendererSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities.ForEach((SimpleMeshRenderer renderer, ref LocalToWorld localToWorld) =>
        {
            Graphics.DrawMesh(renderer.Mesh, localToWorld.Value, renderer.Material, 0);
        }).WithoutBurst().Run();
    }
}

#endif
