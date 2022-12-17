# GridPath

Move straight until you hit a wall. Then change direction,

1. There are only four walls (NSWE). Describe permutations of each wall with 4bits.
   - This is what GridAuthoring creates and is stored in GridWalls.
   - 4bits per grid cell (so two cells in the same row are packed into a byte)
2. Given grid x,y look up walls in that grid section.
3. For any given permutation of walls, the exit (next) direction is known.
  - This is stored Paths in GridPathMovementSystem.
  - It may be possible there are two equally good exit directions.
  - So two path options are stored in Paths and are arbitrarily selected between.
4. It's possible to move around in a loop because of wall layout.
  - While that's fine, just as a bonus we want to break the pattern a little.
  - Under rare conditions, provide alternate exit direction from any grid section.
    - Which may be a grid you'd normally move straight through.
  - This is stored as four extra path options in Paths in GridPathMovementSystem.
