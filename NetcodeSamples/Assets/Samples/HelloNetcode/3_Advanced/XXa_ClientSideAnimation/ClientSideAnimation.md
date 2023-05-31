# HelloNetcode Client Side Animation sample

## Requirements

The spawn player sample is used to trigger the auto spawning of the player when establishing connection.
The character controller is used to move and rotate the player around using the keyboard and mouse input.

* GoInGame
* SpawnPlayer
* CharacterController

## Sample description

This sample shows how to add animation to the spawned player. The animation is created using the built-in animation system known as [Mecanim](https://docs.unity3d.com/Manual/AnimationOverview.html).
The animation is running on the client side only and is not synchronizing to the server. We only synchronize the translation of the parent entity.

The scene comprises a plane on which the character can move around and a spawner as mentioned in the sample SpawnPlayer. You will see the character in the gameview when entering playmode. The camera will follow the character around.
To move the character you must use the arrow keys. You can also turn the character by pressing the mouse button and dragging to the sides to change the viewpoint. Notice the character following the camera view.

In the Character folder inside of this sample is the prefab ClientAnimatedCharacter. This is the entity assigned to the 'Spawner' in the subscene.
It is setup to handle character controllers as the CharacterController sample. Notice that a Ghost Authoring Inspection Component is attached and that it has set DontSerializeVariant on the rotation component.
This is important because of the rotation not being synchronized on the server. In the default case the server's value would overwrite the rotation value set on the client side.

We use the Ghost Presentation Game Object Authoring to spawn the Terraformer prefab as a client side representation of the character. The server side representation is not set as we do not need any presentation object on the server side.
When entering playmode and during the baking step, the presentation object will be spawned and assigned an entity. Netcode does this inside of the GhostPresentationGameObjectSystem system in the package code.
Once the Terraformer is spawned we can from the MonoBehaviour attached to it communicate between the entity and gameobject worlds.

The Terraformer prefab has the Character Animation component and an Animator component. The Animator component has the Animator Controller which contains the logic for when to play animation clips.
This controller has a list of parameters which we use in the Character Animation script to give us control over the animation being played.

By combining the character controller data to know when to jump, run, etc. the state in the animation controller's state machine responds correctly to this information.
In addition when turning the viewport around by left clicking in the game view and dragging around the animation follows the camera's viewpoint.

## Notes
* In the following sample you will see a server side animation sample.
* In order to synchronize the rotation value as well one would need to extend the CharacterControllerPlayerInput found in the Character Controller sample. Here we would need to send the updated rotation value to update the server side as well. 
