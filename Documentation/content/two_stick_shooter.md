# Two-stick shooter ECS tutorial

In this series of posts, we're going to make a simple game using Unity ECS and jobs as much as possible. 
The game type we picked for this was a simple two-stick shooter, something everyone can imagine building in a traditional way pretty easily.

## Scene setup
 
For this tutorial, we're going to use ECS as much as possible.
The scene we need for this tutorial is almost empty as there are very few
__GameObjects__ involved. The only things we use the Scene for are:

* A camera
* A light source
* Template objects that hold parameters we'll use to spawn ECS Entities
* A couple of UI objects to start the game and display health

The tutorial project is located in *Assets/ECS/TwoStickShooterPure*.

## Bootstrapping

How do you bootstrap your game when using ECS? After all, you need something to insert
those initial Entities into the system before anything can update.

One simple answer is to just run some code when the project starts playing. In this project,
there's a class __TwoStickBootstrap__ that comes with two methods. The first method initializes
early and creates the core __EntityManager__ we're going to use to interact with ECS.

Overall, here's what the bootstrapping code achieves:

* It creates an EntityManager; a key ECS abstraction we use to create and modify
  Entities and their components.

* It creates archetypes, which you can think of as blueprints for what components
  will be attached to an Entity later on when it is created. This step is optional,
  but avoids reallocating memory and moving objects later when they are spawned, because
  they will be created with the correct memory layout right away. 

* It pulls out some prototypes and settings from the Scene.

### Scene data

Pure ECS data isn't supported to a great degree in the editor yet, so we'll take two
approaches in the interim to configure our game:

1. For things like asset references, we'll create a couple of prototype GameObjects
   in the Scene, where we can add wrapped __IComponentData__ types. This is the approach
   we've taken to customize the appearance of the hero and the shots. Once we've finished
   the configuration, we can discard these prototype objects.
   
2. For "bags of settings", it's convenient to retain a traditional Unity component on an
   empty GameObject, because that allows you to tweak the values in Play Mode. The
   example project uses a component __TwoStickExampleSettings__ for this purpose that we
   put on an empty GameObject called __Settings__. This allows us to fetch the component
   and keep it around globally in our application, as well as to receive updates when values are
   changed.
  
### Archetypes

As this is a very small game, we can describe all the EntityArchetypes we need directly
in the bootstrap code. To make an archetype, you simply list all the __ComponentTypes__ that
you need to go on an instance of that archetype when it is created. 

Let's look at one archetype, __PlayerArchetype__, which is for creating, well, players:

```c#
PlayerArchetype = entityManager.CreateArchetype(
    typeof(Position2D), typeof(Heading2D), typeof(PlayerInput),
    typeof(Faction), typeof(Health), typeof(TransformMatrix));
```

