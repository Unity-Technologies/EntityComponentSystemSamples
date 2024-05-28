using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using UnityEngine;
using UnityEngine.Scripting;

public struct MultiWorldMousePickerTag : IComponentData {}

public class MultiWorldMousePicker : MonoBehaviour
{
    class MultiWorldMousePickerBaker : Baker<MultiWorldMousePicker>
    {
        public override void Bake(MultiWorldMousePicker authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<MultiWorldMousePickerTag>(entity);
        }
    }
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial class MultiWorldMousePickerGroup : CustomPhysicsSystemGroup
{
    public NativeReference<MousePickSystem.SpringData> m_MousePickerSpringRef;

    public MultiWorldMousePickerGroup() : base(1, true) {}

    protected override void OnCreate()
    {
        base.OnCreate();

        RequireForUpdate<MultiWorldMousePickerTag>();

        m_MousePickerSpringRef = new NativeReference<MousePickSystem.SpringData>(Allocator.Persistent);
        m_MousePickerSpringRef.Value = new MousePickSystem.SpringData {};
    }

    protected override void OnDestroy()
    {
        m_MousePickerSpringRef.Dispose();

        base.OnDestroy();
    }

    internal void SwitchMousePickerState()
    {
        var mousePicker = World.GetExistingSystemManaged<MousePickSystem>();
        var springRef = mousePicker.SpringDataRef;
        mousePicker.SpringDataRef = m_MousePickerSpringRef;
        m_MousePickerSpringRef = springRef;
    }

    protected override void PreGroupUpdateCallback()
    {
        base.PreGroupUpdateCallback();
        SwitchMousePickerState();
    }

    protected override void PostGroupUpdateCallback()
    {
        base.PostGroupUpdateCallback();
        SwitchMousePickerState();
    }
}
