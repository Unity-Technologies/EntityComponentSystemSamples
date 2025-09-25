# HelloNetcode Optimization sample

This sample showcases a ghosts optimization mode (`Static` vs `Dynamic`).

* [Optimization Mode's GhostAuthoringInspector](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/ghost-snapshots.html)
* [Further reading about this optimization](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/optimizations.html)

## Requirements

* GoInGame
* Importance

## Sample description

This sample spawns a number of **Barrel.prefab** ghosts in the scene when entering play mode. 
By using this densely populated scene, we demonstrate that by enabling `Static Optimization Mode` we can achieve reduced 
CPU time during serialization, and smaller package sizes sent over the network.

The number of barrels spawned can be changed by opening the subscene and changing the parameters on the `BarrelSetup`'s inspector:
- **Amount Of Circles**: can be used to add an extra circle of barrels when entering play mode.
- **Spacing**: changes the distance between the barrels for better visibility.
- **Enable Problem**: Ensures the `StaticProblemSystem` is enabled when entering play mode. This is described later in this guide.

The barrels are spawned by the server, and do not contain any physics. 
The barrels will not be moved by any system, and therefore, it does not make sense to continuously send snapshot updates for them.
This is the use-case that Static-Optimization is designed for.

Right-click on the **Barrel** prefab reference in the inspector and select **Properties**. 
Scroll down in the inspector window to the **Ghost Authoring Component**. 
You will see that the **Optimization Mode** is set to **Dynamic**.
Enter play mode and see the barrels spawned in the scene.

> [!NOTE]
> You can ignore the `BarrelWithoutImportance` field for now. 
> It's used in the [09_Importance](../09_Importance/Importance.md) sample.

## Package size

Open the network debugging tool using the menu item `Multiplayer > Open NetDbg`. 
This opens your browser, where you can connect to the running gameplay session.
> [!NOTE]
> We see how the barrels influence the package size on client and server.

Exit play mode and change the **Dynamic Optimization Mode** to **Static**. 
Now enter play mode and reconnect the network debugging tool.
The optimization determines that the delta is zero for the barrels transform, 
and thus does not continue to synchronize the `GhostField`'s non-changing values over the network.

## CPU Time

One thing to keep in mind is that if any system or job has the possibility to modify the synchronized components on an entity, the netcode synchronization will have to serialize the component to compute the delta.
If there are no changes we discard the result and waste the CPU time spent serializing the components.

When looking at CPU time we are using the Unity Profiler. In order for the profiler to show the timings for the job threads we must enable the menu item `Jobs > Use Job Threads`. Also remember to select `Timeline` view.

Exit playmode and check the **Enable Static Optimization Problem** on the `BarrelSetup`'s inspector. This will keep the `StaticProblemSystem` running when we enter playmode.
All the job is doing is taking a reference to all `Translation` components in the scene. It is not doing anything in the job and therefore the barrel's transform remains unchanged.
Open the Unity Profiler and enter playmode. Let the simulation run for a few second and press pause. (Pause by pressing anywhere on the graph in the profiler)

Looking for the `ServerSimulationSystemGroup` here click on `GhostSendSystem` which will highlight the jobs spawned by this system in the job list below.
Search around in the list of jobs and you will find `GhostSendSystem:SerializeJob`. Expand the Worker and notice the InterpolatedBarrel.
It is easy to miss when using Static Optimization that a system with the possibility of writing to a component, regardless of whether it writes to it or not will always have to be serialized.
By rewriting the system to exclude the barrels, or disabling the system entirely we optimize both the package size and CPU time.

## Note

The less optimization we enable the easier it is to spot the performance differences. Disabling burst will increase the timings for the samples significantly and make it easier to show the scaling that the optimization on its own can impact.

Depending on your hardware the timings may vary. You can play around with the spawning parameters to increase/decrease the workload. 
