using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// Update movement on GridPlane and GridCube
// - Simple move around walls
public class GridPathMovementSystem : JobComponentSystem
{
    EntityQuery m_GridQuery;

    // Convenience vectors for turning direction.
    static readonly float[] m_UnitMovement =
    {
        0.0f, 1.0f, // North
        0.0f, -1.0f, // South
        -1.0f, 0.0f, // West
        1.0f, 0.0f, // East
    };

    // Next Direction lookup by grid element walls
    //   - Calculate gridX, gridY based on current actual position (Translation)
    //   - Get 4 path options = [(gridY * rowCount)+gridX]
    //   - Select path option based on current direction (Each of 4 direction 2bits of result)
    static readonly byte[] m_NextDirection =
    {
        // Standard paths. Bounce off walls.
        // Two paths because two directions can be equally likely.

        // PathSet[0]
        0xe4, 0xe7, 0xec, 0xef, 0xc4, 0xd7, 0xcc, 0xff,
        0x24, 0x66, 0x28, 0xaa, 0x04, 0x55, 0x00, 0xe4,

        // PathSet[1]
        0xe4, 0xe6, 0xe8, 0xea, 0xd4, 0xd7, 0xcc, 0xff,
        0x64, 0x66, 0x28, 0xaa, 0x54, 0x55, 0x00, 0xe4,

        // Path (rare) variations below    
        // Very occasionally, move without bouncing off wall.

        // Assume north wall
        0xe6, 0xe6, 0xea, 0xea, 0xd7, 0xd7, 0xff, 0xff,
        0x66, 0x66, 0xaa, 0xaa, 0x55, 0x55, 0x00, 0xe4,

        // Assume south wall
        0xe8, 0xea, 0xe8, 0xea, 0xcc, 0xff, 0xcc, 0xff,
        0x28, 0xaa, 0x28, 0xaa, 0x00, 0x55, 0x00, 0xe4,

        // Assume west wall
        0xd4, 0xd7, 0xcc, 0xff, 0xd4, 0xd7, 0xcc, 0xff,
        0x54, 0x55, 0x00, 0xaa, 0x54, 0x55, 0x00, 0xe4,

        // Assume east wall
        0x64, 0x66, 0x28, 0xaa, 0x54, 0x55, 0x00, 0xff,
        0x64, 0x66, 0x28, 0xaa, 0x54, 0x55, 0x00, 0xe4,
    };

    // Next face to move to when moving off edge of a face
    static readonly byte[] m_NextFaceIndex =
    {
        // X+ X- Y+ Y- Z+ Z- <- From which face
        4, 4, 4, 4, 3, 2, // Off north edge
        5, 5, 5, 5, 2, 3, // Off south edge
        2, 3, 1, 0, 1, 1, // Off west edge
        3, 2, 0, 1, 0, 0, // Off east edge
    };

    static readonly byte[] m_NextFaceDirection =
    {
        // X+ X- Y+ Y- Z+ Z- <- From which face
        2, 3, 0, 1, 1, 0, // Off north edge
        2, 3, 1, 0, 1, 0, // Off south edge
        2, 2, 2, 2, 1, 0, // Off west edge
        3, 3, 3, 3, 1, 0, // Off east edge
    };
    
    // Arbitrarily select direction when two directions are equally valid
    int m_NextDirectionBufferSelect = 0;
    int m_NextDirectionVariationSelect = 0;

    protected override void OnCreate()
    {
        m_GridQuery = GetEntityQuery(
            ComponentType.ReadOnly<GridConfig>(),
            ComponentType.ReadOnly<GridWalls>());
    }

