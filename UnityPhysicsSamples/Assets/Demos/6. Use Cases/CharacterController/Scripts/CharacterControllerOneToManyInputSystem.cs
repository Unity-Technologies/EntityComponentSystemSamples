using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;
using static CharacterControllerUtilities;
using static Unity.Physics.PhysicsStep;



// This input system simply applies the same character input 
// information to every character controller in the scene
[UpdateAfter(typeof(ExportPhysicsWorld)), UpdateBefore(typeof(CharacterControllerSystem))]
public class CharacterControllerOneToManyInputSystem : ComponentSystem
{
    EntityQuery m_CharacterControllerInputQuery; 

    protected override void OnCreate()
    {
        m_CharacterControllerInputQuery = GetEntityQuery(ComponentType.ReadOnly<CharacterControllerInput>());
    }

    protected override void OnUpdate()
    {
        // Read user input
        var input = m_CharacterControllerInputQuery.GetSingleton<CharacterControllerInput>();
        Entities.ForEach(
            (ref CharacterControllerInternalData ccData) =>
            {
                ccData.Input.Movement = input.Movement;
                ccData.Input.Looking = input.Looking;
                ccData.Input.Jumped = input.Jumped;
            }
        );
    }
}
