using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;

namespace Unity.NetCode.Samples
{
    /// <summary>
    /// Helper struct to collect and pass to job dynamic type handles.
    /// The list capacity is fixed to 32 different components types
    /// </summary>
    internal struct ClientOnlyTypeHandleList
    {
        private DynamicComponentTypeHandle dynamicType00;
        private DynamicComponentTypeHandle dynamicType01;
        private DynamicComponentTypeHandle dynamicType02;
        private DynamicComponentTypeHandle dynamicType03;
        private DynamicComponentTypeHandle dynamicType04;
        private DynamicComponentTypeHandle dynamicType05;
        private DynamicComponentTypeHandle dynamicType06;
        private DynamicComponentTypeHandle dynamicType07;
        private DynamicComponentTypeHandle dynamicType08;
        private DynamicComponentTypeHandle dynamicType09;
        private DynamicComponentTypeHandle dynamicType10;
        private DynamicComponentTypeHandle dynamicType11;
        private DynamicComponentTypeHandle dynamicType12;
        private DynamicComponentTypeHandle dynamicType13;
        private DynamicComponentTypeHandle dynamicType14;
        private DynamicComponentTypeHandle dynamicType15;
        private DynamicComponentTypeHandle dynamicType16;
        private DynamicComponentTypeHandle dynamicType17;
        private DynamicComponentTypeHandle dynamicType18;
        private DynamicComponentTypeHandle dynamicType19;
        private DynamicComponentTypeHandle dynamicType20;
        private DynamicComponentTypeHandle dynamicType21;
        private DynamicComponentTypeHandle dynamicType22;
        private DynamicComponentTypeHandle dynamicType23;
        private DynamicComponentTypeHandle dynamicType24;
        private DynamicComponentTypeHandle dynamicType25;
        private DynamicComponentTypeHandle dynamicType26;
        private DynamicComponentTypeHandle dynamicType27;
        private DynamicComponentTypeHandle dynamicType28;
        private DynamicComponentTypeHandle dynamicType29;
        private DynamicComponentTypeHandle dynamicType30;
        private DynamicComponentTypeHandle dynamicType31;

        public int Length { get; set; }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckIndexInRange(int index)
        {
            if (index < 0)
                throw new IndexOutOfRangeException($"Index {index} must be positive.");

            if (index >= Length)
                throw new IndexOutOfRangeException($"Index {index} is out of range in ClientOnlyTypeHandleList of '{Length}' Length.");
        }

        public ref DynamicComponentTypeHandle ElementAt(int index)
        {
            unsafe
            {
                CheckIndexInRange(index);
                return ref UnsafeUtility.ArrayElementAsRef<DynamicComponentTypeHandle>(Ptr, index);
            }
        }

        public unsafe DynamicComponentTypeHandle* Ptr {
            get
            {
                fixed (DynamicComponentTypeHandle* ptr = &dynamicType00)
                    return ptr;
            }
        }

        /// <summary>
        /// Create the type handle list if empty or just update the dynamic handles.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="clientOnlyComponentTypes"></param>
        /// <param name="isReadonly">Set to true when you only need to access the component data readonly</param>
        public void CreateOrUpdateTypeHandleList(ref SystemState state, in NativeArray<ComponentType> clientOnlyComponentTypes, bool isReadonly=false)
        {
            unsafe
            {
                if (Length == 0 && clientOnlyComponentTypes.Length > 0)
                {
                    Length = clientOnlyComponentTypes.Length;
                    var typeHandles = Ptr;
                    for (int i = 0; i < clientOnlyComponentTypes.Length; ++i)
                    {
                        var componentType = clientOnlyComponentTypes[i];
                        if(isReadonly)
                            componentType.AccessModeType = ComponentType.AccessMode.ReadOnly;
                        typeHandles[i] = state.GetDynamicComponentTypeHandle(componentType);
                    }
                }
                else
                {
                    var typeHandles = Ptr;
                    for (int i = 0; i < clientOnlyComponentTypes.Length; ++i)
                        typeHandles[i].Update(ref state);
                }
            }
        }
    }
}
