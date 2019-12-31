using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities.Tests;
using Unity.Mathematics;

namespace Samples.GridPath.Tests
{
    [TestFixture]
    unsafe class CartesianGridOnCubeShortestPathTests : ECSTestsFixture
    {
        struct TestGrid : IDisposable
        {
            public int RowCount;
            public byte* Walls;
            public float4x4* FaceLocalToWorld;
            public float4x4* FaceWorldToLocal;
            public float4x4* FaceLocalToLocal;
            
            public TestGrid(int rowCount)
            {
                RowCount = rowCount;
                
                int wallRowStride = (rowCount + 1) / 2;
                int wallFaceStride = rowCount * wallRowStride;
                int wallsSize = 6 * wallFaceStride;

                Walls = (byte*) UnsafeUtility.Malloc(wallsSize, 16, Allocator.Temp);
                UnsafeUtility.MemClear(Walls, wallsSize);
                for (int i = 0; i < wallsSize; i++)
                {
                    Assert.AreEqual(Walls[i],0);
                }

                FaceWorldToLocal = (float4x4*) UnsafeUtility.Malloc(sizeof(float4x4) * 6, 16, Allocator.Temp);
                FaceLocalToWorld = (float4x4*) UnsafeUtility.Malloc(sizeof(float4x4) * 6, 16, Allocator.Temp);
                FaceLocalToLocal = (float4x4*) UnsafeUtility.Malloc(sizeof(float4x4) * 6 * 6, 16, Allocator.Temp);

                CartesianGridGeneratorUtility.FillCubeFaceTransforms(rowCount, FaceLocalToWorld, FaceWorldToLocal, FaceLocalToLocal);
            }
            
            public void Dispose()
            {
                if (Walls != null)
                    UnsafeUtility.Free(Walls, Allocator.Temp);
                if (FaceLocalToLocal != null)
                    UnsafeUtility.Free(FaceLocalToLocal, Allocator.Temp);
                if (FaceLocalToWorld != null)
                    UnsafeUtility.Free(FaceLocalToWorld, Allocator.Temp);
                if (FaceWorldToLocal != null)
                    UnsafeUtility.Free(FaceWorldToLocal, Allocator.Temp);
            }

            public void SetWallBit(int cellIndex, CartesianGridDirectionBit directionBit)
            {
                var cellPosition = CartesianGridOnCubeUtility.CellFaceCoordinates(cellIndex, RowCount);
                var faceIndex = CartesianGridOnCubeUtility.CellFaceIndex(cellIndex, RowCount);
                var rowStride = (RowCount + 1) / 2;
                var faceStride = RowCount * rowStride;
                var faceGridWallsOffset = faceIndex * faceStride;
                var x = cellPosition.x;
                var y = cellPosition.y;
                var gridWallsIndex = faceGridWallsOffset + ((y * ((RowCount + 1) / 2)) + (x / 2));
                
                Assert.IsTrue(cellPosition.OnGrid(RowCount,RowCount));
                
                Walls[gridWallsIndex] |= (byte)((byte)directionBit << (4 * (x & 1)));
            }
            
            public bool TestWallBit(int cellIndex, CartesianGridDirectionBit directionBit)
            {
                var cellPosition = CartesianGridOnCubeUtility.CellFaceCoordinates(cellIndex, RowCount);
                var faceIndex = CartesianGridOnCubeUtility.CellFaceIndex(cellIndex, RowCount);
                var rowStride = (RowCount + 1) / 2;
                var faceStride = RowCount * rowStride;
                var faceGridWallsOffset = faceIndex * faceStride;
                var x = cellPosition.x;
                var y = cellPosition.y;
                var gridWallsIndex = faceGridWallsOffset + ((y * ((RowCount + 1) / 2)) + (x / 2));
                
                Assert.IsTrue(cellPosition.OnGrid(RowCount,RowCount));
                
                return (((Walls[gridWallsIndex] >> (4 * (x&1))) & (byte)directionBit) == (byte)directionBit);
            }
                    
