# BigBatches

This sample contains a few stress test scenes:

**Hybrid:** A stress test scene that compares the performance of static, color animating, and moving entities.

**HybridLODs:** The same as **Hybrid**, but with a LOD hierarchy under each root entity.

**GameObjects:** The same scene as **Hybrid** and **HybridLOD**, but with GameObjects instead of entities. Each GameObject has an individual animated material.

**GameObjectsSingleMaterial:** The same as **GameObjects**, but the GameObjects share the same material. This is faster but does not support color animation.

## What does it show?

This sample shows that per-entity material overrides are fast in Hybrid Renderer. GameObjects need either:
- A separate material per-object. This limits batching performance.
- A MaterialPropertyBlock. This is not compatible with the SRP Batcher and results in poor performance.
