using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[RequireMatchingQueriesForUpdate]
public unsafe partial class CartesianGridOnCubeSystemGeneratorSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Dependency.Complete();
        Entities.WithStructuralChanges().ForEach((Entity entity, ref CartesianGridOnCubeGenerator cartesianGridOnCubeGenerator, in DynamicBuffer<CartesianGridOnCubeGeneratorFloorPrefab> floorPrefabsBuffer) =>
        {
            var floorPrefabs = new NativeArray<Entity>(floorPrefabsBuffer.Length, Allocator.Temp);
            floorPrefabs.CopyFrom(floorPrefabsBuffer.Reinterpret<Entity>().AsNativeArray());

            var wallPrefab = cartesianGridOnCubeGenerator.WallPrefab;
            var rowCount = cartesianGridOnCubeGenerator.RowCount;
            var wallSProbability = cartesianGridOnCubeGenerator.WallSProbability;
            var wallWProbability = cartesianGridOnCubeGenerator.WallWProbability;

            var floorPrefabCount = floorPrefabs.Length;
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

            CartesianGridGeneratorUtility.FillCubeFaceTransforms(rowCount, (float4x4*)faceLocalToWorld.GetUnsafePtr(), (float4x4*)faceWorldToLocal.GetUnsafePtr(), (float4x4*)faceLocalToLocal.GetUnsafePtr());

            for (int faceIndex = 0; faceIndex < 6; faceIndex++)
            {
                var rowStride = (rowCount + 1) / 2;
                var faceStride = rowCount * rowStride;
                var faceGridWallsOffset = faceIndex * faceStride;

                CartesianGridGeneratorUtility.CreateGridPath(rowCount, rowCount, ((byte*)gridWalls.GetUnsafePtr()) + faceGridWallsOffset, wallSProbability, wallWProbability, false);

                // Create visible geometry
                for (int y = 0; y < rowCount; y++)
                    for (int x = 0; x < rowCount; x++)
                    {
                        var prefabIndex = (x + y) % floorPrefabCount;
                        var tx = ((float)x) - cx;
                        var tz = ((float)y) - cz;

                        CartesianGridGeneratorUtility.CreateFloorPanel(EntityManager, floorPrefabs[prefabIndex], faceLocalToWorld[faceIndex], tx, tz);

                        var gridWallsIndex = faceGridWallsOffset + ((y * ((rowCount + 1) / 2)) + (x / 2));
                        var walls = (gridWalls[gridWallsIndex] >> ((x & 1) * 4)) & 0x0f;

                        if ((walls & 0x02) != 0) // South wall
                            CartesianGridGeneratorUtility.CreateWallS(EntityManager, wallPrefab, faceLocalToWorld[faceIndex], tx, tz);
                        if ((walls & 0x04) != 0) // West wall
                            CartesianGridGeneratorUtility.CreateWallW(EntityManager, wallPrefab, faceLocalToWorld[faceIndex], tx, tz);
                        if ((y == (rowCount - 1)) && ((walls & 0x01) != 0)) // North wall
                            CartesianGridGeneratorUtility.CreateWallS(EntityManager, wallPrefab, faceLocalToWorld[faceIndex], tx, tz + 1.0f);
                        if ((x == (rowCount - 1)) && ((walls & 0x08) != 0)) // East wall
                            CartesianGridGeneratorUtility.CreateWallW(EntityManager, wallPrefab, faceLocalToWorld[faceIndex], tx + 1.0f, tz);
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
    }
}
