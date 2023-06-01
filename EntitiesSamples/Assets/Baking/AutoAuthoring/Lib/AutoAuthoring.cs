using System;
using Unity.Entities;
using Unity.Properties;
using UnityEngine;

namespace AutoAuthoring
{
    /// <summary>
    /// Suppresses the top-level foldout on a complex property
    /// </summary>
    public sealed class AutoAuthoringData : PropertyAttribute {}

    /// <summary>
    /// Base class for authoring components with default bakers.
    /// </summary>
    public abstract class AutoAuthoringBase : MonoBehaviour
    {
        /// <summary>
        /// Resolves the Entity references and bakes the authoring data.
        /// Override this method to customize the baking of authoring data.
        /// </summary>
        /// <param name="baker">The baker instance.</param>
        internal abstract void Bake(IBaker baker);

        /// <summary>
        /// The type of the ECS component to be authored.
        /// </summary>
        /// <returns>Returns the type of the ECS component.</returns>
        public abstract Type GetComponentType();

        /// <summary>
        /// Is true if the ECS component is a buffer type, otherwise is false.
        /// </summary>
        public bool IsBufferComponent => typeof(IBufferElementData).IsAssignableFrom(GetComponentType());

        [BakeDerivedTypes]
        class Baker : Baker<AutoAuthoringBase>
        {
            public override void Bake(AutoAuthoringBase authoring)
            {
                authoring.Bake(this);
            }
        }
    }

    /// <summary>
    /// The base class for authoring components, specialized for each ECS component type.
    /// </summary>
    /// <typeparam name="TComponentData">The type of the ECS component.</typeparam>
    [DisallowMultipleComponent]
    public abstract class AutoAuthoringGeneric<TComponentData> : AutoAuthoringBase
        where TComponentData : new()
    {
        [Serializable]
        protected internal class ComponentDataInfo
        {
            public TComponentData AuthoringData;
            public GameObject[] References;
        }

        [Serializable]
        protected internal struct ComponentDataInfoArray
        {
            public ComponentDataInfo[] ComponentArray;
        }

        /// <summary>
        /// The serialized component data.
        /// </summary>
        /// <remarks>
        /// The data is reflected in the UI using a custom property drawer for the attribute <see cref="AutoAuthoringData"/>.
        /// </remarks>
        [SerializeField]
        [AutoAuthoringData]
        protected internal ComponentDataInfoArray InfoArray;

        static int _EntityFieldCount = -1;

        /// <summary>
        /// The number of entity fields found in the ECS component.
        /// </summary>
        protected internal static int EntityFieldCount =>
            _EntityFieldCount = _EntityFieldCount == -1 ? ComputeEntityFieldCount() : _EntityFieldCount;

        /// <inheritdoc />
        public override Type GetComponentType() => typeof(TComponentData);

        void Reset()
        {
            OnValidate();
        }

        static int ComputeEntityFieldCount()
        {
            // visit the properties of the component type to count the Entity fields
            // Note: this gets called once after a domain reload, for each instantiated type
            var visitor = new EntityFieldCountVisitor();
            PropertyContainer.Accept(visitor, new TComponentData());
            return visitor.EntityFieldCount;
        }

        void OnValidate()
        {
            // initialize the GameObject reference arrays

            if (  (typeof(IComponentData).IsAssignableFrom(typeof(TComponentData))
                || typeof(ISharedComponentData).IsAssignableFrom(typeof(TComponentData))
                || typeof(ICleanupComponentData).IsAssignableFrom(typeof(TComponentData)))
                && InfoArray.ComponentArray is not {Length: 1})
            {
                InfoArray.ComponentArray = new ComponentDataInfo[] {new() {References = new GameObject[EntityFieldCount]}};
            }

            if (typeof(IBufferElementData).IsAssignableFrom(typeof(TComponentData))
                || typeof(ICleanupBufferElementData).IsAssignableFrom(typeof(TComponentData)))
            {
                if (InfoArray.ComponentArray == null)
                    InfoArray.ComponentArray = Array.Empty<ComponentDataInfo>();

                for (int i = 0, count = InfoArray.ComponentArray.Length; i < count; ++i)
                {
                    if (InfoArray.ComponentArray[i] == null)
                        continue;

                    ref var element = ref InfoArray.ComponentArray[i];
                    if (element.References == null || element.References.Length != EntityFieldCount)
                    {
                        element.References = new GameObject[EntityFieldCount];
                    }
                }
            }
        }
    }

