using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// public class CubeSpawnSystem : JobComponentSystem {
public class CubeSpawnSystem : ComponentSystem
{
    public EntityArchetype Cube;
    public EntityArchetype CubeAttach;

    protected override void OnUpdate()
    {
        Cube = Bootstrap.Cube;
        CubeAttach = Bootstrap.CubeAttach;

        var count = Bootstrap.Settings.nbOfCubes;
        float radius = 0;
        var mir = Bootstrap.Settings.getMSI();

        var segment = math.radians((float) 137.51);

        for (var i = 0; i < count; i++)
        {
            radius = 1.3f * math.sqrt(i);
            
            var cubeEntity = EntityManager.CreateEntity(Cube);
            EntityManager.SetComponentData(cubeEntity, new Rotation {Value = quaternion.identity});
            EntityManager.SetSharedComponentData(cubeEntity, mir);
            EntityManager.SetComponentData(cubeEntity, new Position
            {
                Value = new float3(0, 0, 0) + new float3(
                            radius * math.sin(i * segment + Time.deltaTime) * math.cos(0),
                            radius * math.sin(0) * math.sin(i * segment + Time.deltaTime),
                            radius * math.cos(i * segment + Time.deltaTime))
            });
            EntityManager.SetComponentData(cubeEntity, new RotationSpeed {Value = 2});
            
            var attachEntity = EntityManager.CreateEntity(CubeAttach);
            EntityManager.SetComponentData(attachEntity, new Attach
            {
                Child = cubeEntity,
                Parent = Bootstrap.transform
            });
        }

        Enabled = false;
    }

}
