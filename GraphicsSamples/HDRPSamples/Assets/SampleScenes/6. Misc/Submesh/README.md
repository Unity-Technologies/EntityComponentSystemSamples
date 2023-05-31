# Submesh

This sample demonstrates using a Mesh with multiple sub-meshes with Entities Graphics.

<img src="../../../../READMEimages/Submesh.PNG" width="600">

## What does it show?

The scene contains a single GameObject which has a Mesh with three sub-meshes, and three separate Materials, one
for each sub-mesh. As Entities support only a single Material per Entity, these kinds of GameObjects will be baked
into several separate Entities, one per Material.

## How to use this sample scene?

1. In the Hierarchy, make sure the Subscene is closed
2. Go to: **Window > Entities > Hierarchy**
3. Navigate to the Subscene
4. Expand the Entity that has a hierarchy under it
5. Observe that there are three Entities, all of which were baked from the same authoring GameObject