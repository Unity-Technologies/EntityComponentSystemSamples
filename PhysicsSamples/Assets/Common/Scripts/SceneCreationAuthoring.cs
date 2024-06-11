using UnityEngine;

// Base class of authoring components that create scene from code, using SceneCreationSystem
public abstract class SceneCreationAuthoring<T> : MonoBehaviour
    where T : SceneCreationSettings, new()
{
    public Material DynamicMaterial;
    public Material StaticMaterial;
}
