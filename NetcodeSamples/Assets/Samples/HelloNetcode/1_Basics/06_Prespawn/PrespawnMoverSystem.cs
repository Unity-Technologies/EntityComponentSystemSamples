using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;

namespace Samples.HelloNetcode
{
    [UpdateInGroup(typeof(HelloNetcodeSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class PrespawnMoverSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<EnablePrespawn>();
        }

        protected override void OnUpdate()
        {
            var time = SystemAPI.Time.ElapsedTime;
            var deltaTime = SystemAPI.Time.DeltaTime;
#if !ENABLE_TRANSFORM_V1
            Entities.WithName("MovePrespawnBarrels").ForEach((Entity entity, ref GhostComponent ghost, ref LocalTransform transform,
#else
            Entities.WithName("MovePrespawnBarrels").ForEach((Entity entity, ref GhostComponent ghost, ref Translation translation,
#endif
                ref PrespawnData data) =>
            {
                // After 2 seconds make the barrels slide between -2 and 2 on the x axis
                if (time > 2)
                {
                    data.Value++;
#if !ENABLE_TRANSFORM_V1
                    if (transform.Position.x > 2f)
                        data.Direction = -1f;
                    else if (transform.Position.x < -2f)
                        data.Direction = 1f;
                    transform.Position = new float3(transform.Position.x + deltaTime * data.Direction, transform.Position.y, transform.Position.z);
#else
                    if (translation.Value.x > 2f)
                        data.Direction = -1f;
                    else if (translation.Value.x < -2f)
                        data.Direction = 1f;
                    translation.Value = new float3(translation.Value.x + deltaTime * data.Direction, translation.Value.y, translation.Value.z);
#endif
                }
            }).Schedule();
        }
    }
}
