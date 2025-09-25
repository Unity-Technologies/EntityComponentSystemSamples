using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class CubeSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<NetworkStreamInGame>();
    }

    [WithAll(typeof(CubeTagComponent))]
    partial struct CubeJob : IJobEntity
    {
        public float deltaTime;
        public void Execute(Entity entity, ref LocalTransform transform)
        {
            transform.Rotation = math.mul(transform.Rotation, quaternion.RotateZ(math.radians(100 * deltaTime)));

        }
    }

    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        Dependency = new CubeJob()
        {
            deltaTime = deltaTime,
        }.Schedule(Dependency);
    }
}
