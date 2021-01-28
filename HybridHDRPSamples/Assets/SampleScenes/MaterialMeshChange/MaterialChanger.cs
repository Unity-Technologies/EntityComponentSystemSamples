using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
 
[GenerateAuthoringComponent]
public class MaterialChanger : IComponentData
{
    public UnityEngine.Material material0;
    public UnityEngine.Material material1;
    public uint frequency;
    public uint frame;
    public uint active;
}

public class MaterialChangerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        EntityManager entityManager = EntityManager;

        Entities.WithStructuralChanges().
            ForEach((Entity ent, MaterialChanger changer, in RenderMesh mesh) =>
            {
                changer.frame = changer.frame + 1;

                if (changer.frame >= changer.frequency)
                {
                    changer.frame = 0;
                    changer.active = changer.active == 0 ? 1u : 0u;

                    RenderMesh modifiedMesh = mesh;
                    modifiedMesh.material = changer.active == 0 ? changer.material0 : changer.material1;
                    entityManager.SetSharedComponentData<RenderMesh>(ent, modifiedMesh);
                }

            }).Run();
    }
}
