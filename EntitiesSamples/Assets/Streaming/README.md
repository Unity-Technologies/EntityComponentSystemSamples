# Entities streaming samples

## AssetManagement sample

This sample loads and unloads assets at runtime using `WeakObjectReference`.

## BindingRegistry sample

The BindingRegistry tracks associations between GameObject component fields and corresponding entity component fields. By using the `RegisterBinding` attribute, authoring components can be live updated in sync with changes to the corresponding entity components. (This effect is only visible in "mixed mode" of the inspector window.)

## PrefabAndSceneReferences sample

This sample loads entity prefabs and entity scenes at runtime using `EntityPrefabReference` and `EntitySceneReference`. At bake time, the references are assigned GUID's of the assets, and these GUID's are resolved at runtime.

## RuntimeContentManager sample

This sample demonstrates how to use the [RuntimeContentManager](https://docs.unity3d.com/Packages/com.unity.entities@1.0/manual/content-management.html) to load and release assets at runtime.

## SceneManagement samples

This is a series of samples that demonstrate the scene management API's.

