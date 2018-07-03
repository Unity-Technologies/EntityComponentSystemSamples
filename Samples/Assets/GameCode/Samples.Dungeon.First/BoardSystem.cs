using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

// Sample based on: https://unity3d.com/learn/tutorials/topics/scripting/basic-2d-dungeon-generation
// This version is the starting point. It's a direct port to ECS with minimal changes.

namespace Samples.Dungeon.First
{
    // Enum to specify the direction is heading.
    public enum Direction
    {
        North, East, South, West
    }

    public struct Corridor
    {
        public int startXPos;         // The x coordinate for the start of the corridor.
        public int startYPos;         // The y coordinate for the start of the corridor.
        public int corridorLength;    // How many units long the corridor is.
        public Direction direction;   // Which direction the corridor is heading from it's room.

        // Get the end position of the corridor based on it's start position and which direction it's heading.
        public int EndPositionX
        {
            get
            {
                if (direction == Direction.North || direction == Direction.South)
                    return startXPos;
                if (direction == Direction.East)
                    return startXPos + corridorLength - 1;
                return startXPos - corridorLength + 1;
            }
        }


        public int EndPositionY
        {
            get
            {
                if (direction == Direction.East || direction == Direction.West)
                    return startYPos;
                if (direction == Direction.North)
                    return startYPos + corridorLength - 1;
                return startYPos - corridorLength + 1;
            }
        }

        public static Corridor Setup(Room room, Board board, bool firstCorridor)
        {
            // Set a random direction (a random index from 0 to 3, cast to Direction).
            var direction = (Direction)Random.Range(0, 4);
        
            // Find the direction opposite to the one entering the room this corridor is leaving from.
            // Cast the previous corridor's direction to an int between 0 and 3 and add 2 (a number between 2 and 5).
            // Find the remainder when dividing by 4 (if 2 then 2, if 3 then 3, if 4 then 0, if 5 then 1).
            
            // Overall effect is if the direction was South then that is 2, becomes 4, remainder is 0, which is north.
            Direction oppositeDirection = (Direction)(((int)room.enteringCorridor + 2) % 4);

            // If this is noth the first corridor and the randomly selected direction is opposite to the previous corridor's direction...
            if (!firstCorridor && direction == oppositeDirection)
            {
                // Rotate the direction 90 degrees clockwise (North becomes East, East becomes South, etc).
                // This is a more broken down version of the opposite direction operation above but instead of adding 2 we're adding 1.
                // This means instead of rotating 180 (the opposite direction) we're rotating 90.
                int directionInt = (int)direction;
                directionInt++;
                directionInt = directionInt % 4;
                direction = (Direction)directionInt;
            
            }

            // Set a random length.
            var corridorLength = board.CorridorLength.Random;
            var startXPos = 0;
            var startYPos = 0;

            // Create a cap for how long the length can be (this will be changed based on the direction and position).
            int maxLength = board.CorridorLength.m_Max;

            switch (direction)
            {
                // If the choosen direction is North (up)...
                case Direction.North:
                    // ... the starting position in the x axis can be random but within the width of the room.
                    startXPos = Random.Range (room.xPos, room.xPos + room.roomWidth - 1);

                    // The starting position in the y axis must be the top of the room.
                    startYPos = room.yPos + room.roomHeight;

                    // The maximum length the corridor can be is the height of the board (rows) but from the top of the room (y pos + height).
                    maxLength = board.GridCount.y - startYPos - board.RoomHeight.m_Min;
                    break;
                case Direction.East:
                    startXPos = room.xPos + room.roomWidth;
                    startYPos = Random.Range(room.yPos, room.yPos + room.roomHeight - 1);
                    maxLength = board.GridCount.x - startXPos - board.RoomWidth.m_Min;
                    break;
                case Direction.South:
                    startXPos = Random.Range (room.xPos, room.xPos + room.roomWidth);
                    startYPos = room.yPos;
                    maxLength = startYPos - board.RoomHeight.m_Min;
                    break;
                case Direction.West:
                    startXPos = room.xPos;
                    startYPos = Random.Range (room.yPos, room.yPos + room.roomHeight);
                    maxLength = startXPos - board.RoomWidth.m_Min;
                    break;
            }
        
            // We clamp the length of the corridor to make sure it doesn't go off the board.
            corridorLength = Mathf.Clamp (corridorLength, 1, maxLength);

            return new Corridor
            {
                corridorLength = corridorLength,
                direction = direction,
                startXPos = startXPos,
                startYPos = startYPos
            };
        }
    }
    
