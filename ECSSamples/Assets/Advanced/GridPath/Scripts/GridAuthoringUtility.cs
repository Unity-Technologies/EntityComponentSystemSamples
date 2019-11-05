using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using Hash128 = UnityEngine.Hash128;

public static class GridAuthoringUtility
{
    public enum WallFlags : byte
    {
        SouthWall = 1,
        WestWall = 2,
    }
    
    public static void CreateGridPath(int rowCount, int columnCount, NativeArray<GridWalls> gridWalls, float wallSProbability, float wallWProbability, bool outerWalls)
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

    public static void AddTrailingOffsets(EntityManager dstManager, Entity entity, int rowCount, int columnCount)
    {
        var cx = (columnCount * 0.5f);
        var cz = (rowCount * 0.5f);
        
        // Trailing offset so something is not considered "in" a grid section until it is 
        // *completely* in that grid section. So no check needs to be done to see if it is at
        // the center of the grid for correct turning timing. As soon as it's in the grid, it must
        // be in the right place to decide on new direction.
        var trailingOffsets = new NativeArray<GridTrailingOffset>(4, Allocator.Temp);
        trailingOffsets[0] = new GridTrailingOffset { Value = new float2( cx +  0.0f, cz + -0.5f ) }; // North
        trailingOffsets[1] = new GridTrailingOffset { Value = new float2( cx +  0.0f, cz +  0.5f ) }; // South
        trailingOffsets[2] = new GridTrailingOffset { Value = new float2( cx +  0.5f, cz +  0.0f ) }; // West
        trailingOffsets[3] = new GridTrailingOffset { Value = new float2( cx + -0.5f, cz +  0.0f ) }; // East
        
        var gridTrailingOffsetBuffer = dstManager.AddBuffer<GridTrailingOffset>(entity);
        gridTrailingOffsetBuffer.AddRange(trailingOffsets);
        trailingOffsets.Dispose();
    }

    [BurstCompile]
    struct BuildGridPath : IJob
    {
        public NativeArray<GridWalls> GridWalls;
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
            var GridWallsSW = new NativeArray<byte>(GridWallsRowCount * GridWallsColumnCount, Allocator.Temp);
            
