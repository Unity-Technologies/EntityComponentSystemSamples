using Unity.Entities;
using Unity.Rendering;

[UnityEngine.DisallowMultipleComponent]
public class MaterialAuthoring : UnityEngine.MonoBehaviour
{
    class MaterialBaker : Baker<MaterialAuthoring>
    {
        public override void Bake(MaterialAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new URPMaterialPropertyBaseColor {Value = 1});
        }
    }
}
