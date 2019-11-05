using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

// ReSharper disable once InconsistentNaming
[RequiresEntityConversion]
[AddComponentMenu("DOTS Samples/GridPath/Grid Cube")]
[ConverterVersion("joe", 1)]
public class GridCubeAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    [Range(2, 512)]
    public int RowCount;
    public GameObject[] FloorPrefab;
    public GameObject WallPrefab;

    // Specific wall probability, given PotentialWallProbability
    public float WallSProbability = 0.5f;
    public float WallWProbability = 0.5f;

    
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

    // Referenced prefabs have to be declared so that the conversion system knows about them ahead of time
    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(WallPrefab);
        referencedPrefabs.AddRange(FloorPrefab);
    }
    
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var floorPrefabCount = FloorPrefab.Length;
        if (floorPrefabCount == 0)
            return;

#if UNITY_EDITOR
        dstManager.SetName(entity, "Grid");
#endif

        var cx = (RowCount * 0.5f);
        var cz = (RowCount * 0.5f);

        var gridWallsBuffer = dstManager.AddBuffer<GridWalls>(entity);

        var faceLocalToWorld = new NativeArray<FaceLocalToWorld>(6, Allocator.Temp);
        var faceWorldToLocal = new NativeArray<FaceLocalToWorld>(6, Allocator.Temp);
        var faceLocalToLocal = new NativeArray<FaceLocalToWorld>(6 * 6, Allocator.Temp);

        for (int faceIndex = 0; faceIndex < 6; faceIndex++)
        {
            var localToWorld = m_FaceLocalToWorldRotation[faceIndex];
            
            // Translate along normal of face by width
            localToWorld.c3.xyz = localToWorld.c1.xyz * RowCount * 0.5f;

            faceLocalToWorld[faceIndex] = new FaceLocalToWorld
            {
                Value = localToWorld
            };
            faceWorldToLocal[faceIndex] = new FaceLocalToWorld
            {
                Value = math.fastinverse(faceLocalToWorld[faceIndex].Value)
            };
        }

        // Diagonal is identity and unused, but makes lookup simpler.
        for (int i = 0; i < 6; i++)
        {
            for (int j = 0; j < 6; j++)
            {
                faceLocalToLocal[(i * 6) + j] = new FaceLocalToWorld
                {
                    Value = math.mul(faceWorldToLocal[j].Value, faceLocalToWorld[i].Value)
                };
            }
        }

        for (int i = 0; i < 6; i++)
        {
            // 4 bits per grid section (bit:0=N,1=S,2=W,3=E)
            var gridWalls = new NativeArray<GridWalls>(RowCount * ((RowCount + 1) / 2), Allocator.Persistent);

            GridAuthoringUtility.CreateGridPath(RowCount, RowCount, gridWalls, WallSProbability, WallWProbability, false);

            // Create visible geometry
            for (int y = 0; y < RowCount; y++)
            for (int x = 0; x < RowCount; x++)
            {
                var prefabIndex = (x + y) % floorPrefabCount;
                var tx = ((float)x) - cx;
                var tz = ((float)y) - cz;

                GridAuthoringUtility.CreateFloorPanel(dstManager, conversionSystem, gameObject, FloorPrefab[prefabIndex], faceLocalToWorld[i].Value, tx, tz);
                
                var gridWallsIndex = (y * ((RowCount + 1) / 2)) + (x / 2);
                var walls = (gridWalls[gridWallsIndex].Value >> ((x & 1) * 4)) & 0x0f;

                if ((walls & 0x02) != 0) // South wall
                    GridAuthoringUtility.CreateWallS(dstManager, conversionSystem, gameObject, WallPrefab, faceLocalToWorld[i].Value, tx, tz);
                if ((walls & 0x04) != 0) // West wall
                    GridAuthoringUtility.CreateWallW(dstManager, conversionSystem, gameObject, WallPrefab, faceLocalToWorld[i].Value, tx, tz);
            }

            gridWallsBuffer = dstManager.GetBuffer<GridWalls>(entity);
            gridWallsBuffer.AddRange(gridWalls);

            gridWalls.Dispose();
        }

        dstManager.AddComponent<GridCube>(entity);
        dstManager.AddComponentData(entity, new GridConfig
        {
            RowCount = (ushort)RowCount,
            ColCount = (ushort)RowCount
        });

        var faceLocalToWorldBuffer = dstManager.AddBuffer<FaceLocalToWorld>(entity);
        faceLocalToWorldBuffer.AddRange(faceLocalToWorld);
        faceLocalToWorldBuffer.AddRange(faceLocalToLocal);

        GridAuthoringUtility.AddTrailingOffsets(dstManager, entity, RowCount, RowCount);

        faceLocalToWorld.Dispose();
        faceWorldToLocal.Dispose();
        faceLocalToLocal.Dispose();
    }
}
