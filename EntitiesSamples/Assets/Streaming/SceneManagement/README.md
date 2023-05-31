# Entities SceneLoading samples

These are 7 samples demonstrating how to use the Entities Streaming API:

*NOTE: In these samples, opening and closing the subscene while in playmode may trigger errors. You should enter playmode with the subscenes closed.*

## BasicSceneLoading sample

This sample shows succinctly how to load a scene.

## SceneLoading sample 

This sample shows how to load/unload scenes.

## SectionLoading sample

This sample shows how to load/unload sections from a scene.

## CrossSectionReferences sample

This sample demonstrates how references across sections work.

## SectionMetadata sample

This sample shows how to add metadata to section entities.

## SubsceneInstancing sample

This sample shows how to instantiate multiple times a scene. It demonstrates the usage of *PostLoadCommandBuffer* and *ProcessAfterLoad* system.

## Complete sample

This sample streams tiles of a map based on the camera position. It shows some basic LOD implementation and it combines all the elements from previous samples.




This sample streams tiles of a map based on the camera position. It shows some basic LOD implementation and it combines most all the elements shown in previous samples.

## Play Mode

When we enter play mode we see a floor and a few "boxes" of different colors:
* **Blue Box**: Represents a high LOD for a box
* **Yellow Box**: Represents a medium LOD for a box
* **Red Box**: Represents a low LOD for a box

We can move the camera around using the keys "WASD". As we move the camera:
- Tiles will load and unload based on their distance to the camera.
- Boxes will change to the right LOD level based as well on their distance to the camera.

These changes can be seen more clearly if we put the *Scene* and the *Game* view one next to the other.

![Complete Sample 1!](./Common/Complete1.gif "Complete Sample 1")

## How does it work

###Authoring
In *CompleteSampleMain.unity* there are 2 GameObjects:
* **CameraController**: *CameraControllerAuthoring* drives the movement of the GameObject camera. The baker will add components to control the camera with the keys WASD and it will tag the entity with *RelevantEntity*.
* **CompleteSampleGenerator**: *GeneratorAuthoring* contains parameters for the map generation as well as a list of "Tiles Templates" (Subscenes) that can be used to fill in that map.

The baker for *GeneratorAuthoring* will create an additional entity for each tile/cell in the map with the information needed to be be spawned (*TileInfo*):
* **Scene Reference**: Reference to a random scene to use for instancing the tile.
* **Tile Position**: Position where the tile should be instanced.
* **Tile Rotation**: Random rotation (0,90,180,270) to be applied to the tile during instancing.
* **Loading Distance**: The tile will be loaded if the distance between the tile and any relevant entity is lower than this value.
* **Unloading Distance**: The tile will be unloaded if the distance between the tile and all relevant entity is larger than this value.

Each subscene used as a tile template has:
* All the boxes with their LODS versions assigned to the right scene sections:
    * Section 1: High LOD
    * Section 2: Medium LOD
    * Section 3: Low LOD
* A floor (Quad) assigned to Section 0

The floor has as well an authoring component *SectionLODAuthoring* that is used to define the LOD distances. *SectionLODMetaDataBakingSystem* stores this information as section metadata during baking. Each section will only contain its LOD range (*SectionLODRange*).

###Scene Streaming
*SceneTileLoadingSystem* is in charged of loading/unloading the tiles (scenes) based on the *Loading Distance* and *Unloading Distance* defined during authoring.

Scenes are loaded with the flag **NewInstance** to create a new instance for the scenes. The flag **DisableAutoLoad** is also used for loading to make sure none of the sections are loaded initially. The scene entity is stored in *SubsceneEntityComponent* to be used later for unloading the scene.

After the calling **SceneSystem.LoadSceneAsync**, a **PostLoadCommandBuffer** needs be added to the scene entity to store the tile position and orientation. But in order to keep *SceneTileLoadingSystem* burstable, the addition of that command buffer has been deferred to the system *AddPostLoadCommandSystem* (not bursted).

The information stored in the **PostLoadCommandBuffer** will be used by the system *PostLoadProcessSystem* to place and orient the streaming entities.

During loading we add these other components to the scene entity:
* **RequiresPostLoadCommandBuffer**: This component keeps the information needed to create the **PostLoadCommandBuffer** later in *AddPostLoadCommandSystem*.
* **LoadSection0**: Because of the loading flag **DisableAutoLoad**, sections need to be loaded manually. This tag indicates that section 0 needs to be loaded first. All the other sections will be loaded dynamically.
* **TileEntityComponent**: This is used to keep a reference to the tile entity. This is used to access the calculated distances.

When the tiles are unloaded, **SceneSystem.UnloadParameters.DestroyMetaEntities** is passed to **SceneSystem.UnloadScene** to unload the scene and destroy the scene and section entities.

There is a basic priority system implemented in the scene streaming to give more priority to the tiles closer to relevant entities.

###Section Instancing
*SectionLODSystem* will load and unload the individual sections based on the LOD distances stored as metadata and their distances to relevant entities.

This system will first look for scene entities with the component *LoadSection0* to make sure Sections 0 are loaded first.

Then it will add to each section entity their tile center if that information is not already there. As explained before, this information couldn't be stored using metadata.

Then the loading and unloading is done by adding/removing the component **RequestSceneLoaded** to the section entity (similarly to the *Section Loading* sample). The only difference is that the unloading of a section waits for another section to be loaded first to make sure there is always a "box" visible.

###Distance Calculation
*SceneTileLoadingSystem* and *SectionLODSystem* needs the distances between every tile and all the relevant entities. This calculation is done once in *TileDistanceSystem*. The result for each tile is stored in the component *DistanceToRelevant*.

## Additional Info

This sample works well with more than one *Relevant Entity*. You can create a new GameObject in *CompleteSampleMain.unity* with the authoring component *RelevantEntityAuthoring*.

In play mode you will see that the area where this new *Relevant Entity* is placed is always loaded and we can move the camera far away to create 2 different loaded sections of the map.

![Complete Sample 2!](./Common/Complete2.gif "Complete Sample 2")

## Further Improvements

The objective of this sample is to show as clear as possible how all the elements from previous samples can work together. While this sample can be used as reference in real world projects, it is worth remarking that it could be improved in several ways: *(non exhaustive list)*

* The LOD system is very basic. Depending on the project a more advance system might be required.
* The priority system implemented is very naive. Some projects might need a better approach.
* Many calculations can be done in parallel, such as the calculation of tile distances. Also the way the distances are shared is not optimal.
