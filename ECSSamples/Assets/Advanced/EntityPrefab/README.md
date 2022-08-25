## Entity Prefab Sample

This sample shows how to reference entity prefabs so they can be dynamically loaded and instantiated.
In the scene a PrefabSpawnerAuthoring component has an array of references to prefabs. This component is converted to an PrefabSpawner and a PrefabSpawnerBufferElement which holds the configuration of the spawner and an EntityPrefabReference for each prefab.
These references ensure that the converted prefab assets are included when building a player but at runtime they will not take up any memory unless manually loaded.

The sample chooses a random of the referenced prefabs to load by adding a RequestEntityPrefabLoaded component to an entity and waiting for the PrefabLoadResult component to be added.
Once the prefab is loaded the prefab spawner will start spawning the required number of instances.

Load requests are reference counted and the prefab will stay loaded as long as there is a RequestEntityPrefabLoaded component referencing it.
Additionally every instance of the prefab also holds a reference since it needs to prevent its resources from being unloaded.

In the sample the PrefabSpawner will destroy itself once it has finished spawning the required number of instances.
This will also destroy the RequestEntityPrefabLoaded which in turn causes the prefab to unload once all instances have been destroyed.
The sample instances will self destruct after a short amount of time.



