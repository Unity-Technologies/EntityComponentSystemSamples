using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

[ConverterVersion("macton", 2)]
[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.EntitySceneOptimizations)]
public unsafe class CartesianGridOnCubeSystemGeneratorSystem : JobComponentSystem
{
    static readonly float4x4[] m_FaceLocalToWorldRotation =
    {
        new float4x4(
            new float4(0.00f, -1.00f, 0.00f, 0.00f),
            new float4(1.00f, 0.00f, 0.00f, 0.00f),
            new float4(0.00f, 0.00f, 1.00f, 0.00f),
            new float4(0.00f, 0.00f, 0.00f, 1.00f)),
        new float4x4(
            new float4(0.00f, 1.00f, 0.00f, 0.00f),
            new float4(-1.00f, 0.00f, 0.00f, 0.00f),
            new float4(0.00f, 0.00f, 1.00f, 0.00f),
            new float4(0.00f, 0.00f, 0.00f, 1.00f)),
        new float4x4(
            new float4(1.00f, 0.00f, 0.00f, 0.00f),
            new float4(0.00f, 1.00f, 0.00f, 0.00f),
            new float4(0.00f, 0.00f, 1.00f, 0.00f),
            new float4(0.00f, 0.00f, 0.00f, 1.00f)),
        new float4x4(
            new float4(-1.00f, 0.00f, 0.00f, 0.00f),
            new float4(0.00f, -1.00f, 0.00f, 0.00f),
            new float4(0.00f, 0.00f, 1.00f, 0.00f),
            new float4(0.00f, 0.00f, 0.00f, 1.00f)),
        new float4x4(
            new float4(1.00f, 0.00f, 0.00f, 0.00f),
            new float4(0.00f, 0.00f, 1.00f, 0.00f),
            new float4(0.00f, -1.00f, 0.00f, 0.00f),
            new float4(0.00f, 0.00f, 0.00f, 1.00f)),
        new float4x4(
            new float4(1.00f, 0.00f, 0.00f, 0.00f),
            new float4(0.00f, 0.00f, -1.00f, 0.00f),
            new float4(0.00f, 1.00f, 0.00f, 0.00f),
            new float4(0.00f, 0.00f, 0.00f, 1.00f)),
    };
    
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        inputDeps.Complete();
        Entities.WithStructuralChanges().ForEach((Entity entity, ref CartesianGridOnCubeGenerator cartesianGridOnCubeGenerator) =>
        {
            ref var floorPrefab = ref cartesianGridOnCubeGenerator.Blob.Value.FloorPrefab;
            var wallPrefab = cartesianGridOnCubeGenerator.Blob.Value.WallPrefab;
            var rowCount = cartesianGridOnCubeGenerator.Blob.Value.RowCount;
            var wallSProbability = cartesianGridOnCubeGenerator.Blob.Value.WallSProbability;
            var wallWProbability = cartesianGridOnCubeGenerator.Blob.Value.WallWProbability;

            var floorPrefabCount = floorPrefab.Length;
            if (floorPrefabCount == 0)
                return;

            var cx = (rowCount * 0.5f);
            var cz = (rowCount * 0.5f);

            // 4 bits per grid section (bit:0=N,1=S,2=W,3=E)
            // One grid for each face.
            var gridWallsSize = 6 * (rowCount * (rowCount + 1) / 2);

            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var cartesianGridOnCubeBlob = ref blobBuilder.ConstructRoot<CartesianGridOnCubeBlob>();

            var trailingOffsets = blobBuilder.Allocate(ref cartesianGridOnCubeBlob.TrailingOffsets, 4);
            var gridWalls = blobBuilder.Allocate(ref cartesianGridOnCubeBlob.Walls, gridWallsSize);
            var faceLocalToWorld = blobBuilder.Allocate(ref cartesianGridOnCubeBlob.FaceLocalToWorld, 6);
            var faceWorldToLocal = blobBuilder.Allocate(ref cartesianGridOnCubeBlob.FaceWorldToLocal, 6);
            var faceLocalToLocal = blobBuilder.Allocate(ref cartesianGridOnCubeBlob.FaceLocalToLocal, 36);

            cartesianGridOnCubeBlob.RowCount = (ushort)rowCount;

            for (int faceIndex = 0; faceIndex < 6; faceIndex++)
            {
                var localToWorld = m_FaceLocalToWorldRotation[faceIndex];

                // Translate along normal of face by width
                localToWorld.c3.xyz = localToWorld.c1.xyz * rowCount * 0.5f;

                faceLocalToWorld[faceIndex] = localToWorld;
                faceWorldToLocal[faceIndex] = math.fastinverse(faceLocalToWorld[faceIndex]);
            }

            // Diagonal is identity and unused, but makes lookup simpler.
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    faceLocalToLocal[(i * 6) + j] = math.mul(faceWorldToLocal[j], faceLocalToWorld[i]);
                }
            }

            for (int i = 0; i < 6; i++)
            {
                var faceGridWallsOffset = i * (rowCount * (rowCount + 1) / 2);

                CartesianGridGeneratorUtility.CreateGridPath(rowCount, rowCount, ((byte*)gridWalls.GetUnsafePtr()) + faceGridWallsOffset, wallSProbability, wallWProbability, false);

                // Create visible geometry
                for (int y = 0; y < rowCount; y++)
                for (int x = 0; x < rowCount; x++)
                {
                    var prefabIndex = (x + y) % floorPrefabCount;
                    var tx = ((float)x) - cx;
                    var tz = ((float)y) - cz;

                    CartesianGridGeneratorUtility.CreateFloorPanel(EntityManager, floorPrefab[prefabIndex], faceLocalToWorld[i], tx, tz);

                    var gridWallsIndex = faceGridWallsOffset + ((y * ((rowCount + 1) / 2)) + (x / 2));
                    var walls = (gridWalls[gridWallsIndex] >> ((x & 1) * 4)) & 0x0f;

                    if ((walls & 0x02) != 0) // South wall
                        CartesianGridGeneratorUtility.CreateWallS(EntityManager, wallPrefab, faceLocalToWorld[i], tx, tz);
                    if ((walls & 0x04) != 0) // West wall
                        CartesianGridGeneratorUtility.CreateWallW(EntityManager, wallPrefab, faceLocalToWorld[i], tx, tz);
                }
            }

            trailingOffsets[0] = new float2(cx + 0.0f, cz + -0.5f); // North
            trailingOffsets[1] = new float2(cx + 0.0f, cz + 0.5f); // South
            trailingOffsets[2] = new float2(cx + 0.5f, cz + 0.0f); // West
            trailingOffsets[3] = new float2(cx + -0.5f, cz + 0.0f); // East

            EntityManager.AddComponentData(entity, new CartesianGridOnCube
            {
                Blob = blobBuilder.CreateBlobAssetReference<CartesianGridOnCubeBlob>(Allocator.Persistent)
            });

            blobBuilder.Dispose();

            EntityManager.RemoveComponent<CartesianGridOnCubeGenerator>(entity);

        }).Run();
        
        return new JobHandle();
    }
}
