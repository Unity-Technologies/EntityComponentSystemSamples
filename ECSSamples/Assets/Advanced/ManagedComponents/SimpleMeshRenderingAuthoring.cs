// NOTE: This is a per project ifdfef,
//       the samples in this project are run in both modes for testing purposes.
//       In a normal game project this ifdef is not required.

#if !UNITY_DISABLE_MANAGED_COMPONENTS
using Unity.Entities;
using UnityEngine;
using Unity.Transforms;



[ConverterVersion("joe", 2)]
class SimpleMeshRenderingAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public Mesh Mesh = null;
    public Color Color = Color.white;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        // Assets in subscenes can either be created during conversion and embedded in the scene
        var material = new Material(Shader.Find("Standard"));
        material.color = Color;
        // ... Or be an asset that is being referenced.
        
        dstManager.AddComponentData(entity, new SimpleMeshRenderer
        {
            Mesh = Mesh,
            Material = material, 
        });
    }
}

public class SimpleMeshRenderer : IComponentData
{
    public Mesh     Mesh;
    public Material Material;
}

[ExecuteAlways]
[AlwaysUpdateSystem]
[UpdateInGroup(typeof(PresentationSystemGroup))]
class SimpleMeshRendererSystem : ComponentSystem
{
    override protected void OnUpdate()
    {
        Entities.ForEach((SimpleMeshRenderer renderer, ref LocalToWorld localToWorld) =>
        {
            Graphics.DrawMesh(renderer.Mesh, localToWorld.Value, renderer.Material, 0);
        });
    }
}

#endif