using Unity.Entities;
using UnityEngine;

namespace Samples.HelloNetcode
{
    public struct BarrelSetup : IComponentData
    {
        public int AmountOfCircles;
        public float Spacing;
        public bool EnableStaticOptimizationProblem;
    }

    [DisallowMultipleComponent]
    public class BarrelSetupAuthoring : MonoBehaviour
    {
        [RegisterBinding(typeof(BarrelSetup), "AmountOfCircles")]
        public int AmountOfCircles;
        [RegisterBinding(typeof(BarrelSetup), "Spacing")]
        public float Spacing;
        [RegisterBinding(typeof(BarrelSetup), "EnableProblem")]
        public bool EnableProblem;

        class Baker : Baker<BarrelSetupAuthoring>
        {
            public override void Bake(BarrelSetupAuthoring authoring)
            {
                BarrelSetup component = default(BarrelSetup);
                component.AmountOfCircles = authoring.AmountOfCircles;
                component.Spacing = authoring.Spacing;
                component.EnableStaticOptimizationProblem = authoring.EnableProblem;
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, component);
            }
        }
    }
}
