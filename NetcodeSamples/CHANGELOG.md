# 2022-02-15
## Changes
* Upgraded to Netcode for Entities 1.0.0 prerelease version 2, see the [Netcode for Entities changelog](https://docs.unity3d.com/Packages/com.unity.netcode@1.0/changelog/CHANGELOG.html).
* Two new HelloNetcode samples have been added
  * _RelaySupport_ which shows how to use the Unity Relay service with Netcode for Entities. It's added to the Frontend sample.
  * _Importance_ which demonstrates how importance scaling works with many ghosts.

# 2022-11-29
## Changes
* Upgraded to Netcode for Entities 1.0.0 prerelease version, see the [Netcode for Entities changelog](https://docs.unity3d.com/Packages/com.unity.netcode@1.0/changelog/CHANGELOG.html).
* Added HelloNetcode suite of samples, these samples aim to be small, simple and show features in isolation. Later samples then re-use earlier ones so it's simpler to see exactly what's being shown.
* LagCompensation sample was moved to HelloNetcode/Intermediate/HitScanWeapon
* BootstapAndFrontend was moved to HelloNetcode/Basic/BootstrapAndFrontend and also split there into frontend (client only) and bootstrap (Client+Server) assemblies.

# 2022-10-18
## Changes
* Upgraded to Netcode for Entities 1.0.0-exp.8, see the [Netcode for Entities changelog](https://docs.unity3d.com/Packages/com.unity.netcode@1.0/changelog/CHANGELOG.html).
* Added a player list sample

# 2022-03-18
## Changes
* Upgraded to Netcode for Entities 0.50.0, see the [Netcode for Entities changelog](https://docs.unity3d.com/Packages/com.unity.netcode@0.50/changelog/CHANGELOG.html).
* Added a sample for prediction switching.
* Added a frontend sample, it is used for standalone builds and can launch all other samples in client-only or client/server mode.
* Modified asteroids to use the Hybrid Renderer package.
* Removed the transport samples.

# 2021-01-28
## Changes
* Upraded Unity NetCode to 0.6.0, see the [Unity NetCode changelog](https://docs.unity3d.com/Packages/com.unity.netcode@0.6/changelog/CHANGELOG.html)
* Upgraded Unity Transport to 0.6.0 see the [Unity Transport changelog](https://docs.unity3d.com/Packages/com.unity.transport@0.6/changelog/CHANGELOG.html)
* Brought back the custom 2d serialization rules for translation and rotation in asteroids.

# 2020-09-17
## Changes
* Upraded Unity NetCode to 0.4.0, see the [Unity NetCode changelog](https://docs.unity3d.com/Packages/com.unity.netcode@0.4/changelog/CHANGELOG.html)
* Upgraded Unity Transport to 0.4.1 see the [Unity Transport changelog](https://docs.unity3d.com/Packages/com.unity.transport@0.4/changelog/CHANGELOG.html)
* Upgraded all NetCode samples to use subscenes instead of ConvertToEntity. This means you now have to use the files in BuildSettings to make a standalone build.
* Upgraded all samples to use SystemBase and most of them to use Entities.ForEach.

# 2020-08-21
## Changes
* Upraded Unity NetCode to 0.3.0, see the [Unity NetCode changelog](https://docs.unity3d.com/Packages/com.unity.netcode@0.3/changelog/CHANGELOG.html)
* Upgraded Unity Transport to 0.4.0 see the [Unity Transport changelog](https://docs.unity3d.com/Packages/com.unity.transport@0.4/changelog/CHANGELOG.html)
* Added a new sample showing how to write custom network interfaces in the transport.
* Added support for switching between fixed and dynamic timestep in asteroids.
* Added a relevancy implementation to asteroids which can be used to only show ghosts within a specific radius of the player ship.
* Added a mode to asteroids which uses the new static optimization mode along with moving some calculations to the client for a massive bandwidth optimization.
* Rewrote predictive spawning of projectiles and "on spawn" handling in asteroids to be compatible with the new NetCode package.

# 2020-06-09
## Changes
* Upgraded Unity NetCode to 0.2.0, see the [Unity NetCode changelog](https://docs.unity3d.com/Packages/com.unity.netcode@0.2/changelog/CHANGELOG.html)
* Upgraded Unity Transport to 0.3.1 see the [Unity Transport changelog](https://docs.unity3d.com/Packages/com.unity.transport@0.3/changelog/CHANGELOG.html)

# 2020-02-24
## Changes
* Upgraded Unity NetCode to 0.1.0, see the [Unity NetCode changelog](https://docs.unity3d.com/Packages/com.unity.netcode@0.1/changelog/CHANGELOG.html)
* Upgraded Unity Transport to 0.3.0, see the [Unity Transport changelog](https://docs.unity3d.com/Packages/com.unity.transport@0.3/changelog/CHANGELOG.html)
* Added new sample demonstrating lag compensation.

# 2019-12-17
## Changes
* Upgraded Unity NetCode to 0.0.4, see the [Unity NetCode changelog](https://docs.unity3d.com/Packages/com.unity.netcode@0.0/changelog/CHANGELOG.html)
* Upgraded Unity Transport to 0.2.3, see the [Unity Transport changelog](https://docs.unity3d.com/Packages/com.unity.transport@0.2/changelog/CHANGELOG.html)
* Reduced maximum number of lines rendered in asteroids and added simple culling.

# 2019-12-06
## Changes
* Upgraded Unity NetCode to 0.0.3, see the [Unity NetCode changelog](https://docs.unity3d.com/Packages/com.unity.netcode@0.0/changelog/CHANGELOG.html)
* Upgraded Unity Transport to 0.2.2, see the [Unity Transport changelog](https://docs.unity3d.com/Packages/com.unity.transport@0.2/changelog/CHANGELOG.html)
* Upgraded Entities to 0.3.0

# 2019-11-29
## New features
* Unity Transport is a package, see the [Unity Transport changelog](https://docs.unity3d.com/Packages/com.unity.transport@0.2/changelog/CHANGELOG.html) for details.
* Unity NetCode is a package, see the [Unity NetCode changelog](https://docs.unity3d.com/Packages/com.unity.netcode@0.0/changelog/CHANGELOG.html) for details.
* Added a new sample - NetCube - used in the Unite presentation about netcode.
* Removed Matchmaking, SQP and related samples.

# 2019-07-17
## New features
* Added a prefab based workflow for specifying ghosts. A prefab can contain a `GhostAuthoringComponent` which is used to generate code for a ghost. A `GhostPrefabAuthoringComponent` can be used to instantiate the prefab when spawning ghosts on the client. This replaces the .ghost files, all projects need to be updated to the new ghost definitions.
* Added `ConvertToClientServerEntity` which can be used instead of `ConvertToEntity` to target the client / server worlds in the conversion workflow.
* Added a `ClientServerSubScene` component which can be used together with `SubScene` to trigger sub-scene streaming in the client/ server worlds.
* Added a new *Ping-Multiplay* sample based on the *Ping* sample
    * Created to be the main sample for demonstrating Multiplay compatibility and best practices (SQP usage, IP binding, etc.)
    * Contains both client and server code.  Additional details in readme in `/Assets/Samples/Ping-Multiplay/`.
* **DedicatedServerConfig**: added arguments for `-fps` and `-timeout`
* **NetworkEndPoint**: Added a `TryParse()` method which returns false if parsing fails
    * Note: The `Parse()` method returns a default IP / Endpoint if parsing fails, but a method that could report failure was needed for the Multiplay sample
* **CommandLine**:
    * Added a `HasArgument()` method which returns true if an argument is present
    * Added a `PrintArgsToLog()` method which is a simple way to print launch args to logs
    * Added a `TryUpdateVariableWithArgValue()` method which updates a ref var only if an arg was found and successfully parsed

## Changes
* Changed the default behavior for systems in the default groups to be included in the client and server worlds unless they are marked with `[NotClientServerSystem]`. This makes built-in systems work in multiplayer projects.
* Deleted existing SQP code and added reference to SQP Package (now in staging)
* Removed SQP server usage from basic *Ping* sample
    * Note: The SQP server was only needed for Multiplay compatibility, so the addition of *Ping-Multiplay* allowed us to remove SQP from *Ping*
* Made standalone player use the same network simulator settings as the editor when running a development player
* Made the Server Build option (UNITY_SERVER define) properly set up the right worlds for a dedicated server setup. Setting UNITY_CLIENT in the player settings define results in a client only build being made.
* Debugger now shows all running servers and clients.

## Fixes
* Change `World.Active` to the executing world when updating systems.
* Improve time calculations between client and server.
* **DedicatedServerConfig**: Vsync is now disabled programmatically if requesting an FPS different from the current screen refresh rate
## Upgrade guide
All ghost definitions specified in .ghost files needs to be converted to prefabs. Create a prefab containing a `GhostAuthoringComponent` and authoring components for all required components. Use the `GhostAuthoringComponent` to update the component list and generate code.

# 2019-06-05
## New features
* Added support systems for prediction and spawn prediction in the NetCode. These can be used to implement client-side prediction for networked objects.
* Added some support for generating the code required for replicated objects in the NetCode.
* Generalized input handling in the NetCode.
* New fixed timestep code custom for multiplayer worlds.
## Changes
* Split the NetCode into a separate assembly and improved the folder structure to make it easier to use it in other projects.
* Split the Asteroids sample into separate assemblies for client, server and mixed so it is easier to build dedicated servers without any client-side code.
* Moved MatchMaking to a package and supporting code to a separate folder.
* Upgraded Entities to preview 33.
## Fixes
* Fixed an issue with the reliable pipeline not resending when completely idle.
## Upgrade guide

# 2019-04-16
## New features
* Added network pipelines to enable processing of outgoing and incomming packets. The available pipeline stages are `ReliableSequencedPipelineStage` for reliable UDP messages and `SimulatorPipelineStage` for emulating network conditions such as high latency and packet loss. See [the pipeline documentation](com.unity.transport/Documentation~/pipelines-usage.md) for more information.
* Added reading and writing of packed signed and unsigned integers to `DataStream`. These new methods use huffman encoding to reduce the size of transfered data for small numbers.
* Added a new sample asteroids game which we will be using to develop the new netcode.
## Changes
* Update to Unity.Entities preview 26
* Enable Burst compilation for most jobs
* Made it possible to get the remote endpoint for a connection
* Replacing EndPoint parsing with custom code to avoid having a dependency on System.Net
* Change the ping sample command-line parameters for server to -port and -query_port
* For matchmaking - use an Assignment object containing the ConnectionString, the Roster, and an AssignmentError string instead of just the ConnectionString.
## Fixes
* Fixed an issue with building iOS on Windows
* Fixed inconsistent error handling between platforms when the network buffer is full
## Upgrade guide
Unity 2019.1 is now required.

`BasicNetworkDriver` has been renamed to `GenericNetworkDriver` and a new `UdpNetworkDriver` helper class is also available.

System.Net EndPoints can no longer be used as addresses, use the new NetworkEndpoint struct instead.