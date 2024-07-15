# Creating a custom chunk serialization method

This sample shows how implement and register a custom serialization method, to serialize ghost chunks of a given archetype.

## Why using custom serialization function

Only one reason: **Peformance**! When using the a custom serialization function designed for a specific archetype,
you can make (or relax) certain assumptions: I.e.
* No checks for removed components.
* Optimise the loops that gather and copy data.
* Generally write things in a way that gives both the compiler, and burst in primis, more opportunities for
  auto-vectorization.

In general, it also gives a CPU performance gain in cases where the ghost has a lots of small components, reducing function pointer call overhead (and more importantly single call setup overhead).

### Current limitation of the custom serializer API
This is an advanced feature of the Netcode package (we can say it is "in-preview"), available for now only when using the PrefabCreation API (i.e. manually created ghost entities types).

# USING THE API
## Registering custom serializers
In order to use custom chunk serialization function; **said function must be registered to the GhostCollection system, before the
prefabs are collected and processed.** A good place to put the registration, is in the system where you create the prefabs.

To register your custom serialization function, you need to retrieve the `GhostCollectionCustomSerializer` singleton,
and add an entry for a given archetype. A ghost archetype is identified by its GhostType hash.

```csharp
GhostPrefabCreation.ConvertToGhostPrefab(EntityManager, prefab, prefabConfig);
var hash = (Unity.Entities.Hash128)EntityManager.GetComponentData<GhostType>(prefab);
var customSerializers = SystemAPI.GetSingletonRW<GhostCollectionCustomSerializers>();
customSerializers.ValueRW.Serializers.Add(hash, new GhostPrefabCustomSerializer
{
    SerializeChunk = CustomChunkSerializer.SerializerFunc, 
    PreSerializeChunk = CustomChunkSerializer.PreSerializerFunc
});
```

Both `SerializeChunk` and  `PreSerializeChunk` functions are optional, meaning that you can have either one of them, or both.

## Register a custom component list provider for the archetype
In order to be able to serialize a component, it is necessary to access and retrieve the component data from the chunk.
This is achieved by accessing the list of serialized components registered for this archetype, and retrieve from them a list of type handles (the `DynamicTypeHandle`) for each component type.

Each the prefab processed by the `GhostCollectionSystem` has:
- one entry in the `GhostCollectionPrefabSerializer` list, that contains a bunch of metadata information to serialize the type.
- each serialized component will create an entry in the `GhostCollectionComponentIndex` (for root and child entities in order) that contains (among other things):
  - which serializer to use (the SerializerIndex field)
  - an index in the `DynamicTypeList` (ComponentIndex) that let you retrieve the `DynamicComponentTypeHandle`.

```csharp
var typeData = ghostCollectionPrefabSerializer[ghostType];
var componentIndex = typeData.FirstComponent; 
var index = componentIndices[componentIndex];
var dynamicTypeHandle = dynamicTypeList[index.ComponentIndex];
```

When writing a custom serialization method, it can be tricky to find the index of a specific component type
in the `componentIndices` list, for the current ghost archetype. That because a components position in the list depends upon:
- its stable-type-hash
- the serializer hash associated with that specific component for that archetype.

