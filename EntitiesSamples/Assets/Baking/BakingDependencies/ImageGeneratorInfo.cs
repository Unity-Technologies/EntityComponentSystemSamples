using UnityEngine;

namespace Baking.BakingDependencies
{
    [CreateAssetMenu(menuName = "ImageGeneratorInfo")]
    public class ImageGeneratorInfo : ScriptableObject
    {
        [Range(0.0f, 1.0f)]
        public float Spacing;
        public Mesh Mesh;
        public Material Material;
    }
}