            public void AddWallSouth(int cellIndex)
            {
                SetWallBit(cellIndex, CartesianGridDirectionBit.South);
                
                var cellIndexSouth = CartesianGridOnCubeUtility.CellIndexSouth(cellIndex, RowCount, FaceLocalToLocal);
                SetWallBit(cellIndexSouth, CartesianGridDirectionBit.North);
            }
            public void AddWallWest(int cellIndex)
            {
                SetWallBit(cellIndex, CartesianGridDirectionBit.West);
                
                var cellIndexWest = CartesianGridOnCubeUtility.CellIndexWest(cellIndex, RowCount, FaceLocalToLocal);
                SetWallBit(cellIndexWest, CartesianGridDirectionBit.East);
            }

            int WalkPath(int cellIndex, NativeArray<byte> targetDirections, int pathOffset)
            {
                var cellPosition = CartesianGridOnCubeUtility.CellFaceCoordinates(cellIndex, RowCount);
                var faceIndex = CartesianGridOnCubeUtility.CellFaceIndex(cellIndex, RowCount);
                
                var validDirections = CartesianGridOnCubeShortestPath.LookupDirectionToTarget(cellPosition.x, cellPosition.y, faceIndex, RowCount, targetDirections);
                var direction = CartesianGridMovement.PathVariation[((pathOffset&3) * 16) + validDirections];
                
                if (direction == 0xff) // No path
                    return 0;

                var nextCellIndex = -1;
                if (direction == 0)
                    nextCellIndex = CartesianGridOnCubeUtility.CellIndexNorth(cellIndex, RowCount, FaceLocalToLocal);
                else if (direction == 1)
                    nextCellIndex = CartesianGridOnCubeUtility.CellIndexSouth(cellIndex, RowCount, FaceLocalToLocal);
                else if (direction == 2)
                    nextCellIndex = CartesianGridOnCubeUtility.CellIndexWest(cellIndex, RowCount, FaceLocalToLocal);
                else if (direction == 3)
                    nextCellIndex = CartesianGridOnCubeUtility.CellIndexEast(cellIndex, RowCount, FaceLocalToLocal);
                else
                    Assert.Fail();
                
                // Test no wall in the direction given
                if (direction == 0)
                    Assert.IsFalse(TestWallBit(cellIndex, CartesianGridDirectionBit.North));
                else if (direction == 1)
                    Assert.IsFalse(TestWallBit(cellIndex, CartesianGridDirectionBit.South));
                else if (direction == 2)
                    Assert.IsFalse(TestWallBit(cellIndex, CartesianGridDirectionBit.West));
                else if (direction == 3)
                    Assert.IsFalse(TestWallBit(cellIndex, CartesianGridDirectionBit.East));

                return 1 + WalkPath(nextCellIndex, targetDirections, pathOffset);
            }