    protected override JobHandle OnUpdate(JobHandle lastJobHandle)
    {
        // There's only one grid. Make sure it exists before start moving.
        if (m_GridQuery.CalculateEntityCount() != 1)
            return lastJobHandle;

        // Flip which arbitrary direction would be selected.
        int directionBufferIndex = 0;

        // Once every 16 frames, select an arbitrary variation (if happen to be crossing threshold)
        // Both crossing a threshold and hitting a variation at the same time is a very rare event.
        // The purpose of these variations is to occasionally kick things out that are caught in a
        // movement loop.
        if (m_NextDirectionBufferSelect == 15)
        {
            directionBufferIndex = 2 + m_NextDirectionVariationSelect;
            m_NextDirectionVariationSelect = (m_NextDirectionVariationSelect + 1) & 0x03;
        }

        // Otherwise select one of two main paths
        else
        {
            directionBufferIndex = m_NextDirectionBufferSelect & 1;
        }

        // Update selection frame counter
        m_NextDirectionBufferSelect = (m_NextDirectionBufferSelect + 1) & 15;

        // Get component data from the Grid (GridPlane or GridCube)
        var gridConfigFromEntity = GetComponentDataFromEntity<GridConfig>(true);
        var gridWallsFromEntity = GetBufferFromEntity<GridWalls>(true);
        var trailingOffsetsFromEntity = GetBufferFromEntity<GridTrailingOffset>(true);
        var faceLocalToWorldFromEntity = GetBufferFromEntity<FaceLocalToWorld>(true);
        var gridEntity = m_GridQuery.GetSingletonEntity();
        var onGridCube = EntityManager.HasComponent<GridCube>(gridEntity);
        var onGridPlane = EntityManager.HasComponent<GridPlane>(gridEntity);
        var gridConfig = gridConfigFromEntity[gridEntity];
        var gridWalls = gridWallsFromEntity[gridEntity].Reinterpret<byte>().AsNativeArray();
        
        // Trailing edge of movement (relative to center)
        // - Trailing edge is used to determine grid section to make sure the object is *completely* in the
        //   grid section before being considered in the grid section.
        var trailingOffsets = trailingOffsetsFromEntity[gridEntity].Reinterpret<float2>().AsNativeArray();

        if (!(onGridCube || onGridPlane))
            return lastJobHandle;

        var faceLocalToWorld = new NativeArray<float4x4>();
        if (faceLocalToWorldFromEntity.Exists(gridEntity))
        {
            // FaceLocalToWorld is provides:
            // LocalToWorld float4x4 per face (6)
            //   - In order of X+, X-, Y+, Y-, Z+, Z-
            //   - Which is the same order as referenced by GridDirection
            // LocalToLocal float4x4 per pair of faces (36)
            //   - For each source face, in order of X+, X-, Y+, Y-, Z+, Z-
            //       - For each destination face of X+, X-, Y+, Y-, Z+, Z-
            //   - The diagonal along this 6x6 matrix of float4x4 is unused, but simplifies lookup.
            faceLocalToWorld = faceLocalToWorldFromEntity[gridEntity].Reinterpret<float4x4>().AsNativeArray();
        }

        // Starting index into the LocalToLocal matrices described above
        int faceLocalToLocalOffset = 6;

        // Board size (rowCount == colCount when GridCube)
        var rowCount = gridConfig.RowCount;
        var colCount = gridConfig.ColCount;
        var rowStride = ((colCount + 1) / 2);

        // Two global variations of paths divided by 0.5 probability.
        var pathOffset = 16 * directionBufferIndex;

        // Offset center to grid cell
        var cellCenterOffset = new float2(((float)colCount * 0.5f) - 0.5f, ((float)rowCount * 0.5f) - 0.5f);

        // Clamp delta time so you can't overshoot.
        var deltaTime = math.min(Time.DeltaTime, 0.05f);

        // Change direction for GridCube, including traveling around corners and changing active face.
        if (onGridCube)
            lastJobHandle = Entities
                .WithName("GridCubeChangeDirection")
                .WithAll<GridCube>()
                .WithReadOnly(gridWalls)
                .WithReadOnly(faceLocalToWorld)
                .WithReadOnly(trailingOffsets)
                .ForEach((ref GridDirection gridDirection,
                    ref Translation translation,
                    ref GridPosition gridPosition,
                    ref GridFace gridFace) =>
                {
                    var prevDir = gridDirection.Value;
                    var nextGridPosition = new GridPosition(translation.Value.xz + trailingOffsets[prevDir], rowCount, rowCount);
                    if (gridPosition.Equals(nextGridPosition))
                        return; // Still in the same grid cell. No need to change direction.

                    // Which edge of GridCube face is being exited (if any)
                    var edge = -1;
                    
                    // Edge is in order specified in m_NextFaceIndex and m_NextFaceDirection 
                    // - Matches GridDirection values.

                    edge = math.select(edge, 0, nextGridPosition.y >= rowCount);
                    edge = math.select(edge, 1, nextGridPosition.y < 0);
                    edge = math.select(edge, 2, nextGridPosition.x < 0);
                    edge = math.select(edge, 3, nextGridPosition.x >= rowCount);

                    // Change direction based on wall layout (within current face.)
                    if (edge == -1)
                    {
                        gridPosition = nextGridPosition;
                        gridDirection.Value = LookupGridDirectionFromWalls( ref gridPosition, prevDir, rowStride, ref gridWalls, pathOffset);
                    }
                    
                    // Exiting face of GridCube, change face and direction relative to new face.
                    else
                    {
                        int prevFaceIndex = gridFace.Value;

                        // Look up next direction given previous face and exit edge.
                        var nextDir = m_NextFaceDirection[(edge * 6) + prevFaceIndex];
                        gridDirection.Value = nextDir;

                        // Lookup next face index given previous face and exit edge.
                        var nextFaceIndex = m_NextFaceIndex[(edge * 6) + prevFaceIndex];
                        gridFace.Value = nextFaceIndex;

                        // Transform translation relative to next face's grid-space
                        // - This transform is only done to "smooth" the transition around the edges.
                        // - Alternatively, you could "snap" to the same relative position in the next face by rotating the translation components.
                        // - Note that Y position won't be at target value from one edge to another, so that is interpolated in movement update,
                        //   purely for "smoothing" purposes.
                        var localToLocal = faceLocalToWorld[faceLocalToLocalOffset + ((prevFaceIndex * 6) + nextFaceIndex)];
                        translation.Value.xyz = math.mul(localToLocal, new float4(translation.Value, 1.0f)).xyz;

                        // Update gridPosition relative to new face.
                        gridPosition = new GridPosition(translation.Value.xz + trailingOffsets[nextDir], rowCount, rowCount);
                    }
                }).Schedule(lastJobHandle);

        // Change direction for gridPlane
        if (onGridPlane)
            lastJobHandle = Entities
                .WithName("GridPlaneChangeDirection")
                .WithAll<GridPlane>()
                .WithReadOnly(gridWalls)
                .WithReadOnly(trailingOffsets)
                .ForEach((ref GridDirection gridDirection,
                    ref GridPosition gridPosition,
                    in Translation translation) =>
                {
                    var dir = gridDirection.Value;
                    var nextGridPosition = new GridPosition(translation.Value.xz + trailingOffsets[dir], rowCount, colCount);
                    if (gridPosition.Equals(nextGridPosition))
                        return; // Still in the same grid cell. No need to change direction.

                    gridPosition = nextGridPosition;
                    gridDirection.Value = LookupGridDirectionFromWalls(ref gridPosition, dir, rowStride, ref gridWalls, pathOffset);
                }).Schedule(lastJobHandle);

        // Move forward along direction in grid-space given speed.
        // - This is the same for Plane or Cube and is the core of the movement code. Simply "move forward" along direction.
        lastJobHandle = Entities
            .WithName("GridMoveForward")
            .ForEach((ref Translation translation,
                in GridDirection gridDirection,
                in GridSpeed gridSpeed,
                in GridPosition gridPosition) =>
            {
                var dir = gridDirection.Value;

                // Don't allow translation to drift
                var pos = ClampToGrid(translation.Value, dir, gridPosition, cellCenterOffset);

                // Speed adjusted to float m/s from fixed point 6:10 m/s
                var speed = deltaTime * ((float)gridSpeed.Value) * (1.0f / 1024.0f);

                // Write: add unit vector offset scaled by speed and deltaTime to current position
                var dx = m_UnitMovement[(dir * 2) + 0] * speed;
                var dz = m_UnitMovement[(dir * 2) + 1] * speed;
                
                // Smooth y changes when transforming between cube faces. 
                var dy = math.min(speed, 1.0f - pos.y); 

                translation.Value = new float3(pos.x + dx, pos.y + dy, pos.z + dz);
            }).Schedule(lastJobHandle);

        // Transform from grid-space Translation and gridFace to LocalToWorld for GridCube
        // - This is an example of overriding the transform system's default behavior.
        // - GridFace is in the LocalToWorld WriteGroup, so when it this component is present, it is required to be
        //   part of the query in order to write to LocalToWorld. Since the transform system doesn't know anything
        //   about GridFace, it will never be present in those default transformations. So it can be handled custom
        //   here.
        if (onGridCube)
            lastJobHandle = Entities.WithAll<GridCube>()
                .WithName("GridCubeLocalToWorld")
                .ForEach((ref LocalToWorld localToWorld,
                    in Translation translation,
                    in GridFace gridFace) =>
                {
                    var resultLocalToWorld = faceLocalToWorld[gridFace.Value];
                    resultLocalToWorld.c3 = math.mul(resultLocalToWorld, new float4(translation.Value, 1.0f));

                    localToWorld = new LocalToWorld
                    {
                        Value = resultLocalToWorld
                    };
                }).Schedule(lastJobHandle);

        return lastJobHandle;
    }

