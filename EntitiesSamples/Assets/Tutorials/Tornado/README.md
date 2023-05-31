# Tornado sample

In this Tornado simulation, a tornado travels in a figure-8 pattern and tears apart buildings made of bars connected at their end points. The tornado is depicted as a swarm of swirling cubes. The bars and cubes collide with the ground but not with each other.

## Code

- `BuildingSpawnSystem`: spawns the points and bars that make up the
- `BuildingSystem`: simulates the points and bars. (Updated in the `FixedStepSimulationSystemGroup`.)
- `BuildingRenderSystem`: updates the bar transforms for rendering.
 buildings.
- `CameraSystem`: moves the camera to follow the tornado.
- `ConfigAuthoring`: contains all sim parameters plus prefabs for the bars and particles.
- `TornadoSpawnSystem`: spawns the cube particles that make up the tornado.
- `TornadoSystem`: simulates the cube particles. (Updated in the `FixedStepSimulationSystemGroup`.)

## Notes

- The bars are stored as unmanaged lists and structs.
- The key logic is implemented as Burst-compiled jobs.
- The bars are rendered as entities.
- No physics engine is used.
- The simulation runs at a fixed rate of 50 ticks a second. (This explains the oscillating frame times measured in the profiler when running at higher framerates.)
- The stability of a constraint simulation depends a lot on the order in which constraints are solved. A fully parallel solution that maintains consistent order may be possible, but for simplicity, we avoided full parallelism.
- For the duration of the simulation, the number of bars remains constant, but the number of end points increases as the bars get disconnected: two connected bars share the same point, so an additional point is needed when they separate.
- The scene initially only contains the ground plane, a light, the camera, and a config GameObject. All the bars and cubes are instantiated in the `TornadoSpawnSystem` and `BuildingSpawnSystem` at runtime.
- Each point is updated independently, so they can be updated in parallel in an `IJobParallelFor`.
- The bar simulation solves the distance constraints by moving the points and eventually breaking the connections. When a connection breaks, the shared point gets split into two separate points. 
- Each bar's rendering transform matrix is computed from its two points.
- The tornado's position and intensity is computed each frame from the elapsed time. 
- When a bar is disconnected, its two points are not shared with other bars, and it occupies 250 bytes: 200 bytes for the bar itself and 25 bytes each for two points. If we assume a maximum memory bandwidth of 60 GB/s (representative of a modern gaming PC), the theoretical absolute maximum scale of this simulation is 4 billion bars at 60hz. In practice, we'll be lucky to achieve a tenth of that scale, but knowing the theoretical limit helps set our expectations.
- A point's number of connected bars and whether the point is anchored is combined into one value (each byte stored in the 'connectivity' array). This makes it cheaper to check whether a point is connected to anything.
- In experimentation, the building generation code never seems to create buildings with more than about 50 bars. As long as this is true, it's safe to store the number of connected bars in a single byte.

## Entities vs. arrays

The advantage of an entity is flexibility: entities can be independently created and destroyed, their components can be added and removed, and a reference to an entity will remain valid for the entity's lifetime. All entities sharing a subset of component types can be processed together in a single query. On the downside, a reference to an entity is composed of two ints (index and version numbers), and looking up an entity _via_ reference is costlier than if we could just follow a pointer.

The advantage of using arrays instead of entities is that items can be looked up directly by their index. Not only is the lookup cheaper, a reference to an item can be stored as just a single int. On the downside, elements in an array cannot be independently added and removed without also shifting the other elements within the array, thereby changing their indexes.

In this sample, we use entities to render the bars and cubes, but what about the points? The points never change in structure and are never reordered nor deleted, only created (when bars disconnect), so the advantages of entities don't apply here. Storing the points in an array also allows for cheaper lookups, of which we'll need to do many per frame.

Furthermore, the simulation has a large number of bars, each of which must reference its two points. Were the points stored as entities, each bar would require 16 bytes for these references (2 ints per point). By storing the points in an array, each bar requires only 8 bytes for these references (1 int per point). Smaller data is always a good thing, as it leas to less memory access.



