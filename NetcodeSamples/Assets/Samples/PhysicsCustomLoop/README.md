# Custom predicted-physics build loop

Replaces Netcode's stock physics-world build during client prediction. Prediction re-simulates many ticks per frame, and rebuilding the physics world from scratch each tick is wasteful when the body set hasn't changed. This sample builds the world once per prediction batch, then cheaply updates it for the re-simulated ticks in between. Treat it as a sample to adapt, not a drop-in feature.

## Enabling

Add a `PhysicsLoopConfig` singleton to the client world (author it with `PredictedPhysicsStepConfig`, or create it at runtime). Without it the custom systems stay inert and stock physics runs unchanged. `UseImmediateMode` / `StepImmediateMode` build/step on the main thread instead of scheduling jobs (faster for small worlds).

## Flow

- `EnableDisablePhysicsSystems` (first predicted tick): enable `CustomBuildPhysicsWorld`, disable stock `BuildPhysicsWorld`; in immediate-step mode also disable the stock step groups, since `ImmediatePhysicsStep` does that work.
- `ConfigureBuildPhysicsWorld` (each step): full build on the first and final-full predicted tick, cheap update in between.
- `CustomBuildPhysicsWorld` (each step): full build via `SchedulePhysicsWorldBuild`; cheap update via `ScheduleUpdateMotionData` + `ScheduleUpdateBroadphase` (refits the broadphase, no rebuild). A change in body/joint count forces a full build.
- `DisableCustomPhysicsSystems` (after prediction): restore the stock systems so any client-only physics steps normally.

## Limitations

Incremental broadphase is unsupported: the update-only path doesn't maintain `PhysicsTemporalCoherenceInfo`, so the sample throws (from `EnableDisablePhysicsSystems`, when the loop engages) if `PhysicsStep.IncrementalDynamicBroadphase` or `IncrementalStaticBroadphase` is set. Disable both.

## Reference

`PhysicsCustomLoopFlowTests.CustomBuildPhysicsWorld_WithPhysicsLoopConfig_…` (`Assets/Tests/PhysicsCustomLoopTests`) walks the whole flow with the Jobs Debugger forced on, so a dropped physics-world dependency fails loudly.
