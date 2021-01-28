# TransparencyOrdering

This sample demonstrates transparent entities ordering.

<img src="../../../READMEimages/TransparencyOrdering.PNG" width="600">

## What does it show?

This scene contains a lot of semi-transparent cubes. The transparent object ordering this scene displays is a temporary solution.
Hybrid Renderer introduces a new shared component, HybridBatchPartition, which you can use to force entities into separate batches. Hybrid Renderer automatically attaches this shared component to all entities with a transparent material. You can also add this component to entities yourself. This shared component forces entities it is attached to into single-entity chunks and batches, which the render pipeline then sorts. To opt out of this automatic behavior, use DISABLE_HYBRID_TRANSPARENCY_BATCH_PARTITIONING.