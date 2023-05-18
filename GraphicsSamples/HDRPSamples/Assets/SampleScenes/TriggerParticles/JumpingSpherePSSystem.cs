using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[RequireMatchingQueriesForUpdate]
public partial struct JumpingSpherePSSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        var time = (float)SystemAPI.Time.ElapsedTime;
        var y = math.abs(math.cos(time*3f));
        
        //Make the sphere jumps
        foreach (var translation in SystemAPI.Query<RefRW<Translation>>().WithAll<JumpingSphereTag>())
        {
            translation.ValueRW.Value = new float3(0, y, 0);
        }

        //Play ParticleSystem when the sphere is touching the ground
        foreach (var particleSystem in SystemAPI.Query<SystemAPI.ManagedAPI.UnityEngineComponent<UnityEngine.VFX.VisualEffect>>().WithAll<JumpingSpherePSTag>())
        {
            if(y < 0.05f) particleSystem.Value.Play();
        }
    }
}
