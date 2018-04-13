# ECS concepts

If you are familiar with [Entity-Component-System](https://en.wikipedia.org/wiki/Entity%E2%80%93component%E2%80%93system) (ECS) concepts, you might notice the potential for naming conflicts with Unity's existing __GameObject__/__Component__ setup. Below is a list comparing how ECS concepts map to Unity's implementation:

### Entity → Entity

Unity did not have an __Entity__ to begin with, so the structure is simply named after the concept. Entities are like super-lightweight GameObjects in that they do little on their own; they don't store any data (not even a name!).

You add Components to Entities similar to how you add Components to GameObjects.

### Component → ComponentData

We are introducing a new high-performance ComponentType. 

```
struct MyComponent: IComponentData
{} 
```

The __EntityManager__ manages the memory and makes hard guarantees about linear memory access when iterating over a set of Components. It also has zero overhead on a per Entity basis beyond the size of the struct itself.

In order to differentiate from existing component types (such as __MonoBehaviours__), ComponentData is named in reference to the fact that it only stores data. __ComponentData__ can be added and removed from Entities.

### System → ComponentSystem

There are a lot of "systems" in Unity, so the name includes the umbrella term, "component" as well. __ComponentSystems__ define your game's behavior, and can operate on either traditional GameObjects and Components or pure ECS ComponentData and Entity structs.
