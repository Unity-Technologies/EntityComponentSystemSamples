using Unity.Entities;
using UnityEngine;

public struct TestCustomSerializer : IComponentData
{
    public int numInstances;
    public float percentChunkChange;
    public float percentEntityChanges;
    public bool useCustomSerializer;
    public bool usePreserialization;
}

public class TestCustomSerializerAuthoring : MonoBehaviour
{
    public int numInstances;
    public int percentChunkChange;
    public int percentEntityChanges;
    public bool useCustomSerializer;
    public bool usePreserialization;

    private class Baker : Unity.Entities.Baker<TestCustomSerializerAuthoring>
    {
        public override void Bake(TestCustomSerializerAuthoring customSerializerAuthoring)
        {
            AddComponent(GetEntity(TransformUsageFlags.None), new TestCustomSerializer
            {
                numInstances = customSerializerAuthoring.numInstances,
                percentChunkChange = customSerializerAuthoring.percentChunkChange*0.01f,
                percentEntityChanges = customSerializerAuthoring.percentEntityChanges*0.01f,
                useCustomSerializer = customSerializerAuthoring.useCustomSerializer,
                usePreserialization = customSerializerAuthoring.usePreserialization,
            });
        }
    }
}