            public int WalkPathDistance(CartesianGridCoordinates sourcePosition, CartesianGridOnCubeFace sourceCubeFace, CartesianGridCoordinates targetPosition, CartesianGridOnCubeFace targetCubeFace)
            {
                var directionsRowStride = (RowCount + 1) / 2;
                var directionsSize = 6 * RowCount * directionsRowStride;
                var distancesSize = RowCount * RowCount * 6;
                
                var targetDirections = new NativeArray<byte>(directionsSize, Allocator.Temp);
                var sourceDirections = new NativeArray<byte>(directionsSize, Allocator.Temp);
                var targetDistances = new NativeArray<int>(distancesSize, Allocator.Temp);
                var sourceDistances = new NativeArray<int>(distancesSize, Allocator.Temp);
                
                // For testing purposes, recalculate paths every time.
                CartesianGridOnCubeShortestPath.CalculateShortestPathsToTarget(targetDirections, targetDistances, RowCount, targetPosition, targetCubeFace, Walls, FaceLocalToLocal);
                CartesianGridOnCubeShortestPath.CalculateShortestPathsToTarget(sourceDirections, sourceDistances, RowCount, sourcePosition, sourceCubeFace, Walls, FaceLocalToLocal);
                
                // Test distance form source->target is same as target->source
                var sourceCellIndex = CartesianGridOnCubeUtility.CellIndex(sourcePosition, sourceCubeFace, RowCount);
                var targetCellIndex = CartesianGridOnCubeUtility.CellIndex(targetPosition, targetCubeFace, RowCount);
                
                var sourceToTargetDistance = WalkPath(sourceCellIndex, targetDirections, 0);
                var targetToSourceDistance = WalkPath(targetCellIndex, sourceDirections, 0);
                
                Assert.AreEqual(sourceToTargetDistance, targetToSourceDistance);

                var expectedDistance = sourceToTargetDistance;
                
                // No surprises on stored distances
                Assert.AreEqual( sourceToTargetDistance, sourceDistances[targetCellIndex] );
                Assert.AreEqual( sourceToTargetDistance, targetDistances[sourceCellIndex] );
                    
                // Sample path variations (not exhaustive, always follow the variation path option)
                for (int i = 1; i < 4; i++)
                {
                    // Test distance form source->target is same as target->source
                    // Test distance is same for all variations
                    sourceToTargetDistance = WalkPath(sourceCellIndex, targetDirections, i);
                    Assert.AreEqual(expectedDistance, sourceToTargetDistance);
                    
                    targetToSourceDistance = WalkPath(targetCellIndex, sourceDirections, i);
                    Assert.AreEqual(expectedDistance, targetToSourceDistance);
                }
                
                targetDirections.Dispose();
                sourceDirections.Dispose();
                targetDistances.Dispose();
                sourceDistances.Dispose();

                return expectedDistance;
            }
        }

        void TestWrappingEdgeNorth(TestGrid grid)
        {
            int x;
            int y;
            
            for (int faceIndex = 0; faceIndex < 6; faceIndex++)
            {
                y = grid.RowCount - 1;
                for (x = 0; x < grid.RowCount; x++)
                {
                    var sourcePosition = new CartesianGridCoordinates {x = (short) x, y = (short) y};
                    var sourceCubeFace = new CartesianGridOnCubeFace {Value = (byte) faceIndex};
                    var sourceCellIndex = CartesianGridOnCubeUtility.CellIndex(sourcePosition, sourceCubeFace, grid.RowCount);
                    var exitPosition = new CartesianGridCoordinates {x = (short) x, y = (short) (y + 1)};
                    var edge = CartesianGridMovement.CubeExitEdge(exitPosition, grid.RowCount);
                    Assert.AreEqual(edge, 0);
                    
                    var nextCellIndex = CartesianGridOnCubeUtility.CellIndexNorth(sourceCellIndex, grid.RowCount, grid.FaceLocalToLocal);
                    var nextDirection = CartesianGridOnCubeUtility.NextFaceDirection[(edge * 6) + faceIndex];
                    var reverseDirection = CartesianGridMovement.ReverseDirection[nextDirection];
                    var returnCellIndex = CartesianGridOnCubeUtility.CellIndexFromExitEdge(reverseDirection, nextCellIndex, grid.RowCount);
                    
                    Assert.AreEqual(returnCellIndex, sourceCellIndex);
                }
            }
        }
        
        void TestWrappingEdgeSouth(TestGrid grid)
        {
            int x;
            int y;
            
            for (int faceIndex = 0; faceIndex < 6; faceIndex++)
            {
                y = 0;
                for (x = 0; x < grid.RowCount; x++)
                {
                    var sourcePosition = new CartesianGridCoordinates {x = (short) x, y = (short) y};
                    var sourceCubeFace = new CartesianGridOnCubeFace {Value = (byte) faceIndex};
                    var sourceCellIndex = CartesianGridOnCubeUtility.CellIndex(sourcePosition, sourceCubeFace, grid.RowCount);
                    var exitPosition = new CartesianGridCoordinates {x = (short) x, y = (short) (y - 1)};
                    var edge = CartesianGridMovement.CubeExitEdge(exitPosition, grid.RowCount);
                    Assert.AreEqual(edge, 1);

                    var nextCellIndex = CartesianGridOnCubeUtility.CellIndexSouth(sourceCellIndex, grid.RowCount, grid.FaceLocalToLocal);
                    var nextDirection = CartesianGridOnCubeUtility.NextFaceDirection[(edge * 6) + faceIndex];
                    var reverseDirection = CartesianGridMovement.ReverseDirection[nextDirection];
                    var returnCellIndex = CartesianGridOnCubeUtility.CellIndexFromExitEdge(reverseDirection, nextCellIndex, grid.RowCount);
                    
                    Assert.AreEqual(returnCellIndex, sourceCellIndex);
                }
            }
        }
        