    public struct Room
    {
        public int xPos;                      // The x coordinate of the lower left tile of the room.
        public int yPos;                      // The y coordinate of the lower left tile of the room.
        public int roomWidth;                 // How many tiles wide the room is.
        public int roomHeight;                // How many tiles high the room is.
        public Direction enteringCorridor;    // The direction of the corridor that is entering this room.

        // This is used for the first room.  It does not have a Corridor parameter since there are no corridors yet.
        public static Room Setup(Board board)
        {
            // Set a random width and height.
            var roomWidth = board.RoomWidth.Random;
            var roomHeight = board.RoomHeight.Random;
            
            // Set the x and y coordinates so the room is roughly in the middle of the board.
            var xPos = Mathf.RoundToInt(board.GridCount.x / 2f - roomWidth / 2f);
            var yPos = Mathf.RoundToInt(board.GridCount.y / 2f - roomHeight / 2f);

            return new Room
            {
                xPos = xPos,
                yPos = yPos,
                roomHeight = roomHeight,
                roomWidth = roomWidth,
                enteringCorridor = Direction.North
            };
        }

        // This is an overload of the Setup function and has a corridor parameter that represents the corridor entering the room.
        public static Room Setup(Board board, Corridor corridor)
        {
            // Set the entering corridor direction.
            var enteringCorridor = corridor.direction;

            // Set random values for width and height.
            var roomWidth = board.RoomWidth.Random;
            var roomHeight = board.RoomHeight.Random;

            var xPos = 0;
            var yPos = 0;

            switch (corridor.direction)
            {
                // If the corridor entering this room is going north...
                case Direction.North:
                    // ... the height of the room mustn't go beyond the board so it must be clamped based
                    // on the height of the board (rows) and the end of corridor that leads to the room.
                    roomHeight = Mathf.Clamp(roomHeight, 1, board.GridCount.y - corridor.EndPositionY);

                    // The y coordinate of the room must be at the end of the corridor (since the corridor leads to the bottom of the room).
                    yPos = corridor.EndPositionY;
            
                    // The x coordinate can be random but the left-most possibility is no further than the width
                    // and the right-most possibility is that the end of the corridor is at the position of the room.
                    xPos = Random.Range (corridor.EndPositionX - roomWidth + 1, corridor.EndPositionX);

                    // This must be clamped to ensure that the room doesn't go off the board.
                    xPos = Mathf.Clamp (xPos, 0, board.GridCount.x - roomWidth);
                    break;
                case Direction.East:
                    roomWidth = Mathf.Clamp(roomWidth, 1, board.GridCount.x - corridor.EndPositionX);
                    xPos = corridor.EndPositionX;

                    yPos = Random.Range (corridor.EndPositionY - roomHeight + 1, corridor.EndPositionY);
                    yPos = Mathf.Clamp (yPos, 0, board.GridCount.y - roomHeight);
                    break;
                case Direction.South:
                    roomHeight = Mathf.Clamp (roomHeight, 1, corridor.EndPositionY);
                    yPos = corridor.EndPositionY - roomHeight + 1;

                    xPos = Random.Range (corridor.EndPositionX - roomWidth + 1, corridor.EndPositionX);
                    xPos = Mathf.Clamp (xPos, 0, board.GridCount.x - roomWidth);
                    break;
                case Direction.West:
                    roomWidth = Mathf.Clamp (roomWidth, 1, corridor.EndPositionX);
                    xPos = corridor.EndPositionX - roomWidth + 1;

                    yPos = Random.Range (corridor.EndPositionY - roomHeight + 1, corridor.EndPositionY);
                    yPos = Mathf.Clamp (yPos, 0, board.GridCount.y - roomHeight);
                    break;
            }
            
            return new Room
            {
                xPos = xPos,
                yPos = yPos,
                roomHeight = roomHeight,
                roomWidth = roomWidth,
                enteringCorridor = enteringCorridor
            };
        }
    }
    
