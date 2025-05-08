# Changelog

## [1.5.1] - 2025-05-08

### Host migration (HelloNetcode)

#### Changed
- Added test/debug toggle in Frontend menu to manually fail a host migration when elected as host
- Updated for API changes in Netcode for Entities 1.5.1, added ENABLE_HOST_MIGRATION define to player settings to enable the host migration feature

#### Fixed
- Fixed case where a client joins while a host migration is in progress, but the new host fails to become a host, then the client might be elected as the new host, but it would be in the middle of the join-as-client flow and have no client world or scenes loaded yet. 

## [1.5.0-exp.101] - 2025-03-13

### Host migration (HelloNetcode)

#### Changed
- Migration data upload routine moved to a Task to make it more clear
- Return to main menu when you've left the lobby
- More documentation added to the code

#### Fixed
- Fixed lobby heartbeat coroutine lifecycle when host migration is enabled
- Retry downloading host migration data from service when it fails (exception or 0 bytes returned)
- Fix issue where uploads would fail once or twice at service URL expiration threshold before the service data is refreshed
- Better error handling and timeouts at various places

### NetCube

#### Added
- Added separate simple host migration controller to the NetCube sample for demonstration purposes. The simple controller has all the functionality needed to add host migration to the NetCube sample in isolation and is used when using that scene directly (not via the Frontend menu which has a host migration controller for the whole project).

#### Changed
- Now supports host migration in general, mostly persisting data as server-only ghost data, the cube colors and next positions, and skip spawning new cubes for returning players. 

## [1.5.0-exp.2] - 2025-02-11

### Host migration (HelloNetcode)

#### Changes
- Adapt to public API changes in Netcode for Entities package
- Switch to using transport DTLS as it's now safe (package fixed relay issue)

## [1.5.0-exp.1] 2025-01-30

### Added
- Initial commit with host migration support in NetcodeSamples project

### BootstrapAndFrontend (HelloNetcode)

#### Added
- Add option to enable host migration in the Frontend menu scene so it will be enabled in the sample used in the dropdown selection box.

### Host migration (HelloNetcode)

#### Added
- Add `HelloNetcode/1_Basic/01d_HostMigration` sample containing the host migration functionality

### Asteroids

#### Added
- Add host migration support to Asteroids sample (mostly persisting the ship colors and ensuring we pause some systems during migration)
