using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities.Tests;
using Unity.Mathematics;

namespace Samples.GridPath.Tests
{
    [TestFixture]
    unsafe class CartesianGridOnPlaneShortestPathTests : ECSTestsFixture
    {
        struct TestGrid : IDisposable
        {
            public int RowCount;
            public int ColCount;
            public byte* Walls;
            public int WallRowStride => (ColCount + 1) / 2;

            public TestGrid(int rowCount, int colCount)
            {
                RowCount = rowCount;
                ColCount = colCount;
                
                int wallRowStride = (colCount + 1) / 2;
                int wallsSize = rowCount * wallRowStride;

                Walls = (byte*) UnsafeUtility.Malloc(wallsSize, 16, Allocator.Temp);
                UnsafeUtility.MemClear(Walls, wallsSize);
                
                // Add outer boundary walls.
                for (var x = 0; x < colCount; x++)
                    AddWallNorth(new CartesianGridCoordinates {x = (short) x, y = (short) (rowCount - 1)});
                for (var x = 0; x < colCount; x++)
                    AddWallSouth(new CartesianGridCoordinates {x = (short) x, y = (short) 0});
                for (var y = 0; y < rowCount; y++)
                    AddWallWest(new CartesianGridCoordinates {x = (short) 0, y = (short) y});
                for (var y = 0; y < rowCount; y++)
                    AddWallEast(new CartesianGridCoordinates {x = (short) (colCount-1), y = (short) y});
            }

            public void Dispose()
            {
                if (Walls != null)
                    UnsafeUtility.Free(Walls, Allocator.Temp);
            }

            public void SetWallBit(int x, int y, CartesianGridDirectionBit directionBit)
            {
                if (new CartesianGridCoordinates { x=(short)x, y=(short)y}.OnGrid(RowCount,ColCount))
                    Walls[y * WallRowStride + (x >> 1)] |= (byte)((byte)directionBit << (4 * (x & 1)));
            }
            
            public bool TestWallBit(int x, int y, CartesianGridDirectionBit directionBit)
            {
                if (!(new CartesianGridCoordinates {x = (short) x, y = (short) y}.OnGrid(RowCount, ColCount)))
                    return false;
                
                return (((Walls[y * WallRowStride + (x >> 1)] >> (4 * (x&1))) & (byte)directionBit) == (byte)directionBit);
            }
                    
            public void AddWallNorth(CartesianGridCoordinates cellPosition)
            {
                SetWallBit(cellPosition.x, cellPosition.y, CartesianGridDirectionBit.North);
                SetWallBit(cellPosition.x, cellPosition.y+1, CartesianGridDirectionBit.South);
            }
            public void AddWallSouth(CartesianGridCoordinates cellPosition)
            {
                SetWallBit(cellPosition.x, cellPosition.y, CartesianGridDirectionBit.South);
                SetWallBit(cellPosition.x, cellPosition.y-1, CartesianGridDirectionBit.North);
            }
            public void AddWallWest(CartesianGridCoordinates cellPosition)
            {
                SetWallBit(cellPosition.x, cellPosition.y, CartesianGridDirectionBit.West);
                SetWallBit(cellPosition.x-1, cellPosition.y, CartesianGridDirectionBit.East);
            }
            public void AddWallEast(CartesianGridCoordinates cellPosition)
            {
                SetWallBit(cellPosition.x, cellPosition.y, CartesianGridDirectionBit.East);
                SetWallBit(cellPosition.x+1, cellPosition.y, CartesianGridDirectionBit.West);
            }

            int WalkPath(CartesianGridCoordinates startPosition, NativeArray<byte> targetDirections, int pathOffset)
            {
                var validDirections = CartesianGridOnPlaneShortestPath.LookupDirectionToTarget(startPosition, RowCount, ColCount, targetDirections);
                var direction = CartesianGridMovement.PathVariation[((pathOffset&3) * 16) + validDirections];
                
                if (direction == 0xff) // No path
                    return 0;

                var nextPosition = new CartesianGridCoordinates();
                if (direction == 0)
                    nextPosition = new CartesianGridCoordinates {x = startPosition.x, y = (short) (startPosition.y + 1)};
                else if (direction == 1)
                    nextPosition = new CartesianGridCoordinates {x = startPosition.x, y = (short) (startPosition.y - 1)};
                else if (direction == 2)
                    nextPosition = new CartesianGridCoordinates {x = (short)(startPosition.x-1), y = (short) startPosition.y};
                else if (direction == 3)
                    nextPosition = new CartesianGridCoordinates {x = (short)(startPosition.x+1), y = (short) startPosition.y};
                else
                    Assert.Fail();
                
                // Test no wall in the direction given
                if (direction == 0)
                    Assert.IsFalse(TestWallBit(startPosition.x, startPosition.y, CartesianGridDirectionBit.North));
                else if (direction == 1)
                    Assert.IsFalse(TestWallBit(startPosition.x, startPosition.y, CartesianGridDirectionBit.South));
                else if (direction == 2)
                    Assert.IsFalse(TestWallBit(startPosition.x, startPosition.y, CartesianGridDirectionBit.West));
                else if (direction == 3)
                    Assert.IsFalse(TestWallBit(startPosition.x, startPosition.y, CartesianGridDirectionBit.East));

                return 1 + WalkPath(nextPosition, targetDirections, pathOffset);
            }