The PlayerArchetype has the following ComponentTypes:

  * __Position2D__ and __Heading2D__ - These stock ECS components allow the player's avatar to be
     positioned and automatically rendered using built-in 2D->3D transformations.
  * __PlayerInput__ is a component we fill in every frame based on the player's [Input](https://docs.unity3d.com/ScriptReference/Input.html)
  * __Faction__ describes the "team" the player is on. It'll come in useful later when we need
    to have shots just hit the opposing team.
  * __Health__ simply contains a hit point counter.
  * Finally, I've added another stock component __TransformMatrix__ that is required as a
    storage endpoint for [4x4 matrices](https://docs.unity3d.com/ScriptReference/Matrix4x4.html)
    read by the __InstanceRenderer__ system that works with ECS.

You can think of the ECS player-controlled Entity as a combination of these components.
  
The other archetypes are set up similarly.

The next initialization method runs after the Scene has loaded, because it needs to access a blueprint object from the Scene:

### Extracting configuration from the Scene

Once the Scene has been loaded, our __InitializeWithScene__ method is going to be called. Here,
we pull out a few objects from the Scene, including a Settings object we can use to change
the ECS code while it's running.

### Starting a new game

To start a game, we have to put a player Entity into the World. This is accomplished with this code:

```c#
public static void NewGame()
{
    // Access the ECS EntityManager
    var entityManager = World.Active.GetOrCreateManager<EntityManager>();
    
    // Create an Entity based on the PlayerArchetype. It will get
    // default values for all the ComponentTypes we listed.
    Entity player = entityManager.CreateEntity(PlayerArchetype);
    
    // We can change a few components so it makes more sense like this:
    entityManager.SetComponentData(player, new Position2D { Value = new float2(0.0f, 0.0f) });
    entityManager.SetComponentData(player, new Heading2D  { Value = new float2(0.0f, 1.0f) });
    entityManager.SetComponentData(player, new Faction { Value = Faction.Player });
    entityManager.SetComponentData(player, new Health { Value = Settings.playerInitialHealth });
    
    // Finally we add a SharedComponentData that dictates the rendered look
    entityManager.AddSharedComponentData(player, PlayerLook);
}
```

## Systems!

We need a few data transformations to happen to render a frame: 

* We sample player Input (__PlayerInputSystem__).
* We move the player and allow them to shoot (__PlayerMoveSystem__).
* We need to sometimes spawn new enemies (__EnemySpawnSystem__).
* The enemies need to move (__EnemyMoveSystem__).
* The enemies need to shoot (__EnemyShootSystem__).
* We need to spawn new shots based on player or enemy action (__ShotSpawnSystem__).
* We need a way to clean up old shots when they timeout (__ShotDestroySystem__).
* We need to deal damage from shots (__ShotDamageSystem__).
* We need to cull any Entities that have no health left (__RemoveDeadSystem__).
* We need to push some data to the UI objects (__UpdatePlayerHUD__).

### Player input, movement, and shooting

It's worth calling out that multiplayer concerns are front and center in the ECS style
of writing code: we always have an array of players.

PlayerInputSystem is responsible for fetching input from the regular Unity Input API and
inserting that data into a PlayerInput component. It also counts down the **fire cooldown**,
that is, the waiting period before the player can fire again.

PlayerMoveSystem handles basic movement and shooting based on the input from the PlayerInputSystem. 
It is relatively straight forward, except for how it creates a shot when the
player has fired. Rather than spawning a shot directly, it creates a __ShotSpawnData__
component that instructs a different system to do that work later. This separation
of concerns solves several problems:

1. PlayerMoveSystem doesn't need to know what components need to go on an Entity to make a
   working shot.
2. ShotSpawnSystem (which spawns shots from both enemies and players) doesn't need
   to know all the reasons shots get fired.
3. We can spawn the shots into the world all at once at some later, well defined,
   point in time.

This setup achieves something similar to a delayed event in a traditional component
architecture.

### Enemy spawning, moving, and shooting

It would not be a challenging game without enemies shooting back at you, so naturally
there are a few systems dedicated to this.

Of the enemy systems, the most interesting one is the EnemySpawnSystem. It needs to keep
track of when to spawn an enemy, but we don't want to put that state in the system itself.
One of the design principles of ECS is that it shouldn't prevent you from recording
component state and playing it back later to reconstruct a Scene. Storing a bunch of
state variables that carry meaning from frame to frame will break that contract.

The EnemySpawnSystem instead stores its state in a singleton component, attached to a
singleton Entity. We create the Entity and the initial values for this component in
a setup function __EnemySpawner.SetupComponentData()__. Here we also initialize a random
seed and store that along with the rest of the data, so that games will predictably spawn
enemies in the same pattern every time, regardless of frame rate or if something fancy like
state replay is happening.

Inside the EnemySpawnSystem, due to **ref return** not being implemented yet, we have to take a copy
of our system's state from the singular __State__ array, and then when we've modified it for
the next frame, we store it back into the component.

This may look like a lot of boilerplate (and it is) but it's also kind interesting to think
about this in a different way. What if we renamed State to "Wave" and updated more than one
of them at a time, orchestrated by some other system? We would get multiple simultaneous
"Waves" spawning and updating in concert. ECS makes these sort of transformations much easier
and cleaner than if we had used global data attached to the system. 

One quirk is that we have to put off actually spawning an Entity until we've completed
the above step of storing back our component state, because touching the EntityManager will
immediately invalidate all injected arrays (including the one where our state is kept!). Our
solution to this is [command buffers](https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.html) 
(via __EntityCommandBuffer__ - but command buffers don't
yet support __ISharedComponentData__, which is needed here to set the rendered look.)

Enemies move automatically using the stock __MoveForward__ component, so that's taken
care of.

We need them to shoot however, and EnemyShootSystem does just that. It creates
entities with ShotSpawnData data on them that will be converted to shots later; together
with any player shots.

Finally we also need a way to get rid of enemies that go offscreen. __EnemyRemovalSystem__
goes through all enemy positions and kills offscreen enemies by setting their health to -1.

### Handling shots

ShotSpawnSystem deals with creating actual shots from the requests dropped into the ECS by
players and enemies. This is a simple straightforward affair that just loops over all 
ShotSpawnData and converts them into shots.

More interesting is ShotDamageSystem, which intersects bullets and targets and deals
damage. This uses 4 injected groups:

* Players
* Shots fired by players
* Enemies
* Shots fired by enemies

This way it can kick off two jobs:

* Players vs enemy shots
* Enemies vs player shots

It uses a very simplistic point against circle collision test.

We also need to get rid of shots that didn't hit anything and just fly off. When their time to
live goes to zero, we let ShotDestroySystem remove them.

### Final pieces

We need something that culls dead objects from the world, and RemoveDeadSystem does just
that.

Finally, we want to display some data about the player's health on the screen
and UpdatePlayerHUD accomplishes this task.

