# Physics scene

Small sample showing predicted physics.
It comes with a custom bootstrap to modify NetcodeConfig to help enhance physics determinism. For example it will disable tick batching which can cause mispredictions.

The character uses PredictionPhysicsUpdate to add predicted forces, to make sure physics works with no lag.

WASD for movements, T for adding a random force.