        void TestWrappingEdgeWest(TestGrid grid)
        {
            int x;
            int y;
            
            for (int faceIndex = 0; faceIndex < 6; faceIndex++)
            {
                x = 0;
                for (y = 0; y < grid.RowCount; y++)
                {
                    var sourcePosition = new CartesianGridCoordinates {x = (short) x, y = (short) y};
                    var sourceCubeFace = new CartesianGridOnCubeFace {Value = (byte) faceIndex};
                    var sourceCellIndex = CartesianGridOnCubeUtility.CellIndex(sourcePosition, sourceCubeFace, grid.RowCount);
                    var exitPosition = new CartesianGridCoordinates {x = (short) (x-1), y = (short) y};
                    var edge = CartesianGridMovement.CubeExitEdge(exitPosition, grid.RowCount);
                    Assert.AreEqual(edge, 2);
                    
                    var nextCellIndex = CartesianGridOnCubeUtility.CellIndexWest(sourceCellIndex, grid.RowCount, grid.FaceLocalToLocal);
                    var nextDirection = CartesianGridOnCubeUtility.NextFaceDirection[(edge * 6) + faceIndex];
                    var reverseDirection = CartesianGridMovement.ReverseDirection[nextDirection];
                    var returnCellIndex = CartesianGridOnCubeUtility.CellIndexFromExitEdge(reverseDirection, nextCellIndex, grid.RowCount);
                    
                    Assert.AreEqual(returnCellIndex, sourceCellIndex);
                }
            }
        }
        
        void TestWrappingEdgeEast(TestGrid grid)
        {
            int x;
            int y;

            for (int faceIndex = 0; faceIndex < 6; faceIndex++)
            {
                x = grid.RowCount-1;
                for (y = 0; y < grid.RowCount; y++)
                {
                    var sourcePosition = new CartesianGridCoordinates {x = (short) x, y = (short) y};
                    var sourceCubeFace = new CartesianGridOnCubeFace {Value = (byte) faceIndex};
                    var sourceCellIndex = CartesianGridOnCubeUtility.CellIndex(sourcePosition, sourceCubeFace, grid.RowCount);
                    var exitPosition = new CartesianGridCoordinates {x = (short) (x+1), y = (short) y};
                    var edge = CartesianGridMovement.CubeExitEdge(exitPosition, grid.RowCount);
                    Assert.AreEqual(edge, 3);
                    
                    var nextCellIndex = CartesianGridOnCubeUtility.CellIndexEast(sourceCellIndex, grid.RowCount, grid.FaceLocalToLocal);
                    var nextDirection = CartesianGridOnCubeUtility.NextFaceDirection[(edge * 6) + faceIndex];
                    var reverseDirection = CartesianGridMovement.ReverseDirection[nextDirection];
                    var returnCellIndex = CartesianGridOnCubeUtility.CellIndexFromExitEdge(reverseDirection, nextCellIndex, grid.RowCount);
                    
                    Assert.AreEqual(returnCellIndex, sourceCellIndex);
                }
            }
        }

        [Test]
        public void MatchingEdgesEvenSize()
        {
            using (var grid = new TestGrid(16))
            {
                TestWrappingEdgeNorth(grid);
                TestWrappingEdgeSouth(grid);
                TestWrappingEdgeWest(grid);
                TestWrappingEdgeEast(grid);
            }
        }
        
        [Test]
        public void MatchingEdgesOddSize()
        {
            using (var grid = new TestGrid(17))
            {
                TestWrappingEdgeNorth(grid);
                TestWrappingEdgeSouth(grid);
                TestWrappingEdgeWest(grid);
                TestWrappingEdgeEast(grid);
            }
        }

