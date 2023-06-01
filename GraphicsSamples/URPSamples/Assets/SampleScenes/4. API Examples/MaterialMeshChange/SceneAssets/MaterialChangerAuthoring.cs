using System.Collections.Generic;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

public class MaterialChanger : IComponentData
{
    public Material material0;
    public Material material1;
    public uint frequency;
    public uint frame;
    public uint active;
}

[DisallowMultipleComponent]
public class MaterialChangerAuthoring : MonoBehaviour
{
    public Material material0;
    public Material material1;
    [RegisterBinding(typeof(MaterialChanger), "frequency")]
    public uint frequency;
    [RegisterBinding(typeof(MaterialChanger), "frame")]
    public uint frame;
    [RegisterBinding(typeof(MaterialChanger), "active")]
    public uint active;

    class MaterialChangerBaker : Baker<MaterialChangerAuthoring>
    {
        public override void Bake(MaterialChangerAuthoring authoring)
        {
            MaterialChanger component = new MaterialChanger();
            component.material0 = authoring.material0;
            component.material1 = authoring.material1;
            component.frequency = authoring.frequency;
            component.frame = authoring.frame;
            component.active = authoring.active;
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponentObject(entity, component);
        }
    }
}

[RequireMatchingQueriesForUpdate]
public partial class MaterialChangerSystem : SystemBase
{
    private Dictionary<Material, BatchMaterialID> m_MaterialMapping;

    private void RegisterMaterial(EntitiesGraphicsSystem hybridRendererSystem, Material material)
    {
        // Only register each mesh once, so we can also unregister each mesh just once
        if (!m_MaterialMapping.ContainsKey(material))
            m_MaterialMapping[material] = hybridRendererSystem.RegisterMaterial(material);
    }

    protected override void OnStartRunning()
    {
        var hybridRenderer = World.GetOrCreateSystemManaged<EntitiesGraphicsSystem>();
        m_MaterialMapping = new Dictionary<Material, BatchMaterialID>();

        Entities
            .WithoutBurst()
            .ForEach((in MaterialChanger changer) =>
            {
                RegisterMaterial(hybridRenderer, changer.material0);
                RegisterMaterial(hybridRenderer, changer.material1);
            }).Run();
    }

    private void UnregisterMaterials()
    {
        // Can't call this from OnDestroy(), so we can't do this on teardown
        var hybridRenderer = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
        if (hybridRenderer == null)
            return;

        foreach (var kv in m_MaterialMapping)
            hybridRenderer.UnregisterMaterial(kv.Value);
    }

    protected override void OnUpdate()
    {
        EntityManager entityManager = EntityManager;

        Entities
            .WithoutBurst()
            .ForEach((MaterialChanger changer, ref MaterialMeshInfo mmi) =>
            {
                changer.frame = changer.frame + 1;

                if (changer.frame >= changer.frequency)
                {
                    changer.frame = 0;
                    changer.active = changer.active == 0 ? 1u : 0u;
                    var material = changer.active == 0 ? changer.material0 : changer.material1;
                    mmi.MaterialID = m_MaterialMapping[material];
                }
            }).Run();
    }
}
