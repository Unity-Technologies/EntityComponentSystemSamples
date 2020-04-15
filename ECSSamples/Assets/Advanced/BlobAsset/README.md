# BlobAsset Conversion Sample

This sample demonstrates how to convert a information from a GameObject containing a Unity Authoring component to a BlobAsset using the `BlobAssetStore` and `BlobAssetComputationContext` types.

## What does it show

The sample contains a SubScene with 512 GameObjects. Each GameObject has a "MeshToBoundingBoxAuthoring" component that defines the information we want to store in a blob asset. (The GameObjects are stored as a nested prefab to save disk space). 

After conversion, the `MeshBBRenderSystem` uses the blob asset to draw a bounding box for each of the 512 converted entities. (The renderer uses the Unity `Debug.Draw()` function, so the bounding box only appears in the Scene view.)

## Sample elements:

* **BlobAsset Scene** — a standard Unity Scene. It contains the Subscene.
* **Subscene** — contains the grid of GameObjects to convert.
* **Prefabs**:
    * **M4** — a line made up of four Game objects.
    * **M16** — a square made up of four M4 lines.
    * **M64** — a cube made up of four M16 squares.
    * **M512** — a cube made up of eight M64 cubes .
* **MeshToBoundingBoxsAuthoring** — defines the mesh and scale from which to compute the bounding boxes. Although each component in the sample starts out  the same, you can assign different values to each component. 
* **MeshBBComponent** —defines the structure of the blob asset used to store the computed bounding boxes and also the IComponentData struct used to assign a BlobAssetReference to an individual entity.
* **MeshBBConversionSystem** — Converts each unique combination of the values of Mesh and Scale encountered in the MeshToBoundingBoxsAuthoring components to a unique blob asset. 
* **MeshBBRenderSystem** — draws the bounding boxes in the Unity Scene view.

## What it does

The `MeshToBoundingBoxAuthoring` authoring component has two properties:

1. **Mesh**: you assign a mesh from which the bounding box is computed
2. **Mesh Scale**: a scale that will be applied to the bounding box

The `MeshBBConversionSystem` converts this component to a BlobAsset that contains the bounding box info of the selected mesh scaled with the given value.

Each GameObject in the Subscene with the Authoring component is then converted to an ECS Entity that has a `MeshBBComponent` component data referencing the appropriate BlobAsset.

The BlobAsset is shared among entities if it has the same properties (Mesh & Mesh Scale).

The MeshBBRenderSystem processes all the `MeshBBComponent` instances and renders their bounding boxes in the editor using the Unity `Debug.Draw()` function . (Note that this sample does not render in the Game view or provide any runtime behavior in the Play mode.)

## Some insights

The `MeshBBConversionSystem` is a ` GameObjectConversionSystem` that does most of the interesting work in this sample. Conversion systems run in the Editor when you make a change to the relevant GameObjects and Components in a Subscene and also when you open or close a Subscene for editing.

`MeshBBConversionSystem` performs its conversion task in three steps:

1. For all changed `MeshToBoundingBoxAuthoring` authoring components, the system determines if a BlobAsset must be computed and pushes the values needed to compute the asset onto the `BlobAssetComputationContext` computation stack. 
2. Schedules a job to compute the blob assets using the values created in step 1.
3. Creates a MeshBBComponent, assigns the corresponding `BlobAssetReference` for the blob asset created in step 2, and adds the component to the entity.

A `GameObjectConversionSystem` is a `ComponentSystem`, so it normally performs its work on the main thread. However, as this sample illustrates, you can use the C# Job System to move some parts of a conversion to worker threads. The `BlobAssetComputationContext` provides the `AddBlobAssetToCompute()` function to help optimize blob asset creation. 

Using the `AddBlobAssetToCompute()` function, you can add a *settings* struct containing the values you need in order to create the blob asset to the *context*, associating a set of values with a hash. The hash value is used to determine whether a given authoring component represents a new blob asset or if it should share one already added for a different authoring component. You also use the hash to retrieve a reference to the blob asset after it is created.

