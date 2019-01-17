# Version Numbers

## Scope

The purpose of version numbers (aka. generations) is the detection of potential changes. Amongst other things, they can be used to implement cheap and efficient optimization strategies, e.g. some processing might be skipped when the data it operates on is guaranteed not to have changed since last frame.

It often happens that by performing a very quick conservative version check for a bunch of entities at once, significant performance gains can be easily obtained.

This page lists and documents all the different version numbers used by ECS, in particular conditions that will cause them to change.

## Preliminary Remarks

All version numbers are 32 bits signed integers, they always increase unless they wrap around, signed integer overflow is defined behavior in C#. This means that comparing version numbers should be done using the (in)equality operator, not relational operators.

The right way to check if VersionB is more recent than VersionA is:
`bool VersionBIsMoreRecent = (VersionB - VersionA) > 0;`

There is usually no guarantee by how much a version number will increase.

## EntityId.Version

An `EntityId` is made of an index and a version number. Since indices are recycled, the version number is increased in `EntityManager` every time the Entity is destroyed. If there is a mismatch in the version numbers when an `EntityId` is looked up in `EntityManager`, it means the entity referred to doesn’t exist anymore.

> Before fetching the position of the enemy some unit is tracking via an EntityId, you can call `ComponentDataFromEntity.Exists` that uses the version number to check if the entity still exists.

## World.Version

The version number of a world is increased every time a manager (i.e. system) is created or destroyed.

## EntityDataManager.GlobalVersion

Is increased before every single (job) component system update.

> The purpose of this version number is to be used in conjunction with `System.LastSystemVersion`.

## System.LastSystemVersion

Takes the value of `EntityDataManager.GlobalVersion` after every single (job) component system update.

> The purpose of this version number is to be used in conjunction with `Chunk.ChangeVersion[]`.

## Chunk.ChangeVersion[] (ArchetypeChunk.GetComponentVersion)

For each component type in the archetype, this array contains the value of `EntityDataManager.GlobalVersion` at the time the component array was last accessed as writeable within this chunk. This in no way guarantees that anything has effectively changed, only that it could have potentially changed.

Shared components can never be accessed as writeable, even if there is technically a version number stored for those too, it serves no purpose.

When using the `[ChangedFilter]` attribute in an `IJobProcessComponentData`, the `Chunk.ChangeVersion` for that specific component is compared to `System.LastSystemVersion`, so only chunks whose component arrays have been accessed as writeable since after the system last started running will be processed.

> If the amount of health points of a group of units is guaranteed not to have changed since the previous frame, checking if those units should update their damage model can be skipped altogether.

## EntityManager.m_ComponentTypeOrderVersion[]

For each non-shared component type, the version number is increased every time an iterator involving that type should become invalid. In other words, anything that might modify arrays of that type (not instances).

> If we have static objects identified by a particular component, and a per-chunk bounding box, we know we only need to update those bounding boxes if the type order version changes for that component.

## SharedComponentDataManager.m_SharedComponentVersion[]

These version numbers increase when any structural change happens to the entities stored in a chunk referencing that shared component.

> Imagine we keep a count of entities per shared component, we can rely on that version number to only redo each count if the corresponding version number changes.