Therefore, we support passing a `CollectComponents` function pointer to the GhostPrefabCreation.ConvertToGhostPrefab` (a field of the config argument),
such that you know exactly the index into the `componentIndices` list for a specific component type.

For example:

```csharp
public static void CollectComponents(IntPtr componentTypesPtr, IntPtr componentCountPtr)
{
    ref var componentTypes = ref GhostComponentSerializer.TypeCast<NativeList<ComponentType>>(componentTypesPtr);
    ref var componentCount = ref GhostComponentSerializer.TypeCast<NativeArray<int>>(componentCountPtr);
    //Root
    componentTypes.Add(ComponentType.ReadWrite<GhostOwner>());
    componentTypes.Add(ComponentType.ReadWrite<LocalTransform>());
    componentTypes.Add(ComponentType.ReadWrite<IntCompo1>());
    componentTypes.Add(ComponentType.ReadWrite<IntCompo2>());
    componentTypes.Add(ComponentType.ReadWrite<IntCompo3>());
    componentTypes.Add(ComponentType.ReadWrite<FloatCompo1>());
    componentTypes.Add(ComponentType.ReadWrite<FloatCompo2>());
    componentTypes.Add(ComponentType.ReadWrite<FloatCompo3>());
    componentTypes.Add(ComponentType.ReadWrite<InterpolatedOnlyComp>());
    componentTypes.Add(ComponentType.ReadWrite<OwnerOnlyComp>());
    componentTypes.Add(ComponentType.ReadWrite<Buf1>());
    componentTypes.Add(ComponentType.ReadWrite<Buf2>());
    componentTypes.Add(ComponentType.ReadWrite<Buf3>());
    componentCount[0] = 13;
    //Child 1
    componentTypes.Add(ComponentType.ReadWrite<IntCompo1>());
    componentTypes.Add(ComponentType.ReadWrite<FloatCompo1>());
    componentTypes.Add(ComponentType.ReadWrite<Buf1>());
    componentCount[1] = 3;
    //Child 2
    componentTypes.Add(ComponentType.ReadWrite<IntCompo2>());
    componentTypes.Add(ComponentType.ReadWrite<FloatCompo2>());
    componentTypes.Add(ComponentType.ReadWrite<Buf2>());
    componentCount[2] = 3;
}
```

You can find this function in the `CustomChunkSerializer.cs` file.

When the function is present, the components list for that archetype use the order specified by that method, giving you the possibility
to access the component types information consistently, and also allowing you to define the component serialization order.

## Implementing custom chunk serializer

We provide a bunch of utility methods that can be used to serialize the enable bits, buffers and components inside the
`CustomGhostSerializerHelpers` class.
The `CustomChunkSerializer.cs` can be considered a sort of template that you can reuse to write or generate your serializer almost
automatically (by just invoking the provider helper functions).

To make the code as re-usable as possible (and reducing mistakes), you must re-use the generated component serializer struct
(i.e `MyAssembly.Generated.MyComponentGhostComponentSerializer`) and:
- invoke on an instance of that serializer one of the `CopyToSnapshot<T>` methods.
- invoke on an instance of that serializer one of the `SerializeWithSingleBaseline` or `SerializeWithThreeBaseline` or `SerializeBuffer`.

Unfortunately, given that the `MyAssembly.Generated.MyComponentGhostComponentSerializer` serializer is auto-generated, most IDE's will not help auto-complete the method name, nor will they recognize the classes existence. But the code will compile correctly.

The serialization is divided in two steps:

### STEP 1 - COPY TO SNAPSHOT
You just need to implement the `CustomChunkSerializer.CopyComponentsToSnapshot` method, by copying the enable bits (if necessary), and copying the component data. For example:

```csharp

new Unity.NetCode.Generated.GhostOwnerGhostComponentSerializer().CopyComponentToSnapshot(chunk, ref context,
    ghostChunkComponentTypesPtr, indices[0], snapshotPtr, ref snapshotOffset);
new Unity.NetCode.Generated.TransformDefaultVariantGhostComponentSerializer().CopyComponentToSnapshot(chunk, ref context,
    ghostChunkComponentTypesPtr,indices[1], snapshotPtr, ref snapshotOffset);
CustomGhostSerializerHelpers.CopyEnableBits(chunk, context.startIndex, context.endIndex, context.snapshotStride,
    ref ghostChunkComponentTypesPtr[indices[2].ComponentIndex], enableBits, ref maskOffset);
```

Similarly, for child entity components, we have similar methods that just scaffold some implementation details and boilerplate template code.

### STEP 2 - SERIALIZE TO THE DATASTREAM
After the data has been copied into the snapshot buffer, we can serialize the snapshot (entity by entity), into the data stream. Based on the acknowledge baselines, we should serialize by either:
- A single or default baseline
- three baselines.

The `CustomChunkSerializer` class has two methods that need to be implemented:
- `SerializeWithSingleBaseline`
- `SerializeWithThreeBaseline`

All code-generated serializers provide three static methods can be used for writing the snapshot data into the stream:
- `SerializeSingleBaseline`: serialize the data using only one baseline (either acked or the default one).
- `SerializeThreeBaseline`: serialize the data using the last three baselines (all acked) and by predicting the value to reduce the delta.
- `SerializeBuffer`: serialize a buffer to the stream using one single baseline (either the default or the acked one).

See the `CustomChunkSerializer.cs` for an example how to use them. But as a short example:

```csharp

//With a single baseline
compBitSize[0*compBitSizeStride] = default(Unity.NetCode.Generated.GhostOwnerGhostComponentSerializer).SerializeComponentThreeBaseline(snapshotData,baseline0Ptr,
    baseline1Ptr, baseline2Ptr, changeMaskData, ref changeMaskOffset, ref snapshotOffset, ref predictor, ref writer, compressionModel);

//With three baseline
compBitSize[0*compBitSizeStride] = default(Unity.NetCode.Generated.GhostOwnerGhostComponentSerializer).SerializeComponentThreeBaseline(snapshotData,baseline0Ptr,
    baseline1Ptr, baseline2Ptr, changeMaskData, ref changeMaskOffset, ref snapshotOffset, ref predictor, ref writer, compressionModel);    
```




