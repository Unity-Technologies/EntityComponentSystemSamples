using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

public static  unsafe class CartesianGridGeneratorUtility
{
    public enum WallFlags : byte
    {
        SouthWall = 1,
        WestWall = 2,
    }
    
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
            new float4(-1.00f, 0.00f, 0.00f, 0.00f),
            new float4(0.00f, 0.00f, 1.00f, 0.00f),
            new float4(0.00f, 1.00f, 0.00f, 0.00f),
            new float4(0.00f, 0.00f, 0.00f, 1.00f)),
        new float4x4(
            new float4(1.00f, 0.00f, 0.00f, 0.00f),
            new float4(0.00f, 0.00f, -1.00f, 0.00f),
            new float4(0.00f, 1.00f, 0.00f, 0.00f),
            new float4(0.00f, 0.00f, 0.00f, 1.00f)),
    };
    
    public static unsafe void CreateGridPath(int rowCount, int columnCount, byte* gridWalls, float wallSProbability, float wallWProbability, bool outerWalls)
    {
        var buildGridPathJob = new BuildGridPath
        {
            GridWalls = gridWalls,
            RowCount = rowCount,
            ColumnCount = columnCount,
            WallSProbability = wallSProbability,
            WallWProbability = wallWProbability,
            OuterWalls = outerWalls
        };
        buildGridPathJob.Run();
    }

    [BurstCompile]
    unsafe struct BuildGridPath : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public byte* GridWalls;
        public int RowCount;
        public int ColumnCount;
        public float WallSProbability;
        public float WallWProbability;
        public bool OuterWalls;

        public void Execute()
        {
            var cx = (ColumnCount * 0.5f);
            var cz = (RowCount * 0.5f);

            // Add additional row/col to create SE walls along outer edge
            var GridWallsRowCount = RowCount + 1;
            var GridWallsColumnCount = ColumnCount + 1;
            
            // 2 bit (0=None, 1=South Wall, 2=West Wall)
            // Temp allocations in jobs auto-disposed
            var GridWallsSW = new NativeArray<byte>(GridWallsRowCount * GridWallsColumnCount, Allocator.Temp);
            
            // Populate the grid
            for (int y = 0; y < GridWallsRowCount; y++)
            for (int x = 0; x < GridWallsColumnCount; x++)
            {
                // By default place outer walls along the edge of the grid.
                if (((y&1)==1) && (x&1)==1)
                    GridWallsSW[(y * GridWallsColumnCount) + x] |= (byte)WallFlags.SouthWall;
                if ((y == 0) && (x < (GridWallsColumnCount - 1)))
                    GridWallsSW[(y * GridWallsColumnCount) + x] |= (byte)WallFlags.SouthWall;
                if ((x == 0) && (y < (GridWallsRowCount - 1)))
                    GridWallsSW[(y * GridWallsColumnCount) + x] |= (byte)WallFlags.WestWall;
                if ((y == (GridWallsRowCount - 1)) && (x < (GridWallsColumnCount - 1)))
                    GridWallsSW[(y * GridWallsColumnCount) + x] |= (byte)WallFlags.SouthWall;
                if ((x == (GridWallsColumnCount - 1)) && (y < (GridWallsRowCount - 1)))
                    GridWallsSW[(y * GridWallsColumnCount) + x] |= (byte)WallFlags.WestWall;

                if ((x < (GridWallsColumnCount - 1)) && (y < (GridWallsRowCount - 1)))
                {
                    var tx = ((float)x) - cx;
                    var tz = ((float)y) - cz;
                    var noiseShift = (float)((((long)GridWalls) >> 8) & 0xfff);

                    var n0 = noise.snoise(new float2(tx * 1.2345f + noiseShift, tz * 1.2345f + noiseShift));
                    var n1 = noise.snoise(new float2(tz * 1.6789f + noiseShift, tx * 1.6789f + noiseShift));

                    if (((n0 * 0.5f) + 0.5f) < WallSProbability)
                        GridWallsSW[(y * GridWallsColumnCount) + x] |= (byte)WallFlags.SouthWall;
                    if (((n1 * 0.5f) + 0.5f) < WallWProbability)
                        GridWallsSW[(y * GridWallsColumnCount) + x] |= (byte)WallFlags.WestWall;
                }

                // Make sure there are no outer walls along the edge of the grid
                if (!OuterWalls)
                {
                    if (x == 0)
                        GridWallsSW[(y * GridWallsColumnCount) + x] &= (byte)~WallFlags.WestWall;
                    if (x == (GridWallsColumnCount - 1))
                        GridWallsSW[(y * GridWallsColumnCount) + x] &= (byte)~WallFlags.WestWall;
                    if (y == (GridWallsRowCount - 1))
                        GridWallsSW[(y * GridWallsColumnCount) + x] &= (byte)~WallFlags.SouthWall;
                    if (y == 0)
                        GridWallsSW[(y * GridWallsColumnCount) + x] &= (byte)~WallFlags.SouthWall;
                }
            }

            int gridWallSize = RowCount * ((ColumnCount + 1) / 2);
            UnsafeUtility.MemClear(GridWalls, gridWallSize);
            
            for (int y = 0; y < RowCount; y++)
            for (int x = 0; x < ColumnCount; x++)
            {
                var wallN = ((GridWallsSW[((y + 1) * GridWallsColumnCount) + x] & (byte)WallFlags.SouthWall) != 0);
                var wallS = ((GridWallsSW[(y * GridWallsColumnCount) + x] & (byte)WallFlags.SouthWall) != 0);
                var wallW = ((GridWallsSW[(y * GridWallsColumnCount) + x] & (byte)WallFlags.WestWall) != 0);
                var wallE = ((GridWallsSW[(y * GridWallsColumnCount) + (x + 1)] & (byte)WallFlags.WestWall) != 0);
                var walls = ((wallN) ? 0x01 : 0x00)
                    | ((wallS) ? 0x02 : 0x00)
                    | ((wallW) ? 0x04 : 0x00)
                    | ((wallE) ? 0x08 : 0x00);

                var gridWallIndex = (y * ((ColumnCount+1)/2)) + (x / 2);
                walls <<= (x & 1) * 4; // odd columns packed into upper 4 bits
                walls |= GridWalls[gridWallIndex];
                GridWalls[gridWallIndex] = (byte)walls;
            }
        }
    }
     
    public static void CreateFloorPanel(EntityManager dstManager, Entity prefab, float4x4 parentLocalToWorld, float tx, float tz)
    {
        var pos = new float3(tx + 0.5f, 0.0f, tz + 0.5f);
        var childLocalToParent = math.mul(float4x4.Translate(pos), float4x4.Scale(0.98f));
        var localToWorld = new LocalToWorld
        {
            Value = math.mul(parentLocalToWorld,childLocalToParent)
        };
        
        CreatePanel(dstManager,  prefab, localToWorld);
    }
    
    public static void CreateWallS(EntityManager dstManager, Entity prefab, float4x4 parentLocalToWorld, float tx, float tz)
    {
        var pos = new float3(tx + 0.5f, 1.0f, tz); 
        var childLocalToParent = math.mul(float4x4.Translate(pos), float4x4.Scale(1.1f, 1.1f, 0.1f));
        var localToWorld = new LocalToWorld
        {
            Value = math.mul(parentLocalToWorld,childLocalToParent)
        };
        
        CreatePanel(dstManager, prefab, localToWorld);
    }
    
    public static void CreateWallW(EntityManager dstManager, Entity prefab, float4x4 parentLocalToWorld, float tx, float tz)
    {
        var pos = new float3(tx, 1.0f, tz + 0.5f);
        var childLocalToParent = math.mul(float4x4.Translate(pos), float4x4.Scale(0.1f, 1.1f, 1.1f));
        var localToWorld = new LocalToWorld
        {
            Value = math.mul(parentLocalToWorld,childLocalToParent)
        };

        CreatePanel(dstManager, prefab, localToWorld);
    }
    
    public static void CreatePanel(EntityManager dstManager, Entity prefab, LocalToWorld localToWorld)
    {
        var panelEntity = dstManager.Instantiate(prefab);
        
        dstManager.RemoveComponent<Translation>(panelEntity);
        dstManager.RemoveComponent<Rotation>(panelEntity);
        dstManager.SetComponentData(panelEntity, localToWorld);
    }


    public static void FillCubeFaceTransforms(int rowCount, float4x4* faceLocalToWorld, float4x4* faceWorldToLocal, float4x4* faceLocalToLocal)
    {
        for (int faceIndex = 0; faceIndex < 6; faceIndex++)
        {
            ref var localToWorld = ref m_FaceLocalToWorldRotation[faceIndex];

            // Translate along normal of face by width
            localToWorld.c3.xyz = localToWorld.c1.xyz * rowCount * 0.5f;

            faceLocalToWorld[faceIndex] = localToWorld;
            faceWorldToLocal[faceIndex] = math.fastinverse(faceLocalToWorld[faceIndex]);
        }

        // Face x Face matrix for access simplicity, but:
        //   - Diagonal is identity and unused
        //   - Only the edge transitions are actually used (4 edges for each face) 
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                faceLocalToLocal[(i * 6) + j] = math.mul(faceWorldToLocal[j], faceLocalToWorld[i]);
            }
        }
    }
}