            // Populate the grid
            for (int y = 0; y < GridWallsRowCount; y++)
            for (int x = 0; x < GridWallsColumnCount; x++)
            {
                // By default place outer walls along the edge of the grid.
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

                    var n0 = noise.snoise(new float2(tx * 1.2345f, tz * 1.2345f));
                    var n1 = noise.snoise(new float2(tz * 1.6789f, tx * 1.6789f));

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
                walls |= GridWalls[gridWallIndex].Value;
                GridWalls[gridWallIndex] = new GridWalls { Value = (byte)walls };
            }
        }
    }
    
    public static void CreateFloorPanel(EntityManager dstManager, GameObjectConversionSystem conversionSystem, GameObject gameObject, GameObject prefab, float4x4 parentLocalToWorld, float tx, float tz)
    {
        var pos = new float3(tx + 0.5f, 0.0f, tz + 0.5f);
        var childLocalToParent = math.mul(float4x4.Translate(pos), float4x4.Scale(0.98f));
        var localToWorld = new LocalToWorld
        {
            Value = math.mul(parentLocalToWorld,childLocalToParent)
        };
        
        CreatePanel(dstManager, conversionSystem, gameObject, prefab, localToWorld);
    }
    
    public static void CreateWallS(EntityManager dstManager, GameObjectConversionSystem conversionSystem, GameObject gameObject, GameObject prefab, float4x4 parentLocalToWorld, float tx, float tz)
    {
        var pos = new float3(tx + 0.5f, 1.0f, tz); 
        var childLocalToParent = math.mul(float4x4.Translate(pos), float4x4.Scale(1.1f, 1.1f, 0.1f));
        var localToWorld = new LocalToWorld
        {
            Value = math.mul(parentLocalToWorld,childLocalToParent)
        };
        
        CreatePanel(dstManager, conversionSystem, gameObject, prefab, localToWorld);
    }
    
    public static void CreateWallW(EntityManager dstManager, GameObjectConversionSystem conversionSystem, GameObject gameObject, GameObject prefab, float4x4 parentLocalToWorld, float tx, float tz)
    {
        var pos = new float3(tx, 1.0f, tz + 0.5f);
        var childLocalToParent = math.mul(float4x4.Translate(pos), float4x4.Scale(0.1f, 1.1f, 1.1f));
        var localToWorld = new LocalToWorld
        {
            Value = math.mul(parentLocalToWorld,childLocalToParent)
        };

        CreatePanel(dstManager, conversionSystem, gameObject, prefab, localToWorld);
    }
    
    public static void CreatePanel(EntityManager dstManager, GameObjectConversionSystem conversionSystem, GameObject gameObject, GameObject prefab, LocalToWorld localToWorld)
    {
        var meshRenderer = prefab.GetComponent<MeshRenderer>();
        var meshFilter = prefab.GetComponent<MeshFilter>();
        var materials = new List<Material>(10);
        var mesh = meshFilter.sharedMesh;
        meshRenderer.GetSharedMaterials(materials);

        var segmentEntity = conversionSystem.CreateAdditionalEntity(gameObject);
        var pos = localToWorld.Position;
        
        var renderBounds = new RenderBounds
        {
            Value = new AABB
            {
                Center = new float3(0.0f, 0.0f, 0.0f),
                Extents = new float3(0.5f, 0.5f, 0.5f)
            }
        };
        var worldRenderBounds = new WorldRenderBounds
        {
            Value = new AABB
            {
                Center = pos,
                Extents = new float3(0.5f, 0.5f, 0.5f)
            }
        };
        var frozenRenderSceneTag = new FrozenRenderSceneTag
        {
            HasStreamedLOD = 0,
            SceneGUID = Hash128.Compute("Grid Panel"),
            SectionIndex = 0
        };

#if UNITY_EDITOR
        dstManager.SetName(segmentEntity, "Grid Panel");
#endif
        dstManager.AddComponentData(segmentEntity, localToWorld);
        dstManager.AddComponentData(segmentEntity, renderBounds);

        dstManager.AddComponentData(segmentEntity, worldRenderBounds);
        dstManager.AddSharedComponentData(segmentEntity, frozenRenderSceneTag);
        dstManager.AddComponent(segmentEntity, typeof(Static));

        CreateRenderMesh(segmentEntity, dstManager, conversionSystem, meshRenderer, mesh, materials);
    }

    static void CreateRenderMesh( Entity entity, EntityManager dstEntityManager, GameObjectConversionSystem conversionSystem, Renderer meshRenderer, Mesh mesh, List<Material> materials)
    {
        var materialCount = materials.Count;

        // Don't add RenderMesh (and other required components) unless both mesh and material assigned.
        if ((mesh != null) && (materialCount > 0))
        {
            var renderMesh = new RenderMesh
            {
                mesh = mesh,
                castShadows = meshRenderer.shadowCastingMode,
                receiveShadows = meshRenderer.receiveShadows,
                layer = meshRenderer.gameObject.layer
            };

            //@TODO: Transform system should handle RenderMeshFlippedWindingTag automatically. This should not be the responsibility of the conversion system.
            float4x4 localToWorld = meshRenderer.transform.localToWorldMatrix;
            var flipWinding = math.determinant(localToWorld) < 0.0;

            if (materialCount == 1)
            {
                renderMesh.material = materials[0];
                renderMesh.subMesh = 0;

                dstEntityManager.AddSharedComponentData(entity, renderMesh);

                dstEntityManager.AddComponentData(entity, new PerInstanceCullingTag());
                dstEntityManager.AddComponentData(entity, new RenderBounds { Value = mesh.bounds.ToAABB() });

                if (flipWinding)
                    dstEntityManager.AddComponent(entity, ComponentType.ReadWrite<RenderMeshFlippedWindingTag>());

                conversionSystem.ConfigureEditorRenderData(entity, meshRenderer.gameObject, true);
            }
            else
            {
                for (var m = 0; m != materialCount; m++)
                {
                    var meshEntity = conversionSystem.CreateAdditionalEntity(meshRenderer);

                    renderMesh.material = materials[m];
                    renderMesh.subMesh = m;

                    dstEntityManager.AddSharedComponentData(meshEntity, renderMesh);

                    dstEntityManager.AddComponentData(meshEntity, new PerInstanceCullingTag());
                    dstEntityManager.AddComponentData(meshEntity, new RenderBounds { Value = mesh.bounds.ToAABB() });
                    dstEntityManager.AddComponentData(meshEntity, new LocalToWorld { Value = localToWorld });

                    if (!dstEntityManager.HasComponent<Static>(meshEntity))
                    {
                        dstEntityManager.AddComponentData(meshEntity, new Parent { Value = entity });
                        dstEntityManager.AddComponentData(meshEntity, new LocalToParent { Value = float4x4.identity });
                    }

                    if (flipWinding)
                        dstEntityManager.AddComponent(meshEntity, ComponentType.ReadWrite<RenderMeshFlippedWindingTag>());

                    conversionSystem.ConfigureEditorRenderData(meshEntity, meshRenderer.gameObject, true);
                }
            }
        }
    }
}