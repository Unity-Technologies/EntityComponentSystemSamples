using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(CartesianGridMoveForwardSystem))]
public unsafe partial class CartesianGridOnCubeTransformSystem : SystemBase
{
    EntityQuery m_GridQuery;

    protected override void OnCreate()
    {
        m_GridQuery = GetEntityQuery(ComponentType.ReadOnly<CartesianGridOnCube>());
        RequireForUpdate(m_GridQuery);
    }

    protected override void OnUpdate()
    {
        var cartesianGridOnCube = SystemAPI.GetSingleton<CartesianGridOnCube>();
        var faceLocalToWorld = (float4x4*)cartesianGridOnCube.Blob.Value.FaceLocalToWorld.GetUnsafePtr();

        // Transform from grid-space Translation and gridFace to LocalToWorld for GridCube
        // - This is an example of overriding the transform system's default behavior.
        // - CubeFace is in the LocalToWorld WriteGroup, so when it this component is present, it is required to be
        //   part of the query in order to write to LocalToWorld. Since the transform system doesn't know anything
        //   about CubeFace, it will never be present in those default transformations. So it can be handled custom
        //   here.
        Entities
            .WithName("CartesianGridOnCubeTransform")
            .WithNativeDisableUnsafePtrRestriction(faceLocalToWorld)
            .ForEach((ref LocalToWorld localToWorld,
#if !ENABLE_TRANSFORM_V1
                in LocalTransform transform,
#else
                in Translation translation,
#endif
                in CartesianGridOnCubeFace cubeFace) =>
                {
                    var cubeFaceIndex = cubeFace.Value;
                    var resultLocalToWorld = faceLocalToWorld[cubeFaceIndex];
#if !ENABLE_TRANSFORM_V1
                    resultLocalToWorld.c3 = math.mul(resultLocalToWorld, new float4(transform.Position, 1.0f));
#else
                    resultLocalToWorld.c3 = math.mul(resultLocalToWorld, new float4(translation.Value, 1.0f));
#endif
                    localToWorld = new LocalToWorld
                    {
                        Value = resultLocalToWorld
                    };
                }).Schedule();
    }
}
