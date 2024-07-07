# HelloNetcode Client Side Animation sample

## Requirements

The spawn player sample is used to trigger the auto spawning of the player when establishing connection.
The character controller is used to move and rotate the player around using the keyboard and mouse input.

* GoInGame
* SpawnPlayer
* CharacterController

## Sample description

This sample code demonstrates how to add animation to the spawned player using the built-in animation system known as [Mecanim](https://docs.unity3d.com/Manual/AnimationOverview.html) in Unity.
The animation runs on the client-side only and is not synchronized to the server. The translation of the parent entity is the only component that synchronizes.

The scene contains a plane on which the character can move around and a spawner as mentioned in the `SpawnPlayer` sample.
Upon entering play mode, the character will appear in the game view, and the camera will follow them around.
To move the character, use the arrow keys. You can turn the character using the mouse, which will change the viewpoint. Notice that the character follows the camera view.

Inside the `Character` folder, you will find the prefab `ClientAnimatedCharacter`, which is the entity assigned to the `Spawner` in the subscene.
It is set up to handle character controllers like the `CharacterController` sample.
A `Ghost Authoring Inspection Component` is attached and has set `DontSerializeVariant` on the rotation component.
This is important because the rotation is not synchronized on the server, and the server's value would overwrite the rotation value set on the client-side.

The `Ghost Presentation Game Object Authoring` is used to spawn the `Terraformer` prefab as a client-side representation of the character.
The server-side representation is not set because no presentation object is required on the server-side.
During the baking step and when entering play mode, the presentation object is spawned and assigned an entity.
Netcode does this inside the `GhostPresentationGameObjectSystem` system in the package code.
Once the `Terraformer` is spawned, communication between the entity and game object worlds can occur from the `MonoBehaviour` attached to it.

The `Terraformer` prefab has the `Character Animation` component and an `Animator` component.
The `Animator` component has the `Animator Controller` containing the logic for when to play animation clips.
This controller has a list of parameters that are used in the `Character Animation` script to control the animation being played.

The `Character Animation` class is invoked from the system `UpdateAnimationStateSystem` to ensure that the input data is gathered before invoking the `UpdateAnimationState` method.
This method also returns an updated `LocalTransform` component used in the system to update the rotation on the character.

By combining the character controller data to know when to jump, run, etc., the state in the animation controller's state machine responds correctly to this information.
Additionally, when turning the viewport around by moving the mouse in the game view, the animation follows the camera's viewpoint.

## Notes
* In the following sample you will see a server side animation sample.
* In order to synchronize the rotation value as well one would need to extend the CharacterControllerPlayerInput found in the Character Controller sample. Here we would need to send the updated rotation value to update the server side as well. 

## Hack to fix editor problem

In the sample scene an additional scene was added called Hack. This contains a component Hack which references the Terraformer and store all animation clips from the Animator in a List.
A bug was discovered in the editor/player implementation of how serialization and deserialization of the baked entity references works.
During awake the references are not initialized in the expected order, leading to unassigned animation clip references in the standalone build.

Having this additional scene with reference to the animation clips ensures that the animation clip references are maintained and serialized correctly for the Animator.
When the fix for the runtime is published this hack will be unnecessary.

To make this hack a zero cost abstraction in terms of CPU cost, auto load has been disabled on the SubScene. This can be seen in the inspector for the Hack scene.  
