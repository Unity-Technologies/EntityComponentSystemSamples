using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
 
[GenerateAuthoringComponent]
public class MeshChanger : IComponentData
{
    public UnityEngine.Mesh mesh0;
    public UnityEngine.Mesh mesh1;
    public uint frequency;
    public uint frame;
    public uint active;
}

public class MeshChangerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        EntityManager entityManager = EntityManager;

        Entities.WithStructuralChanges().
            ForEach((Entity ent, MeshChanger changer, in RenderMesh mesh) =>
            {
                changer.frame = changer.frame + 1;

                if (changer.frame >= changer.frequency)
                {
                    changer.frame = 0;
                    changer.active = changer.active == 0 ? 1u : 0u;

                    RenderMesh modifiedMesh = mesh;
                    modifiedMesh.mesh = changer.active == 0 ? changer.mesh0 : changer.mesh1;
                    entityManager.SetSharedComponentData<RenderMesh>(ent, modifiedMesh);
                }

            }).Run();
    }
}
