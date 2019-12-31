using Unity.Mathematics;

public static unsafe class CartesianGridOnCubeUtility
{
    // Next face to move to when moving off edge of a face
    public static readonly byte[] NextFaceIndex =
    {
        // 0  1  2  3  4  5    
        // X+ X- Y+ Y- Z+ Z- <- From which face
        4, 4, 4, 4, 2, 2, // Off north edge
        5, 5, 5, 5, 3, 3, // Off south edge
        2, 3, 1, 0, 0, 1, // Off west edge
        3, 2, 0, 1, 1, 0, // Off east edge
    };
    
    public static readonly byte[] NextFaceDirection =
    {
        // 0  1  2  3  4  5    
        // X+ X- Y+ Y- Z+ Z- <- From which face
        3, 2, 1, 0, 1, 0, // Off north edge
        2, 3, 1, 0, 1, 0, // Off south edge
        2, 2, 2, 2, 1, 0, // Off west edge
        3, 3, 3, 3, 1, 0, // Off east edge
    };
    
    static readonly float2[] m_GridToGrid =
    {
        //
        // From North edge
        //
        
        // 0
        new float2(0.0f, -1.0f), 
        new float2(-1.0f, 0.0f), 
        
        // 1
        new float2(0.0f, 1.0f), 
        new float2(1.0f, 0.0f), 
        
        // 2
        new float2(-1.0f, 0.0f), 
        new float2(0.0f, 1.0f), 
        
        // 3
        new float2(1.0f, 0.0f), 
        new float2(0.0f, -1.0f), 

        // 4
        new float2(-1.0f, 0.0f), 
        new float2(0.0f, 1.0f), 
        
        // 5
        new float2(1.0f, 0.0f), 
        new float2(0.0f, -1.0f), 
        
        //
        // From South edge
        //
        
        // 0
        new float2(0.0f, -1.0f), 
        new float2(-1.0f, 0.0f), 
        
        // 1
        new float2(0.0f, 1.0f), 
        new float2(1.0f, 0.0f), 
        
        // 2
        new float2(1.0f, 0.0f), 
        new float2(0.0f, -1.0f), 
        
        // 3
        new float2(-1.0f, 0.0f), 
        new float2(0.0f, 1.0f), 

        // 4
        new float2(1.0f, 0.0f), 
        new float2(0.0f, -1.0f), 
        
        // 5
        new float2(-1.0f, 0.0f), 
        new float2(0.0f, 1.0f), 
        
        //
        // From West edge
        //
        
        // 0
        new float2(-1.0f, 0.0f), 
        new float2(0.0f, 1.0f), 
        
        // 1
        new float2(-1.0f, 0.0f), 
        new float2(0.0f, 1.0f), 
        
        // 2
        new float2(-1.0f, 0.0f), 
        new float2(0.0f, 1.0f), 
        
        // 3
        new float2(-1.0f, 0.0f), 
        new float2(0.0f, 1.0f), 

        // 4
        new float2(0.0f, -1.0f), 
        new float2(-1.0f, 0.0f), 
        
        // 5
        new float2(0.0f, 1.0f), 
        new float2(1.0f, 0.0f), 
        
        //
        // From East edge
        //
        
        // 0
        new float2(-1.0f, 0.0f), 
        new float2(0.0f, 1.0f), 
        
        // 1
        new float2(-1.0f, 0.0f), 
        new float2(0.0f, 1.0f), 
        
        // 2
        new float2(-1.0f, 0.0f), 
        new float2(0.0f, 1.0f), 
        
        // 3
        new float2(-1.0f, 0.0f), 
        new float2(0.0f, 1.0f), 

        // 4
        new float2(0.0f, 1.0f), 
        new float2(1.0f, 0.0f), 
        
        // 5
        new float2(0.0f, -1.0f), 
        new float2(-1.0f, 0.0f), 
    };
    
    public static int CellFaceIndex(int cellIndex, int rowCount)
    {
        var cellCount = rowCount * rowCount;
        var faceIndex = cellIndex / cellCount;
        return faceIndex;
    }
    
