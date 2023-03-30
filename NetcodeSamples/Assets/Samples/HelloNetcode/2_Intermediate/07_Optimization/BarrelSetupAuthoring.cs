using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct BarrelSetup : IComponentData
    {
        public int AmountOfCircles;
        public int Spacing;
        public bool EnableProblem;
    }

    [DisallowMultipleComponent]
    public class BarrelSetupAuthoring : MonoBehaviour
    {
        [RegisterBinding(typeof(BarrelSetup), "AmountOfCircles")]
        public int AmountOfCircles;
        [RegisterBinding(typeof(BarrelSetup), "Spacing")]
        public int Spacing;
        [RegisterBinding(typeof(BarrelSetup), "EnableProblem")]
        public bool EnableProblem;

        class Baker : Baker<BarrelSetupAuthoring>
        {
            public override void Bake(BarrelSetupAuthoring authoring)
            {
                BarrelSetup component = default(BarrelSetup);
                component.AmountOfCircles = authoring.AmountOfCircles;
                component.Spacing = authoring.Spacing;
                component.EnableProblem = authoring.EnableProblem;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
