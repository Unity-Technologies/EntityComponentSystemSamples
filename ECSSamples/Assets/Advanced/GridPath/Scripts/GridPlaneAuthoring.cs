using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// ReSharper disable once InconsistentNaming
[RequiresEntityConversion]
[AddComponentMenu("DOTS Samples/GridPath/Grid Plane")]
[ConverterVersion("joe", 1)]
public class GridPlaneAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    [Range(2,512)] public int ColumnCount;
    [Range(2,512)] public int RowCount;
    public GameObject[] FloorPrefab;
    public GameObject WallPrefab;
    
    // Specific wall probability, given PotentialWallProbability
    public float WallSProbability = 0.5f;
    public float WallWProbability = 0.5f;

    // Referenced prefabs have to be declared so that the conversion system knows about them ahead of time
    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(WallPrefab);
        referencedPrefabs.AddRange(FloorPrefab);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var prefabCount = FloorPrefab.Length;
        
#if UNITY_EDITOR
        dstManager.SetName(entity, "Grid");
#endif

        var cx = (ColumnCount * 0.5f);
        var cz = (RowCount * 0.5f);
        
        // 4 bits per grid section (bit:0=N,1=S,2=W,3=E)
        var gridWalls = new NativeArray<GridWalls>(RowCount * (ColumnCount+1)/2, Allocator.Persistent);
        
        GridAuthoringUtility.CreateGridPath(RowCount, ColumnCount, gridWalls, WallSProbability, WallWProbability, true);

        // Create visible geometry
        for (int y = 0; y < RowCount; y++)
        for (int x = 0; x < ColumnCount; x++)
        {
            var prefabIndex = (x+y) % prefabCount; 
            var tx = ((float)x) - cx;
            var tz = ((float)y) - cz;

            GridAuthoringUtility.CreateFloorPanel(dstManager, conversionSystem, gameObject, FloorPrefab[prefabIndex], float4x4.identity, tx, tz);
            
            var gridWallsIndex = (y * ((ColumnCount + 1) / 2)) + (x / 2);
            var walls = (gridWalls[gridWallsIndex].Value >> ((x & 1) * 4)) & 0x0f;

            if ((walls & 0x02) != 0) // South wall
                GridAuthoringUtility.CreateWallS(dstManager, conversionSystem, gameObject, WallPrefab, float4x4.identity, tx, tz);
            if ((walls & 0x04) != 0) // West wall
                GridAuthoringUtility.CreateWallW(dstManager, conversionSystem, gameObject, WallPrefab, float4x4.identity, tx, tz);
            if (y == (RowCount - 1)) // North wall
                GridAuthoringUtility.CreateWallS(dstManager, conversionSystem, gameObject, WallPrefab, float4x4.identity, tx, tz + 1.0f);
            if (x == (ColumnCount - 1)) // East wall
                GridAuthoringUtility.CreateWallW(dstManager, conversionSystem, gameObject, WallPrefab, float4x4.identity, tx + 1.0f, tz);
        }

        var gridWallsBuffer = dstManager.AddBuffer<GridWalls>(entity);
        gridWallsBuffer.AddRange(gridWalls);

        dstManager.AddComponent<GridPlane>(entity);
        dstManager.AddComponentData(entity, new GridConfig
        {
            RowCount = (ushort)RowCount,
            ColCount = (ushort)ColumnCount
        });
        
        GridAuthoringUtility.AddTrailingOffsets(dstManager, entity, RowCount, ColumnCount);

        gridWalls.Dispose();
    }
}
