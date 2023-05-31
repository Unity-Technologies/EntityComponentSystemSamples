# Networked level synchronization demo

This sample contains a demonstration for how to manage level synchronization between server and clients when using the netcode package.

## Structure

- The main module is the _NetcodeLevelSync.cs_ file which implements the networked level sync flow. It does no scene load/unloading itself as that is very use case specific.
- The _LevelSync_Bootstrap_ scene contains a subscene based level flow which demonstrates how to make use of the networked sync module.
  - It has a UI which makes use of the LevelManager.cs as entry point.
  - It uses LevelTracker* systems to handle loading/unloading routines.

## Flow

_NetcodeLevelSync_ implements the following flow:
- Server initiates level loading
  - Disable ghost sync (_NetworkStreamInGame_ on all connections)
  - Unload current level and start loading the new level
  - Send RPC command to connected clients telling them to also switch levels
- When client receives the RPC command
  - Disable ghost sync (_NetworkStreamInGame_ on server connection)
  - Unload current level and start loading next one
  - When done loading send RPC to server signalling you are ready
- When server has received ready message from all clients and is himself done loading
  - Enable ghost sync (_NetworkStreamInGame_ on all connections)

_NetcodeLevelSync_ is controlled through _LevelSyncStateComponent_ data.
```c#
public struct LevelSyncStateComponent : IComponentData
{
    public LevelSyncState State;
    public int CurrentLevel;
    public int NextLevel;
}
```

The valid level sync states are
```c#
public enum LevelSyncState
{
    Idle,
    LevelLoadRequest,
    LevelLoadInProgress,
    LevelLoaded
}
```

- Normally the state will be _Idle_.
- When server starts level loading he should place the state in _LevelLoadInProgress_
- When a command to load a new level has arrived the client will see the _LevelLoadRequest_ and needs to react to that by starting the level load routine himself and setting state to _LevelLoadInProgress_.
- When either server or client are done loading they should set the state to _LevelLoaded_
- State will go back to _Idle_ when processing is done.

## Sample code reference 

A good way to see the flow more clearly is to have a look at the test for it, as it goes though all the steps in a simple way in one file. See [SceneAutomationSystems.cs](../../Tests/SceneLoadingTests/SceneAutomationSystems.cs) 
