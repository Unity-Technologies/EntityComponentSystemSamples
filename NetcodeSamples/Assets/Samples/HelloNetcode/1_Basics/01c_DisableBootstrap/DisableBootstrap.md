# HelloNetcode Disabling the Bootstrap

When the Entities package is installed, it automatically creates a 'Default World' via its custom bootstrapping (see `Unity.Entities.AutomaticWorldBootstrap` class),
so that authoring components in the loaded scenes can be injected (upon entering Play Mode) via a fast-path.

Similarly, Netcode for Entities overrides this (via `ClientServerBootstrap` implementing `ICustomBoostrap`) to create two worlds by default;
[a client world, and a server world](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/client-server-worlds.html).
Each are automatically injected with the appropriate authoring data,
and [this is how they automatically connect at startup](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/network-connection.html#connection-flow).

## Requirements

In some situations, it's undesirable to have worlds automatically created at start-up (e.g. when booting to a UI frontend, rather than a game scene).
Therefore, netcode provides a few ways to disable the automatic Entities bootstrapping:
1. Project-wide: Implement an `ICustomBoostrap` by inheriting from `ClientServerBootstrap`. See the [Bootstrap/FrontendBootstrap.cs](../01_BootstrapAndFrontend/Bootstrap/FrontendBootstrap.cs) file for an example.
2. Project-wide: Create a `NetcodeConfig`, set its `EnableClientServerBootstrap` enum to `EnableBootstrapSetting.DisableAutomaticBootstrap`, and then set this `ScriptableObject` as the default via the 'Netcode for Entities' Project Settings. 
3. Per-Scene Override: Add the `OverrideAutomaticNetcodeBootstrap` to a root `GameObject` in your active scene, and set its field to `EnableBootstrapSetting.DisableAutomaticBootstrap`.

>[!NOTE]
> The per-scene override `OverrideAutomaticNetcodeBootstrap` can also be used to re-enable bootstrapping selectively, if disabled project-wide via `NetcodeConfig` Project Setting.

## Adhering to `EnableBootstrapSetting.DisableAutomaticBootstrap` overrides in user-code `ICustomBootstrap`/`ClientServerBootstrap` implementations 

If you write your own `ICustomBoostrap` implementation, it will **not** automatically respect `OverrideAutomaticNetcodeBootstrap`, nor any `NetcodeConfig.Global` setting.
To query these two settings via your own bootstrapper, call `DetermineIfBootstrappingEnabled` as follows:

```csharp
    // The preserve attribute is required to make sure the bootstrap is not stripped in il2cpp builds with stripping enabled.
    [UnityEngine.Scripting.Preserve]
    // The bootstrap needs to extend `ClientServerBootstrap`, there can only be one class extending it in the project.
    public class MyGameCustomBootstrap : ClientServerBootstrap
    {
        // The initialize method is what Entities calls to create the default worlds.
        public override bool Initialize(string defaultWorldName)
        {
            // If the user added an `OverrideDefaultNetcodeBootstrap` MonoBehaviour to their active scene,
            // or disabled Bootstrapping project-wide via a `NetcodeConfig.Global`, we should respect that here.
            if (!DetermineIfBootstrappingEnabled())
                return false;
            
            ...
        }
    }
```

Alternatively, you can query only for the `OverrideAutomaticNetcodeBootstrap` by calling `DiscoverAutomaticNetcodeBootstrap`, which returns the `MonoBehaviour` if found (and `null` if not).

## Sample description

This DisableBootstrap sample makes use of the `OverrideAutomaticNetcodeBootstrap` approach to disable bootstrapping exclusively for this scene.
I.e. You can observe - upon entering Play Mode from the [DisableBootstrap scene](DisableBootstrap.unity) - that no netcode worlds are created.
We recommend viewing and debugging netcode worlds (and their connections) via the [Netcode for Entities PlayMode Tools Window](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/playmode-tool.html), which also contains many other netcode configuration options, including further bootstrap customization.
