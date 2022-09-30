using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial class LivePropertyByGenerateAuthoringComponentSystem : SystemBase
{
    protected override void OnUpdate()
    {
#if !ENABLE_TRANSFORM_V1
        Entities.ForEach((ref LivePropertyComponent move, ref LocalToWorldTransform transform) =>
#else
        Entities.ForEach((ref LivePropertyComponent move, ref Translation translation, ref Rotation rotation, ref NonUniformScale scale) =>
#endif
        {
            // transform
            float deltaTime = SystemAPI.Time.DeltaTime;

#if !ENABLE_TRANSFORM_V1
            transform.Value.Position.x += 0.01f;
            transform.Value.Position.y += 0.01f;
            transform.Value.Position.z += 0.01f;

            transform.Value = transform.Value.RotateY(2 * deltaTime);

            transform.Value.Scale += 0.01f;
#else
            translation.Value.x += 0.01f;
            translation.Value.y += 0.01f;
            translation.Value.z += 0.01f;

            rotation.Value = math.mul(
                math.normalize(rotation.Value),
                quaternion.AxisAngle(math.up(), 2 * deltaTime));

            scale.Value += 0.01f;
#endif

            // float, int, bool
            move.FloatField = 0.0f;
            move.IntField += 1;
            move.BoolField = false;

            // float2, int2, bool2
            move.Float2Field.x += 1.0f;
            move.Float2Field.y += 1.0f;

            move.Int2Field.x += 1;
            move.Int2Field.y += 1;

            move.Bool2Field.x = !move.Bool2Field.x;
            move.Bool2Field.y = !move.Bool2Field.y;

            // float3, int3, bool3
            move.Float3Field.x += 1.0f;
            move.Float3Field.y += 1.0f;
            move.Float3Field.z += 1.0f;

            move.Int3Field.x += 1;
            move.Int3Field.y += 1;
            move.Int3Field.z += 1;

            move.Bool3Field.x = !move.Bool3Field.x;
            move.Bool3Field.y = !move.Bool3Field.y;
            move.Bool3Field.z = !move.Bool3Field.z;

            // float4, int4, bool4
            move.Float4Field.x += 1.0f;
            move.Float4Field.y += 1.0f;
            move.Float4Field.z += 1.0f;
            move.Float4Field.w += 1.0f;

            move.Int4Field.x += 1;
            move.Int4Field.y += 1;
            move.Int4Field.z += 1;
            move.Int4Field.w += 1;

            move.Bool4Field.x = !move.Bool4Field.x;
            move.Bool4Field.y = !move.Bool4Field.y;
            move.Bool4Field.z = !move.Bool4Field.z;
            move.Bool4Field.w = !move.Bool4Field.w;
        }).WithoutBurst().Run();
    }
}
