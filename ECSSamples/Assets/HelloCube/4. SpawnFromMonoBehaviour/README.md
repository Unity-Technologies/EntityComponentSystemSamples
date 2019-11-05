# HelloCube: SpawnFromMonoBehaviour

This sample demonstrates how to spawn Entities and Components using a Prefab GameObject. The scene spawns a "field" of the pairs of spinning cubes.

## What does it show?

This sample uses the Components and Systems from HelloCube IJobForEach.

Unity.Entities provides a GameObjectConversionUtility to convert a GameObject hierarchy to its Entity representation. With this utility, you can convert a Prefab into an Entity representation and use that representation to spawn new instances whenever needed.

When you instantiate the Entity prefab, the whole prefab representation is cloned as a group, in the same way that instantiating a Prefab based on GameObjects does.

The Spawner_FromMonoBehaviour class converts the Prefab to its Entity representation in the Start() method and then instantiates a field of spinning objects.
