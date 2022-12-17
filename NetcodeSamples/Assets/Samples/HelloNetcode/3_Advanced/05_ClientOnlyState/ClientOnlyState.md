# HelloNetcode Client-Only state backup

The sample show how to backup and restore predicted ghosts components that are not replicated by the server, and that are (usually) present only on the client. 
I.e components that are used to track a client-only state machine, counters, animation states etc.

You can learn more about prediction [here](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/prediction.html).

## Requirements

Connection needs to be in game already.
* GoInGame
* SpawnPlayer

## Sample description
The sample uses the helper library SamplesCommon.ClientOnlyComponentBackup, that provides a common set of utility, systems, and logic for restoring and backup client-only components.

We choose to make the *PlayerMovement* component not replicated by the server. For that purpose, we created a new player ghost prefab, similar to the one used in the SpawnPlayer sample,
and by using the the *ghost authoring inspector component*, we customised the *PlayerMovement* to be not replicated (but still present on the server and client, because the server is also using it)
by setting the variant to use as 'DontSerializeVariant'. You can learn more about variants and their setup in [Ghost component types and variants](https://docs.unity3d.com/Packages/com.unity.netcode@latest?subfolder=/manual/ghost-types-templates.html).
 
The client requests the *PlayerMovement* component to be backup/restored by registering it to the **ClientOnlyCollection** singleton, by calling the
RegisterClientOnlyComponents method. **The registration must done before entering in game**.


At runtime, on the client worlds, two systems are responsible to copy and restore the components states as part of the prediction loop:
- **ClientOnlyComponentBackupSystem**: responsible to backup the ghost which has client-only components and create other accessory metadata.
- **ClientOnlyComponentRestoreSystem**: responsible to restore the client-only components when new snapshots are received.

### How the backup and restore systems works

The *ClientOnlyComponentBackupSystem* run at the end of the prediction loop, and it is responsible to build some new metadata, necessary to backup the component state, and to backup the component data.
In particular:
- Collects and create metadata for all ghost prefabs that present a client-only component. This is a pre-processing stage performed before the actual backup can take place. 
- Adds to all predicted ghosts that uses a prefab type with client-only data a new ClientOnlyBackup state component. The component contains a buffer, used to store the component backup, 
and other accessory data.
- For each full prediction tick, it processes all predicted ghosts with a ClientOnlyBackup, and stores their client-only component states in the backup buffer. 

The components backup is stored in a resizable circular buffer. The buffer does not have a fixed size because it need to grow as necessary to accomodate latency and the fact ghosts are sent at different interval/priority. All predicted tick since the last snapshot
received for that ghost must be present, in order to be able to restore the correct components states.

The *ClientOnlyComponentRestoreSystem* is responsible to restore the component states. It runs as part of the GhostSimulationSystemGroup, after the GhostUpdateSystem.
This order is particularly important for two reasons:
- The GhostUpdateSystem updates for all the predicted ghost the tick at which the prediction should start. As such, this information must be up-to-date when the ClientOnlyComponentRestoreSystem runs.
- The GhostUpdateSystem updates all the ghost components states, because a snapshot is received or a "partial" restore (prediction backup).
 
By running the system after the GhostUpdate and before the next prediction loop, we are guaranteeing that **all the components are restored to the correct tick** inside the whole simulation group.

The restoring process itself does two important step:
- refresh the component data for all predicted ghosts that need to be updated, by mem-copying back the component the data from the backup.
- reduces the size of the backup buffer, by removing the old component backups. This let the buffer sizes to stay in check and have bounded capacity.

Further information can be found in the SamplesCommon.ClientOnlyComponentBackup (see all comments in code).

### Notes
The sample could be seen as a little "improper", meaning that the component is used also by the server to drive some simulation. However, due to the nature of the logic itself (and its predictability),
the client can actually completely predict the value of the counter. And because of that we choose to use it as an example.

### Limitations
- Buffers are currently not supported
- Only 32 client-only component types can be registered.
