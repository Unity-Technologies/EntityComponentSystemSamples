using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public partial class JumpingSpherePSSystem : SystemBase
{
    protected override void OnUpdate()
    {      
        //Make the sphere jumps
        var time = (float)Time.ElapsedTime;
        var y = math.abs(math.cos(time*3f));
        Entities.WithAll<JumpingSphereTag>().ForEach((ref Translation translation) => 
        {
            translation.Value = new float3(0, y, 0);

        }).ScheduleParallel();

        //Play ParticleSystem when the sphere is touching the ground
        Entities.WithoutBurst().WithAll<JumpingSpherePSTag>().ForEach((UnityEngine.VFX.VisualEffect ps ) =>
        {
            if(y < 0.05f) ps.Play();

        }).Run();
    }
}