    static float3 ClampToGrid(float3 v, byte dir, GridPosition gridPosition, float2 cellCenterOffset)
    {
        // When (dir == N,S) clamp to grid cell center x
        // When (dir == W,E) clamp to grid cell center y
        var mx = (dir >> 1) * 1.0f;
        var my = ((dir >> 1) ^ 1) * 1.0f;

        return new float3
        {
            x = (mx * v.x) + (my * (gridPosition.x - cellCenterOffset.x)),
            z = (my * v.z) + (mx * (gridPosition.y - cellCenterOffset.y)),
            y = v.y
        };
    }

    static byte LookupGridDirectionFromWalls(ref GridPosition gridPosition, byte dir, int rowStride, ref NativeArray<byte> gridWalls, int pathOffset)
    {
        // gridPosition needs to be on-grid (positive and < [colCount, rowCount]) when looking up next direction.
        
        // Index into grid array
        var gridWallsIndex = (gridPosition.y * rowStride) + (gridPosition.x / 2);

        // Walls in current grid element (odd columns in upper 4 bits of byte)
        var walls = (gridWalls[gridWallsIndex] >> ((gridPosition.x & 1) * 4)) & 0x0f;

        // New direction = f( grid index, movement direction )
        return (byte)((m_NextDirection[pathOffset + walls] >> (dir * 2)) & 0x03);
    }
}
