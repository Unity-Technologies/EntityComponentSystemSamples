# Register prefabs sample

This sample is showing how prefab loading can work. It contains both scene which will register a prefab via the `Netcode.RegisterPrefab` API when loaded, and traditional subscenes which will be baked with a ghost prefab entity.

Notes:
- The Frontend scene must be disabled in the build scene list and the RegisterPrefab scene enabled instead. As it is controlling how server/client startup happens it can't be loaded via the Frontend scene which normally handles this.
- The `Cube1` and `Cube2` prefabs are configured to be automatically registered and to skip automatic registration respectively. The manual registration which happens when `Scene1` (with `Cube1`) and `Scene2` (with `Cube2`) are loaded will run depending on how registration is configured (via `SkipAutomaticPrefabRegistration` toggle). This toggle can be changed to test behavior in different ways.
- Currently, by default, registered prefabs aren't required to be the same on the client and the server. You can test required matching prefabs by setting the `GhostPrefabLoadingTimeout` to zero or higher `NetcodeConfig` asset.

Limitations:
- The order in which prefabs are registered (or scenes loaded) is important. The client must perform operations in the same order as the server.
- `Scene1` and `Scene2` can be loaded before any worlds are created (before server/client start up), as the prefab registration is stored and deployed when a world is ready for it. The ghosts however cannot be instantiated before the server/client starts (and buttons in the UI will remain disabled until then).
- Because of how automatic prefab registration works, it can detect ghost prefabs between playmode sessions in the editor. So for example `Cube1` might be detected no a second run of playmode even before `Scene1` has been loaded (button remains disabled though but technically the ghost could be instantiated). This can also happen in a built player when starting, disconnecting and starting again for the same reason.
