# HelloSpawnACube

This is the third sample in the HelloECS sample series. Here we introduce entities and how to spawn them. Previously, we had been using GameObjects with GameObjectEntity components. These are not "pure" ECS entities, but "hybrids" of GameObjects and Entities. We use these where we do not yet have support for "pure" entities, components, and systems.


# What does it do?

This sample spawns a single cube when you start the game.


# What does it show?

This sample shows how to spawn a "pure" entity for our cube using a "hybrid" spawner GameObject/Entity which specifies a prefab to use as a template.


# More about "hybrid" GameObjects and "pure" Entities

A GameObject with a GameObjectEntity component on it is both a GameObject and an Entity. This lets us use entities with existing GameObject-only functionality. For example, anything you manipulate in the editor must be a GameObject. 

In play mode, it is possible to use "pure" Entities (i.e. entities that do not have an associated GameObject), but it is only possible to modify them from code at runtime as part of your game simulation. You can see "pure" entities in the Inspector while in play mode by selecting them from the Entity Debugger. This is available under Window > Analysis > Entity Debugger.