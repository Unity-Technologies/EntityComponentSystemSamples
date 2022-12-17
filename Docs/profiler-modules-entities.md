# Entities Profiler modules

The Entities package adds two modules to the [Profiler window]((https://docs.unity3d.com/Manual/Profiler.html)):

* **Entities Structural Changes:** Displays stats about [structural change operations](), which includes creating entities, destroying entities, adding components to entities, and removing components from entities.
* **Entities Memory**: Displays the memory usage of each [archetype]() in your project.

To open the Profiler window, go to **Window &gt; Analysis &gt; Profiler**. To enable or disable modules, use the **Profiler Modules** dropdown in the top-left. 

>[!IMPORTANT]
>The Profiler doesn’t collect any data for modules that aren’t enabled during the profile capture.

To profile baking, capture profiles in Edit Mode to profile the editor itself.

![](images/profiler-entities-structural.png)<br/>_Profiler window with the Entities Profiler modules displayed_


## The Entities Structural Changes profiler module

![](images/profiler-entities-structural.png )<br/>_Profiler window with the Entities Structural Changes module displayed_

The Entities Structural Changes Profiler module records stats for each frame of four kinds of [structural change operations]():

* **Create Entity**
* **Destroy Entity**
* **Add Component**
* **Remove Component**

For each of these four operations, the bottom pane displays a hierarchical view of the worlds, system groups, and systems that performed the structural change, along with the costs (cumulative running time in milliseconds) and counts of the operation.

## The Entities Memory profiler module

![](images/profiler-entities-memory.png)<br/>_Profiler window with the Entities Memory module displayed_

The Entities Memory profiler module records the memory usage of each [archetype](concepts-archetypes.md) for each frame.

- **Allocated Memory:** the amount allocated for the whole archetype.
- **Unused Memory:** the amount of the archetype's allocated memory which is unused.

The bottom pane is just like the [Archetypes window](editor-archetypes-window.md), but this pane displays the information recorded for the selected frame.

## Additional resources

* [Profiling your application](https://docs.unity3d.com/Manual/profiler-profiling-applications.html)
* [Memory in Unity](https://docs.unity3d.com/Manual/performance-memory-overview.html)
