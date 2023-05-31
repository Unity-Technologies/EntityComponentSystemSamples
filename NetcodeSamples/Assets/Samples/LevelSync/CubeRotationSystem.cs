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

    protected override void OnUpdate()
    {
        var deltaTime = SystemAPI.Time.DeltaTime;

        Entities.WithAll<CubeTagComponent>().ForEach((Entity entity, ref LocalTransform transform) =>
        {
            transform.Rotation = math.mul(transform.Rotation, quaternion.RotateZ(math.radians(100 * deltaTime)));


        }).Schedule();
    }
}
