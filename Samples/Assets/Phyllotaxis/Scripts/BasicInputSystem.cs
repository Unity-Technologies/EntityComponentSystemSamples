using Unity.Entities;
using UnityEngine;

public class InputSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.P)) SineSystemOnAxis.ssoa.Enabled = !SineSystemOnAxis.ssoa.Enabled;
    }
}
