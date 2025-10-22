# Changelog

## [1.9.1] - 2025-10-11

### Fixed
- Issue in host migration sample where the migration update data didn't update/compress properly when hosting a game, leaving and then re-hosting.
- Issue in connection approval sample when going through relay.

## [1.9.0] - 2025-09-13

### Added
- Added debug level logging in hello netcode samples for better following input handling and spawning.

### Fixed
- Issue in `Asteroids` where the `AsteroidScore` ghost was being stripped out of builds.

### Host Migration (HelloNetcode)

#### Changes
- In `Asteroids` removed the server-only player color tracking system. Now the colors are again mapped to the network ID like before and these are properly migrated. One side effect is that when the client which was the previous host reconnects he'll not be detected as a reconnection anymore if the client world has been destroyed (like when returning to main menu).

#### Fixed
- Issue where reconnected player were not detected properly and got duplicate spawns in the `PlayerSpawn` sample and those who derive from it.
- Issue with double amount of barrels spawning in `Importance` sample after a migration.
- Possible crash when waiting for the relay join code to arrive (server world can disappear).
- Issue with UTC time being used in lobby migration data but not in local time

## [1.8.0] - 2025-08-17

### Changed
- Improvements to importance system in `Asteroids`.
- Improvements to the `Optimization` sample.
- Big overhaul done on the `Importance` sample, use with the new `Importance Visualizer` in the Netcode for Entities package.

### Fixed
- In `Asteroids` properly apply the bullet scale from the prefab.


## [1.7.0] - 2025-07-29

### Fixed
- Issue in the frontend where the selected sample would not be saved between sessions (always picked the first one in the Samples or HelloNetcode lists)

### Relay Support (HelloNetcode)

#### Changed
- When a join code has been entered the hosting button is disabled

#### Fixed
- Message is now printed to the UI field when no join code is present when pressing join button
- When join button is pressed with an empty field, it no longer tries to immediately join when you start entering the join code (now properly resets state)
- Issue where you could not host or join a second time after going back to the frontend menu
- Failure in `ConnectionApproval` sample when running via relay


## [1.6.2] - 2025-07-07

### Added
- Preserialization test with a custom template added to the project.


## [1.6.1] - 2025-05-28

### Fixed
- Issues with the relay sample and the WebGL platform target not working properly together.

### Asteroids

### Changed
- Improved relevancy and importance scaling implementation, increase asteroid count from 200 to 800
- Improved the `AsteroidScore` and collision handling.

### Host migration (HelloNetcode)

#### Changed
- Added test/debug toggle to manually fail a host migration when elected as host
- Updated for API changes in Netcode for Entities 1.6.1, added `ENABLE_HOST_MIGRATION` define to player settings to enable the host migration feature

#### Fixed
- Fixed case where a client joins while a host migration is in progress, but the new host fails to become a host, then the client might be elected as the new host, but it would be in the middle of the join-as-client flow and have no client world or scenes loaded yet. 


## [1.5.0] - 2025-04-22

### Added

### Changed
- Modified `LoadScenes_AllScenesShouldConnect` test to spam the reconnect button, to place more burden on the correctness of Netcode for Entities (and each samples) reconnect flows.
- Cleaned up some constantly changing meta files.
- Removed the duplicate Asteroids scene.
- Fixed issue in the `ConnectionApproval` sample, where disconnecting would not stop the sending of the RPC, leading to an RPC warning when reconnecting. Ditto for `PlayerList` and RPC samples.
- Updated `SceneLoading` tests to use `AutomaticThinClientWorldsUtility`.
- Stop including `DisableBootstrap` scene in the sample selection dropdown.
- Switch to using Multiplayer SDK instead of relay package.
- `GhostPredictionSwitchingSystemForThinClient` is no longer needed (netcode does not require it).

### Fixed
- Issues with `PredictedSpawning` classification system.
- Issues with particles in the `MultyPhysicsWorld` sample