    public class BoardSystem : ComponentSystem
    {
        // The type of tile that will be laid in a specific position.
        public enum TileType
        {
            Wall, Floor,
        }
        
        struct BoardGroupData
        {
            public ComponentDataArray<Board> Boards;
            public EntityArray Entities;
            public readonly int Length;
        }
        [Inject] private BoardGroupData BoardGroup;

        private ComponentGroup OuterWallTileGroup;
        private ComponentGroup FloorTileGroup;
        private ComponentGroup WallTileGroup;
        
        protected override void OnUpdate()
        {
            if (BoardGroup.Length == 0)
            {
                return;
            }
            
            // Copy data from ComponentGroups 
            var boards = new NativeArray<Board>(BoardGroup.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            var boardEntities = new NativeArray<Entity>(BoardGroup.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < BoardGroup.Length; i++)
            {
                boards[i] = BoardGroup.Boards[i];
                boardEntities[i] = BoardGroup.Entities[i];
            }
            
            for (int i = 0; i < BoardGroup.Length; i++)
            {
                var board = boards[i];
                var boardEntity = boardEntities[i];
                var roomCount = board.NumRooms.Random;
                if (roomCount < 1)
                {
                    return;
                }
                
                var tiles = new NativeArray<TileType>(board.GridCount.x * board.GridCount.y, Allocator.Temp);
                var rooms = new NativeArray<Room>(roomCount, Allocator.Temp); 
                var corridors = new NativeArray<Corridor>(roomCount - 1, Allocator.Temp); 

                CreateRoomsAndCorridors(board, rooms, corridors);
                SetTilesValuesForRooms(rooms, tiles, board.GridCount.x);
                SetTilesValuesForCorridors(corridors, tiles, board.GridCount.x);

                InstantiateTiles(board, tiles, board.GridCount.x, boardEntity);
                InstantiateOuterWalls(board, boardEntity);
                EntityManager.RemoveComponent<Board>(boardEntity);
                
                
                tiles.Dispose();
                rooms.Dispose();
                corridors.Dispose();
            }
            
            boards.Dispose();
            boardEntities.Dispose();
        }

        protected override void OnCreateManager(int capacity)
        {
            OuterWallTileGroup = GetComponentGroup(
                ComponentType.ReadOnly(typeof(BoardReference)),
                ComponentType.ReadOnly(typeof(OuterWallTile)));
            FloorTileGroup = GetComponentGroup(
                ComponentType.ReadOnly(typeof(BoardReference)),
                ComponentType.ReadOnly(typeof(FloorTile)));
            WallTileGroup = GetComponentGroup(
                ComponentType.ReadOnly(typeof(BoardReference)),
                ComponentType.ReadOnly(typeof(WallTile)));
        }

        void InstantiateOuterWalls(Board board, Entity boardEntity)
        {
            // The outer walls are one unit left, right, up and down from the board.
            float halfWidth   = (board.GridStep.x * board.GridCount.x) * 0.5f;
            float halfHeight  = (board.GridStep.y * board.GridCount.y) * 0.5f;
            float halfStepX   = board.GridStep.x * 0.5f;
            float halfStepY   = board.GridStep.y * 0.5f;
            float leftEdgeX   = (-halfWidth) + halfStepX;
            float rightEdgeX  = halfWidth - halfStepX;
            float bottomEdgeY = (-halfHeight) + halfStepY;
            float topEdgeY    = halfHeight - halfStepY;
            
            // Shift outer wall outward one step
            leftEdgeX   -= board.GridStep.x;
            rightEdgeX  += board.GridStep.x;
            topEdgeY    += board.GridStep.y;
            bottomEdgeY -= board.GridStep.y;

            var boardReference = new BoardReference
            {
                TileSetId = board.TileSetId
            };
            OuterWallTileGroup.SetFilter(boardReference);
            var outerWallTileGroupEntities = OuterWallTileGroup.GetEntityArray();

            if (outerWallTileGroupEntities.Length > 0)
            {
                // copy data from ComponentGroup
                var tileEntities = new NativeArray<Entity>(outerWallTileGroupEntities.Length,Allocator.Temp,NativeArrayOptions.UninitializedMemory);
                for (int i = 0; i < outerWallTileGroupEntities.Length; i++)
                {
                    tileEntities[i] = outerWallTileGroupEntities[i];
                }
            

                // Instantiate both vertical walls (one on each side).
                InstantiateVerticalOuterWall(leftEdgeX, bottomEdgeY, topEdgeY, board.GridStep.y, tileEntities, boardEntity);
                InstantiateVerticalOuterWall(rightEdgeX, bottomEdgeY, topEdgeY, board.GridStep.y, tileEntities, boardEntity);

                // Instantiate both horizontal walls, these are one in left and right from the outer walls.
                InstantiateHorizontalOuterWall(leftEdgeX + board.GridStep.x, rightEdgeX - board.GridStep.x, bottomEdgeY, board.GridStep.x, tileEntities, boardEntity);
                InstantiateHorizontalOuterWall(leftEdgeX + board.GridStep.x, rightEdgeX - board.GridStep.x, topEdgeY, board.GridStep.x, tileEntities, boardEntity);
            
                tileEntities.Dispose();
            }
        }
        
        void InstantiateVerticalOuterWall (float xCoord, float startingY, float endingY, float gridStep, NativeArray<Entity> tileEntities, Entity boardEntity)
        {
            // Start the loop at the starting value for Y.
            float currentY = startingY;

            // While the value for Y is less than the end value...
            while (currentY <= endingY)
            {
                // ... instantiate an outer wall tile at the x coordinate and the current y coordinate.
                InstantiateFromArray(tileEntities, xCoord, currentY, boardEntity);

                currentY += gridStep;
            }
        }
        
        void InstantiateHorizontalOuterWall (float startingX, float endingX, float yCoord, float gridStep, NativeArray<Entity> tileEntities, Entity boardEntity)
        {
            // Start the loop at the starting value for X.
            float currentX = startingX;

            // While the value for X is less than the end value...
            while (currentX <= endingX)
            {
                // ... instantiate an outer wall tile at the y coordinate and the current x coordinate.
                InstantiateFromArray(tileEntities, currentX, yCoord, boardEntity);

                currentX += gridStep;
            }
        }
        
        void CreateRoomsAndCorridors(Board board, NativeArray<Room> rooms, NativeArray<Corridor> corridors)
        {
            // Setup the first room, there is no previous corridor so we do not use one.
            rooms[0] = Room.Setup(board);

            // Setup the first corridor using the first room.
            corridors[0] = Corridor.Setup(rooms[0], board, true);

            for (int i = 1; i < rooms.Length; i++)
            {
                // Create a room.
                rooms[i] = new Room ();
            
                // Setup the room based on the previous corridor.
                rooms[i] = Room.Setup(board, corridors[i - 1]);

                // If we haven't reached the end of the corridors array...
                if (i < corridors.Length)
                {
                    // ... create a corridor.
                    corridors[i] = new Corridor ();

                    // Setup the corridor based on the room that was just created.
                    corridors[i] = Corridor.Setup(rooms[i], board, false);
                }
            }
        }
        
        void SetTilesValuesForRooms(NativeArray<Room> rooms, NativeArray<TileType> tiles, int tilesStride)
        {
            // Go through all the rooms...
            
            for (int i = 0; i < rooms.Length; i++)
            {
                Room currentRoom = rooms[i];
            
                // ... and for each room go through it's width.
                for (int j = 0; j < currentRoom.roomWidth; j++)
                {
                    int xCoord = currentRoom.xPos + j;

                    // For each horizontal tile, go up vertically through the room's height.
                    for (int k = 0; k < currentRoom.roomHeight; k++)
                    {
                        int yCoord = currentRoom.yPos + k;

                        tiles[(yCoord * tilesStride) + xCoord] = TileType.Floor;
                    }
                }
            }
        }
        
        void SetTilesValuesForCorridors(NativeArray<Corridor> corridors, NativeArray<TileType> tiles, int tilesStride)
        {
            // Go through every corridor...
            for (int i = 0; i < corridors.Length; i++)
            {
                var currentCorridor = corridors[i];

                // and go through it's length.
                for (int j = 0; j < currentCorridor.corridorLength; j++)
                {
                    // Start the coordinates at the start of the corridor.
                    int xCoord = currentCorridor.startXPos;
                    int yCoord = currentCorridor.startYPos;

                    // Depending on the direction, add or subtract from the appropriate
                    // coordinate based on how far through the length the loop is.
                    switch (currentCorridor.direction)
                    {
                        case Direction.North:
                            yCoord += j;
                            break;
                        case Direction.East:
                            xCoord += j;
                            break;
                        case Direction.South:
                            yCoord -= j;
                            break;
                        case Direction.West:
                            xCoord -= j;
                            break;
                    }
                    
                    tiles[(yCoord * tilesStride) + xCoord] = TileType.Floor;
                }
            }
        }
        
        void InstantiateTiles(Board board, NativeArray<TileType> tiles, int tilesStride, Entity parent)
        {
            // The outer walls are one unit left, right, up and down from the board.
            float halfWidth   = (board.GridStep.x * board.GridCount.x) * 0.5f;
            float halfHeight  = (board.GridStep.y * board.GridCount.y) * 0.5f;
            float halfStepX   = board.GridStep.x * 0.5f;
            float halfStepY   = board.GridStep.y * 0.5f;
            float leftEdgeX   = (-halfWidth) + halfStepX;
            float topEdgeY    = halfHeight - halfStepY;
            
            var boardReference = new BoardReference
            {
                TileSetId = board.TileSetId
            };
            FloorTileGroup.SetFilter(boardReference);
            var floorTileGroupEntities = FloorTileGroup.GetEntityArray();
            WallTileGroup.SetFilter(boardReference);
            var wallTileGroupEntities = WallTileGroup.GetEntityArray();
            
            // copy data from ComponentGroup
            var floorTileEntities = new NativeArray<Entity>(floorTileGroupEntities.Length,Allocator.Temp,NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < floorTileGroupEntities.Length; i++)
            {
                floorTileEntities[i] = floorTileGroupEntities[i];
            }
            
            var wallTileEntities = new NativeArray<Entity>(wallTileGroupEntities.Length,Allocator.Temp,NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < wallTileGroupEntities.Length; i++)
            {
                wallTileEntities[i] = wallTileGroupEntities[i];
            }
            
            // Go through all the tiles...
            for (int i = 0; i < board.GridCount.y; i++)
            {
                for (int j = 0; j < board.GridCount.x; j++)
                {
                    var x = leftEdgeX + (j * board.GridStep.x);
                    var y = topEdgeY + (-i * board.GridStep.y);
                    
                    if ( tiles[(i* tilesStride) + j] == TileType.Floor )
                    {
                        // ... and instantiate a floor tile for it.
                        if (floorTileEntities.Length > 0)
                        {
                            InstantiateFromArray(floorTileEntities, x, y, parent);
                        }
                    }
                    else
                    {
                        if (wallTileEntities.Length > 0)
                        {
                            InstantiateFromArray(wallTileEntities, x, y, parent);
                        }
                    }
                }
            }
            
            floorTileEntities.Dispose();
            wallTileEntities.Dispose();
        }
        
        void InstantiateFromArray (NativeArray<Entity> prefabs, float xCoord, float yCoord, Entity parent)
        {
            // Create a random index for the array.
            int randomIndex = Random.Range(0, prefabs.Length);

            var entity = EntityManager.Instantiate(prefabs[randomIndex]);
            var position = new LocalPosition
            {
                Value = new float3(xCoord, 0.0f, yCoord)
            };
            EntityManager.SetComponentData(entity, position);

            var transformParent = new TransformParent
            {
                Value = parent
            };
            EntityManager.SetComponentData(entity, transformParent);
        } 
    }
    
    
}
