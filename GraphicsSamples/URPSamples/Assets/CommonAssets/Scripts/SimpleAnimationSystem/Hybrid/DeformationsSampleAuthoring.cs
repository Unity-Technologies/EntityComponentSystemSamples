using UnityEngine;

// Adding this component will trigger a conversion system that adds
// components which will calculate the SkinMatrices based on transform data.
internal class DeformationsSampleAuthoring : MonoBehaviour
{
    [Tooltip("Override the color in Deformation Material")]
    public Color Color = new Color(.9f, .3f, .5f);
}
