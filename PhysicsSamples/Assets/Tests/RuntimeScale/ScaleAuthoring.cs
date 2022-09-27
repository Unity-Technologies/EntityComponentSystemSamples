using UnityEngine;
using Unity.Entities;
using Unity.Transforms;

public class ScaleAuthoring : MonoBehaviour
{
    public float UniformScale = 1.0f;

    class ScaleBaker : Baker<ScaleAuthoring>
    {
        public override void Bake(ScaleAuthoring authoring)
        {
            AddComponent(new Scale() { Value = authoring.UniformScale });
        }
    }
}
