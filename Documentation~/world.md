# World

A `World` owns both an [EntityManager](entity_manager.md) and a set of [ComponentSystems](component_system.md). You can create as many `World` objects as you like. Commonly you would create a simulation `World` and rendering or presentation `World`.

By default we create a single `World` when entering __Play Mode__ and populate it with all available `ComponentSystem` objects in the project, but you can disable the default `World` creation and replace it with your own code via a global define.

- **Default World creation code** (see file: _Packages/com.unity.entities/Unity.Entities.Hybrid/Injection/DefaultWorldInitialization.cs_)
- **Automatic bootstrap entry point** (see file:  _Packages/com.unity.entities/Unity.Entities.Hybrid/Injection/AutomaticWorldBootstrap.cs_) 

> **Note**: We are currently working on multiplayer demos, that will show how to work in a setup with separate simulation & presentation `World` objects. This is a work in progress, so right now we have no clear guidelines and are likely missing features in ECS to enable it. 

[Back to Unity Data-Oriented reference](reference.md)