    public static CartesianGridCoordinates CellFaceCoordinates(int cellIndex, int rowCount)
    {
        var cellCount = rowCount * rowCount;
        var faceCellIndex = cellIndex % cellCount;
        var y = faceCellIndex / rowCount;
        var x = faceCellIndex - (y * rowCount);
        return new CartesianGridCoordinates { x = (short)x, y = (short)y };
    }

    public static int CellIndexFromExitEdge(int edge, int cellIndex, int rowCount)
    {
        var faceIndex = CellFaceIndex(cellIndex, rowCount);
        var facePosition = CellFaceCoordinates(cellIndex, rowCount);
        return CellIndexFromExitEdge(edge, faceIndex, facePosition, rowCount);
    }

    public static int CellIndexFromExitEdge(int edge, int faceIndex, CartesianGridCoordinates facePosition, int rowCount)
    {
        var cellCount = rowCount * rowCount;
        var nextFaceIndex = NextFaceIndex[(edge * 6) + faceIndex];
        var cx = (rowCount-1) * 0.5f;
        var px = math.clamp(facePosition.x, 0, rowCount - 1);
        var py = math.clamp(facePosition.y, 0, rowCount - 1);
        var x0 = px - cx;
        var y0 = py - cx;
        var ax = m_GridToGrid[(edge * 6 * 2) + (faceIndex * 2) + 0];
        var ay = m_GridToGrid[(edge * 6 * 2) + (faceIndex * 2) + 1];
        var x1 = (ax.x * x0) + (ax.y * y0);
        var y1 = (ay.x * x0) + (ay.y * y0);
        var nx = (short) (x1 + cx + 0.5f);
        var ny = (short) (y1 + cx + 0.5f);
        
        var nextFaceCellIndex = (ny * rowCount) + nx;
        return (nextFaceIndex * cellCount) + nextFaceCellIndex; 
    }

    public static int CellIndex(CartesianGridCoordinates cellPosition, CartesianGridOnCubeFace cubeFace, int rowCount)
    {
        var rowStride = rowCount;
        var faceStride = rowCount * rowStride;
        var cellIndex = (cubeFace.Value * faceStride) + (cellPosition.y * rowStride) + cellPosition.x;
        return cellIndex;
    }
    
    public static int CellIndexNorth(int cellIndex, int rowCount, float4x4* faceLocalToLocal)
    {
        var faceIndex = CellFaceIndex(cellIndex, rowCount);
        var facePosition = CellFaceCoordinates(cellIndex, rowCount);

        facePosition.y += 1;
        
        var edge = CartesianGridMovement.CubeExitEdge(facePosition, rowCount);
        if (edge == -1)
            return cellIndex + rowCount;

        return CellIndexFromExitEdge(edge, faceIndex, facePosition, rowCount);
    }
    
    public static int CellIndexSouth(int cellIndex, int rowCount, float4x4* faceLocalToLocal)
    {
        var faceIndex = CellFaceIndex(cellIndex, rowCount);
        var facePosition = CellFaceCoordinates(cellIndex, rowCount);

        facePosition.y -= 1;
        
        var edge = CartesianGridMovement.CubeExitEdge(facePosition, rowCount);
        if (edge == -1)
            return cellIndex - rowCount;

        return CellIndexFromExitEdge(edge, faceIndex, facePosition, rowCount);
    }
    
    public static int CellIndexWest(int cellIndex, int rowCount, float4x4* faceLocalToLocal)
    {
        var faceIndex = CellFaceIndex(cellIndex, rowCount);
        var facePosition = CellFaceCoordinates(cellIndex, rowCount);

        facePosition.x -= 1;
        
        var edge = CartesianGridMovement.CubeExitEdge(facePosition, rowCount);
        if (edge == -1)
            return cellIndex - 1;

        return CellIndexFromExitEdge(edge, faceIndex, facePosition, rowCount);
    }
    
    public static int CellIndexEast(int cellIndex, int rowCount, float4x4* faceLocalToLocal)
    {
        var faceIndex = CellFaceIndex(cellIndex, rowCount);
        var facePosition = CellFaceCoordinates(cellIndex, rowCount);

        facePosition.x += 1;
        
        var edge = CartesianGridMovement.CubeExitEdge(facePosition, rowCount);
        if (edge == -1)
            return cellIndex + 1;

        return CellIndexFromExitEdge(edge, faceIndex, facePosition, rowCount);
    } 
}
