# HelloNetcode host migration sample

See details about the feature in the [Host migration](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/host-migration/host-migration.html) section in the manual.

See details about the feature implementation in this sample in the [Host migration in Asteroids](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/host-migration/host-migration-sample.html) section.

> [!NOTE]
> The samples which contain no ghosts, like the hello netcode samples `RPC`, `ConnectionMonitor`, `GoInGame` and `ConnectionApproval` will not have any data to migrate and will not be useable to test host migrations. The host migration system will only start collecting host migration data when there is at least 1 ghost in the server world. Therefore, normally, no host migration data will be uploaded until at least one ghost is spawned which never happens in these samples. The host migration controller will then abort and report an error as no host migration data is found during a host migration event.