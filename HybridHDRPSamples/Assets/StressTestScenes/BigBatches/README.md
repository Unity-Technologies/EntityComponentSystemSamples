# BigBatches

This sample contains a few stress test scenes:

**Hybrid:** Stress test scene comparing performance of static, color animating and moving entities

**HybridLODs:** The same, but with LOD hierarchy under each root entity

**GameObjects:** Same scene, but with GameObjects. Each game object with individual animated material (one per object)

**GameObjectsSingleMaterial:** Same, but Game objects with one shared material. Faster, but color animation NOT supported.

## What does it show?

With Hybrid Renderer animating per-entity material overrides is fast. GameObjects need either separate material per object, which limits batching performance, or MaterialPropertyBlock, which is not compatible with SRP Batcher at all, resulting in poor performance.