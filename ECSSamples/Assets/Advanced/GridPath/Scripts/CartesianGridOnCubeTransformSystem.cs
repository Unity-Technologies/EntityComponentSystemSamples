using System;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(CartesianGridMoveForwardSystem))]
public unsafe class CartesianGridOnCubeTransformSystem : JobComponentSystem
{
    EntityQuery m_GridQuery;

    protected override void OnCreate()
    {
        m_GridQuery = GetEntityQuery(ComponentType.ReadOnly<CartesianGridOnCube>());
        RequireForUpdate(m_GridQuery);
    }

    protected override JobHandle OnUpdate(JobHandle lastJobHandle)
    {
        var cartesianGridOnCube = GetSingleton<CartesianGridOnCube>();
        var faceLocalToWorld = (float4x4*)cartesianGridOnCube.Blob.Value.FaceLocalToWorld.GetUnsafePtr();

        // Transform from grid-space Translation and gridFace to LocalToWorld for GridCube
        // - This is an example of overriding the transform system's default behavior.
        // - CubeFace is in the LocalToWorld WriteGroup, so when it this component is present, it is required to be
        //   part of the query in order to write to LocalToWorld. Since the transform system doesn't know anything
        //   about CubeFace, it will never be present in those default transformations. So it can be handled custom
        //   here.
        lastJobHandle = Entities
            .WithName("CartesianGridOnCubeTransform")
            .WithNativeDisableUnsafePtrRestriction(faceLocalToWorld)
            .ForEach((ref LocalToWorld localToWorld,
                in Translation translation,
                in CubeFace cubeFace) =>
            {
                var cubeFaceIndex = cubeFace.Value;
                var resultLocalToWorld = faceLocalToWorld[cubeFaceIndex];
                resultLocalToWorld.c3 = math.mul(resultLocalToWorld, new float4(translation.Value, 1.0f));

                localToWorld = new LocalToWorld
                {
                    Value = resultLocalToWorld
                };
            }).Schedule(lastJobHandle);

        return lastJobHandle;
    }
}
