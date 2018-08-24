# Dynamic Buffers

A dynamic buffer is a type of component data that allows a variable-sized, "stretchy"
buffer to be associated with an Entity. It behaves as a component type that
carries an internal capacity of a certain number of elements, but can allocate
a heap memory block if the internal capacity is exhausted.

Memory management is fully automatic when using this approach. Memory associated with
dynamic buffers is managed by the Entity Manager so that when a dynamic buffer
component is removed, any associated heap memory is automatically freed as well.

Dynamic buffers supersede fixed array support which has been removed.

## Declaring Buffer Element Types

To declare a buffer, you declare it with the type of element that you will be
putting into the buffer:

    // This describes the number of buffer elements that should be reserved
    // in chunk data for each instance of a buffer. In this case, 8 integers
    // will be reserved (32 bytes) along with the size of the buffer header
    // (currently 16 bytes on 64-bit targets)
    [InternalBufferCapacity(8)]
    public struct MyBufferElement : IBufferElementData
    {
        // These implicit conversions are optional, but can help reduce typing.
        public static implicit operator int(MyBufferElement e) { return e.Value; }
        public static implicit operator MyBufferElement(int e) { return new MyBufferElement { Value = e }; }

        // Actual value each buffer element will store.
        public int Value;
    }

While it seem strange to describe the element type and not the buffer itself,
this design enables two key benefits in the ECS: 

1. It supports having more than one dynamic buffer of type `float3`, or any
   other common value type. You can add any number of buffers that leverage the
   same value types, as long as the elements are uniquely wrapped in a top-level
   struct.

2. We can include buffer element types in Entity archetypes, and it generally
   will behave like having a component.

## Adding Buffer Types To Entities

To add a buffer to an Entity, you can use the normal methods of adding a
component type onto an Entity:

### Using AddBuffer()

    entityManager.AddBuffer<MyBufferElement>(entity);

### Using an archetype

    Entity e = entityManager.CreateEntity(typeof(MyBufferElement));

## Accessing Buffers

There are several ways to access dynamic buffers, which parallel access methods
to regular component data.

### Direct, main-thread only access 

    DynamicBuffer<MyBufferElement> buffer = entityManager.GetBuffer<MyBufferElement>(entity);

### Injection based access

Similar to `ComponentDataArray` you can inject a `BufferArray` which provides
the dynamic buffers in a parallel array to the other injections. This example
provides a system that appends a value to every buffer in an injected set:

    public class InjectionDemo : JobComponentSystem
    {
        public struct Data
        {
            public readonly int Length;
            public BufferArray<EcsIntElement> Buffers;
        }

        [Inject] Data m_Data;

        public struct MyJob : IJobParallelFor
        {
            public BufferArray<EcsIntElement> Buffers;

            public void Execute(int i)
            {
                Buffers[i].Append(i * 3);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new MyJob { Buffers = m_Data.Buffers }.Schedule(m_Data.Length, 32, inputDeps);
        }
    }

## Entity based access

You can also look up buffers on a per-entity basis:

        var lookup = GetBufferArrayFromEntity<EcsIntElement>();
        var buffer = intLookup[myEntity];
        buffer.Append(17);
        buffer.RemoveAt(0);

## Entity based injection access

Similarly to injecting `ComponentDataFromEntity` you can inject
`BufferDataFromEntity` and look up buffers indirectly. 

## Reinterpreting Buffers (experimental)

Buffers can be reinterpreted as a type of the same size. The intention is to
allow controlled type-punning and to get rid of the wrapper element types when
they get in the way. To reinterpret, simply call `Reinterpret<T>`:

    var intBuffer = entityManager.GetBuffer<EcsIntElement>().Reinterpret<int>();

The reinterpreted buffer carries with it the safety handle of the original
buffer, and is safe to use. They use the same underlying buffer header, so
modifications to one reinterpreted buffer will be immediately reflected in
others.

Note that there are no type checks involved, so it is entirely possible to
alias a `uint` and `float` buffer.