    /// <summary>
    /// Provides a default baker and support for Entity references for a <see cref="IComponentData"/> ECS component.
    /// </summary>
    /// <typeparam name="TComponentData">The ECS component, which must be unmanaged and implement <see cref="IComponentData"/>.</typeparam>
    public class AutoAuthoring<TComponentData> : AutoAuthoringGeneric<TComponentData>
        where TComponentData : unmanaged, IComponentData
    {
        /// <summary>
        /// The authoring data instance.
        /// </summary>
        protected TComponentData Data => InfoArray.ComponentArray[0].AuthoringData;

        /// <inheritdoc />
        internal override void Bake(IBaker baker)
        {
            ref var info = ref InfoArray.ComponentArray[0];

            var visitor = new ComponentDataPatcher(baker, info.References);
            PropertyContainer.Accept(visitor, ref info.AuthoringData);
            var entity = baker.GetEntity(TransformUsageFlags.Dynamic);
            baker.AddComponent(entity, info.AuthoringData);
        }
    }

#if !UNITY_DISABLE_MANAGED_COMPONENTS
    /// <summary>
    /// Provides a default baker and support for Entity references for a <see cref="IComponentData"/> ECS managed component.
    /// </summary>
    /// <typeparam name="TComponentData">The ECS component, which must be a <b>class</b> and implement <see cref="IComponentData"/>.</typeparam>
    public class ManagedAutoAuthoring<TComponentData> : AutoAuthoringGeneric<TComponentData>
        where TComponentData : class, IComponentData, new()
    {
        /// <summary>
        /// The authoring data instance.
        /// </summary>
        protected TComponentData Data => InfoArray.ComponentArray[0].AuthoringData;

        /// <inheritdoc />
        internal override void Bake(IBaker baker)
        {
            ref var info = ref InfoArray.ComponentArray[0];

            var visitor = new ComponentDataPatcher(baker, info.References);
            PropertyContainer.Accept(visitor, ref info.AuthoringData);
            var entity = baker.GetEntity(TransformUsageFlags.Dynamic);
            baker.AddComponentObject(entity, info.AuthoringData);
        }
    }
#endif // !UNITY_DISABLE_MANAGED_COMPONENTS

    /// <summary>
    /// Provides a default baker and support for Entity references for a <see cref="IBufferElementData"/> ECS buffer.
    /// </summary>
    /// <typeparam name="TComponentData">The ECS buffer element type, which must be unmanaged and implement <see cref="IBufferElementData"/>.</typeparam>
    public class BufferAutoAuthoring<TComponentData> : AutoAuthoringGeneric<TComponentData>
        where TComponentData : unmanaged, IBufferElementData
    {
        /// <inheritdoc />
        internal override void Bake(IBaker baker)
        {
            var entity = baker.GetEntity(TransformUsageFlags.Dynamic);
            var buffer = baker.AddBuffer<TComponentData>(entity);
            var visitor = new ComponentDataPatcher(baker);
            for (int index = 0, count = InfoArray.ComponentArray?.Length ?? 0; index < count; ++index)
            {
                ref var element = ref InfoArray.ComponentArray[index];
                visitor.Reset(element.References);
                PropertyContainer.Accept(visitor, ref element.AuthoringData);
                buffer.Add(element.AuthoringData);
            }
        }
    }

    /// <summary>
    /// Provides a default baker and support for Entity references for a <see cref="ISharedComponentData"/> ECS component.
    /// </summary>
    /// <typeparam name="TComponentData">The ECS component, which must be unmanaged and implement <see cref="ISharedComponentData"/>.</typeparam>
    public class SharedAutoAuthoring<TComponentData> : AutoAuthoringGeneric<TComponentData>
        where TComponentData : unmanaged, ISharedComponentData
    {
        /// <summary>
        /// The authoring data instance.
        /// </summary>
        protected TComponentData Data => InfoArray.ComponentArray[0].AuthoringData;

        /// <inheritdoc />
        internal override void Bake(IBaker baker)
        {
            ref var info = ref InfoArray.ComponentArray[0];
            var entity = baker.GetEntity(TransformUsageFlags.Dynamic);
            baker.AddSharedComponent(entity, info.AuthoringData);
        }
    }

    /// <summary>
    /// Provides a default baker and support for Entity references for a <see cref="ISharedComponentData"/> ECS managed component.
    /// </summary>
    /// <typeparam name="TComponentData">The ECS component, which must be a <b>struct</b> and implement <see cref="ISharedComponentData"/>.</typeparam>
    public class ManagedSharedAutoAuthoring<TComponentData> : AutoAuthoringGeneric<TComponentData>
        where TComponentData : struct, ISharedComponentData
    {
        /// <summary>
        /// The authoring data instance.
        /// </summary>
        protected TComponentData Data => InfoArray.ComponentArray[0].AuthoringData;

        /// <inheritdoc />
        internal override void Bake(IBaker baker)
        {
            ref var info = ref InfoArray.ComponentArray[0];

            var entity = baker.GetEntity(TransformUsageFlags.Dynamic);
            baker.AddSharedComponentManaged(entity, info.AuthoringData);
        }
    }
}