        [Test]
        public void PathOnEmptyGrid()
        {
            using (var grid = new TestGrid(16))
            {
                var targetPosition = new CartesianGridCoordinates {x = 15, y = 15};
                var targetCubeFace = new CartesianGridOnCubeFace {Value = 0};
                var sourcePosition = new CartesianGridCoordinates {x = 8, y = 8};
                var sourceCubeFace0 = new CartesianGridOnCubeFace {Value = 0};
                var sourceCubeFace1 = new CartesianGridOnCubeFace {Value = 1};
                var sourceCubeFace2 = new CartesianGridOnCubeFace {Value = 2};
                var sourceCubeFace3 = new CartesianGridOnCubeFace {Value = 3};
                var sourceCubeFace4 = new CartesianGridOnCubeFace {Value = 4};
                var sourceCubeFace5 = new CartesianGridOnCubeFace {Value = 5};

                var dist0 = grid.WalkPathDistance(sourcePosition, sourceCubeFace0, targetPosition, targetCubeFace);
                Assert.AreEqual(dist0, 14 );
                
                var dist1 = grid.WalkPathDistance(sourcePosition, sourceCubeFace1, targetPosition, targetCubeFace);
                Assert.AreEqual(dist1, 32);
                
                var dist2 = grid.WalkPathDistance(sourcePosition, sourceCubeFace2, targetPosition, targetCubeFace);
                Assert.AreEqual(dist2, 30);
                
                var dist3 = grid.WalkPathDistance(sourcePosition, sourceCubeFace3, targetPosition, targetCubeFace);
                Assert.AreEqual(dist3, 16);
                
                var dist4 = grid.WalkPathDistance(sourcePosition, sourceCubeFace4, targetPosition, targetCubeFace);
                Assert.AreEqual(dist4, 17 );
                
                var dist5 = grid.WalkPathDistance(sourcePosition, sourceCubeFace5, targetPosition, targetCubeFace);
                Assert.AreEqual(dist5, 31);
            }
        }
  
        [Test]
        public void PathRandomObstacles()
        {
            using (var grid = new TestGrid(31))
            {
                int obstacleCount = 32;
                var rand = new Unity.Mathematics.Random(0xF545AA3F);
                for (int i = 0; i < obstacleCount; i++)
                {
                    var faceIndex = rand.NextInt(0, 6);
                    var xy = rand.NextInt2(new int2(grid.RowCount, grid.RowCount));
                    var sourcePosition = new CartesianGridCoordinates {x = (short)xy.x, y = (short)xy.y};
                    var cubeFace = new CartesianGridOnCubeFace { Value = (byte)faceIndex};
                    var cellIndex = CartesianGridOnCubeUtility.CellIndex(sourcePosition, cubeFace, grid.RowCount);
                    if (rand.NextBool())
                        grid.AddWallWest(cellIndex);
                    else
                        grid.AddWallSouth(cellIndex);
                }

                int testCount = 64;
                for (int i = 0; i < testCount; i++)
                {
                    var sourceXY = rand.NextInt2(new int2(grid.RowCount, grid.RowCount));
                    var sourceFaceIndex = rand.NextInt(0, 6);
                    var sourceCubeFace = new CartesianGridOnCubeFace { Value = (byte)sourceFaceIndex};
                    var sourcePosition = new CartesianGridCoordinates {x = (short)sourceXY.x, y = (short)sourceXY.y};
                    
                    var targetXY = rand.NextInt2(new int2(grid.RowCount, grid.RowCount));
                    var targetFaceIndex = rand.NextInt(0, 6);
                    var targetCubeFace = new CartesianGridOnCubeFace { Value = (byte)targetFaceIndex};
                    var targetPosition = new CartesianGridCoordinates {x = (short)targetXY.x, y = (short)targetXY.y};
                    
                    grid.WalkPathDistance(sourcePosition, sourceCubeFace, targetPosition, targetCubeFace);
                }
            }
        }
    }
}