            public int WalkPathDistance(CartesianGridCoordinates sourcePosition, CartesianGridCoordinates targetPosition)
            {
                var directionsRowStride = (ColCount + 1) / 2;
                var directionsSize = RowCount * directionsRowStride;
                
                var targetDirections = new NativeArray<byte>(directionsSize, Allocator.Temp);
                var sourceDirections= new NativeArray<byte>(directionsSize, Allocator.Temp);
                
                // For testing purposes, recalculate paths every time.
                CartesianGridOnPlaneShortestPath.CalculateShortestPathsToTarget(targetDirections, RowCount, ColCount, targetPosition, Walls);
                CartesianGridOnPlaneShortestPath.CalculateShortestPathsToTarget(sourceDirections, RowCount, ColCount, sourcePosition, Walls);
                
                // Test distance form source->target is same as target->source
                var sourceToTargetDistance = WalkPath(sourcePosition, targetDirections, 0);
                var targetToSourceDistance = WalkPath(targetPosition, sourceDirections, 0);
                Assert.AreEqual(sourceToTargetDistance, targetToSourceDistance);

                var expectedDistance = sourceToTargetDistance;
                    
                // Sample path variations (not exhaustive, always follow the variation path option)
                for (int i = 1; i < 4; i++)
                {
                    // Test distance form source->target is same as target->source
                    // Test distance is same for all variations
                    sourceToTargetDistance = WalkPath(sourcePosition, targetDirections, i);
                    Assert.AreEqual(expectedDistance, sourceToTargetDistance);
                    targetToSourceDistance = WalkPath(targetPosition, sourceDirections, i);
                    Assert.AreEqual(expectedDistance, targetToSourceDistance);
                }
                
                targetDirections.Dispose();
                sourceDirections.Dispose();

                return expectedDistance;
            }
        }

        [Test]
        public void PathOnEmptyGrid()
        {
            using (var grid = new TestGrid(16, 16))
            {
                var targetPosition = new CartesianGridCoordinates {x = 15, y = 15};
                var dist = 0;

                dist = grid.WalkPathDistance(new CartesianGridCoordinates {x = 0, y = 0}, targetPosition);
                Assert.AreEqual(dist, 30 );
                dist = grid.WalkPathDistance(new CartesianGridCoordinates {x = 8, y = 7}, targetPosition);
                Assert.AreEqual(dist, 15 );
            }
        }
        
        [Test]
        public void PathOnIsland()
        {
            using (var grid = new TestGrid(16, 16))
            {
                // create island left/right side
                for (int y = 0; y < grid.RowCount; y++)
                    grid.AddWallWest(new CartesianGridCoordinates {x = 8, y = (short)y});
               
                var targetPosition = new CartesianGridCoordinates {x = 15, y = 15};
                var dist = 0;
                
                // right side has open path
                dist = grid.WalkPathDistance(new CartesianGridCoordinates {x = 8, y = 8}, targetPosition);
                Assert.AreEqual(dist, 14);
                dist = grid.WalkPathDistance(new CartesianGridCoordinates {x = 8, y = 0}, targetPosition);
                Assert.AreEqual(dist, 22);
                
                // left side blocked
                dist = grid.WalkPathDistance(new CartesianGridCoordinates {x = 0, y = 8}, targetPosition);
                Assert.AreEqual(dist, 0);
                dist = grid.WalkPathDistance(new CartesianGridCoordinates {x = 0, y = 0}, targetPosition);
                Assert.AreEqual(dist, 0);
            }
        }
        
        [Test]
        public void PathRandomObstacles()
        {
            using (var grid = new TestGrid(32, 15))
            {
                int obstacleCount = 32;
                var rand = new Unity.Mathematics.Random(0xF545AA3F);
                for (int i = 0; i < obstacleCount; i++)
                {
                    var xy = rand.NextInt2(new int2(grid.ColCount, grid.RowCount));
                    if (rand.NextBool())
                        grid.AddWallWest(new CartesianGridCoordinates {x = (short)xy.x, y = (short)xy.y});
                    else
                        grid.AddWallSouth(new CartesianGridCoordinates {x = (short)xy.x, y = (short)xy.y});
                }

                int testCount = 64;
                for (int i = 0; i < testCount; i++)
                {
                    var sourceXY = rand.NextInt2(new int2(grid.ColCount, grid.RowCount));
                    var targetXY = rand.NextInt2(new int2(grid.ColCount, grid.RowCount));
                    var sourcePosition = new CartesianGridCoordinates {x = (short)sourceXY.x, y = (short)sourceXY.y};
                    var targetPosition = new CartesianGridCoordinates {x = (short)targetXY.x, y = (short)targetXY.y};
                    grid.WalkPathDistance(sourcePosition, targetPosition);
                }
            }
        }
    }
}
