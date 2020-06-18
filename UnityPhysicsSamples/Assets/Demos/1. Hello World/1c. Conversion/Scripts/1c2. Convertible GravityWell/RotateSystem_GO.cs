using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class RotateSystem_GO : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        foreach(var rotator in GameObject.FindObjectsOfType<RotateComponent_GO>())
        {
            var av = rotator.LocalAngularVelocity * Time.deltaTime;
            var rotation = Quaternion.Euler(av);
            rotator.gameObject.transform.localRotation *= rotation;
        }
    }
}


#region ECS
[UpdateBefore(typeof(GravityWellSystem_GO_ECS))]
public class RotateSystem_GO_ECS : SystemBase
{
    protected override void OnUpdate()
    {
        // Create local deltaTime so it is accessible inside the ForEach lambda
        var deltaTime = Time.DeltaTime;

        Entities
        .WithBurst()
        .ForEach((ref Rotation rotation, in RotateComponent_GO_ECS rotator) =>
        {
            var av = rotator.LocalAngularVelocity * deltaTime;
            rotation.Value = math.mul(rotation.Value, quaternion.EulerZXY(av));
        }).Run();
    }
}
#endregion

