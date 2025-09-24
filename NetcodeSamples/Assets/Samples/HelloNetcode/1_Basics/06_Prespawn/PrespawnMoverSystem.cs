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

        partial struct MovePrespawnJob : IJobEntity
        {
            public double time;
            public float deltaTime;
            void Execute(ref GhostInstance ghost, ref LocalTransform transform, ref PrespawnData data)
            {
                // After 2 seconds make the barrels slide between -2 and 2 on the x axis
                if (time > 2)
                {
                    data.Value++;

                    if (transform.Position.x > 2f)
                        data.Direction = -1f;
                    else if (transform.Position.x < -2f)
                        data.Direction = 1f;
                    transform.Position = new float3(transform.Position.x + deltaTime * data.Direction, transform.Position.y, transform.Position.z);
                }
            }
        }
        protected override void OnUpdate()
        {
            double time = SystemAPI.Time.ElapsedTime;
            float deltaTime = SystemAPI.Time.DeltaTime;

            Dependency = new MovePrespawnJob()
            {
                time = time,
                deltaTime = deltaTime,
            }.Schedule(Dependency);
        }
    }
}
