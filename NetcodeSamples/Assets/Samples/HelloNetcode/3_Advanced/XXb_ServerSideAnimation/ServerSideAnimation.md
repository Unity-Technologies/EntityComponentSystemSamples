# HelloNetcode Server Side Animation sample

## Requirements

The spawn player sample is used to trigger the auto spawning of the player when establishing connection.
The character controller is used to move and rotate the player around using the keyboard and mouse input.
The character model from client side animation is reused in this sample.

* GoInGame
* SpawnPlayer
* CharacterController
* ClientSideAnimation

## Sample description

This sample shows how to add animation to the spawned player. The animation is created using the built-in animation system known as [Mecanim](https://docs.unity3d.com/Manual/AnimationOverview.html).
The animation is running on the server side and is replicated on each client.

The scene comprises a plane on which the character can move around and a spawner as mentioned in the sample SpawnPlayer. You will see the character in the gameview when entering playmode. The camera will follow the character around.
To move the character you must use the arrow keys. You can also turn the character by pressing the mouse button and dragging to the sides to change the viewpoint. Notice the character following the camera view.

The prefab named ServerAnimatedCharacter is the entity assigned to the 'Spawner' in the subscene.
It is setup to handle character controllers as the CharacterController sample.

We use the Ghost Presentation Game Object Authoring to spawn the Terraformer_Client prefab as a client side representation of the character. The server side representation is set to the Terraformer_Server prefab. If you open the server side prefab you will notice that the geometry is disabled and only the skeleton is left enabled.
The animation is applying the motion to the skeleton, which will then be replicated to the client's version of the model where the geometry is enabled.

When entering playmode and during the baking step, the presentation objects will be spawned and assigned an entity. You will see them as Terraformer_[Server/Client] (Clone) in the scene hierarchy view.

The Terraformer prefab make use the GhostAnimationController from the Netcode package. These require that we assign a GhostAnimationGraph asset to the field named Animation Graph Asset.
In this sample one of those is called StateSelector, four more are created containing logic for Jump, Run, Stand and InAir.

The StateSelector is responsible for deciding the current state in the animation logic. This is done by querying the character controller logic and deciding whether the character should be moving, jumping, etc.
