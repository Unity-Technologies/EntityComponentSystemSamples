using Unity.Collections;
using Unity.Entities;
using Unity.Physics.Authoring;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using UnityEngine;

public struct MultiWorldMousePickerTag : IComponentData {}

public class MultiWorldMousePicker : MonoBehaviour
{
    class MultiWorldMousePickerBaker : Baker<MultiWorldMousePicker>
    {
        public override void Bake(MultiWorldMousePicker authoring)
        {
            AddComponent<MultiWorldMousePickerTag>();
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
        m_MousePickerSpringRef = new NativeReference<MousePickSystem.SpringData>(Allocator.Persistent);
        m_MousePickerSpringRef.Value = new MousePickSystem.SpringData {};
        RequireForUpdate<MultiWorldMousePickerTag>();
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

        // Enable debug display clean in this group - since it updates before main physics.
        World.GetExistingSystemManaged<CleanPhysicsDebugDataSystem>().Enabled = true;
    }

    protected override void PostGroupUpdateCallback()
    {
        base.PostGroupUpdateCallback();
        SwitchMousePickerState();

        //// Disable debug display for main physics - since it updates after this group.
        World.GetExistingSystemManaged<CleanPhysicsDebugDataSystem>().Enabled = false;
    }
}
