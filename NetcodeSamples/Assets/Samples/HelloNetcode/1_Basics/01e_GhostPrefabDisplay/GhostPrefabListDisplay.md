# HelloNetcode Ghost Prefab Display HUD sample

This sample adds a toggle and scroll view which will display all detected ghost prefabs (Prefab + GhostType components) in every client/server world.

Ghost types which the server has detected but the client has not will be explicitly mentioned as `MISSING` and will be replaced when a prefab with a matching ghost type has been detected.