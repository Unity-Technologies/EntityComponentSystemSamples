# State Machine Guard AI

For reference, the latest Entities package documentation can be found [here](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html).

## Sample Overview
This sample demonstrates how a state machine for AI agents can be implemented using ECS. The principles are illustrated using a prototypical stealth game: You move your character through a 2D world  (using the WASD keys) and try to avoid being seen by guards that patrol between waypoints. The guards' AI is essentially a state machine.
This sample consists of the scene `StateMachineAI.unity` which includes
 - a Player game object
 - a Guard game object
 - two Waypoint game objects

The Enemy's `GuardAuthoring` component contains a list of waypoints. Both the Player and the Guard have a `ConvertToEntity` component attached which converts their data into the appropriate component data needed for ECS when entering Playmode.

## Problem Statement
We need a guard AI for a simple stealth game. The guard patrols between waypoints, waits at the waypoint once reached, and chases the player if seen. If the player gets too far away from the guard, the guard stops chasing the player and returns to patrolling.

For this game, we expect one player and 3-10 guards on a single map. Every guard will have between 1-8 waypoints. The waypoints will be hand-authored in the Editor. Player and Guard parameters (such as movement speed, idle time, etc) must also be authored in the Editor.

## Our Solution:
From the requirements, we identified that we had three distinct states of the guard: Patrolling, Chasing, and Idling.

**Patrolling:** The guard needs to know where it is going and which waypoint it’s currently moving towards. For this we need a list of all of the waypoints, which we decided to store as a DynamicBuffer (**WaypointPosition**) on the guard entity. When they reach their destination target, the guard goes Idle (**CheckReachedWaypointSystem**). The **UpdateIdleTimerSystem** is where the guard will transition from Idling to Patrolling.

**Idling:** The guard needs to know how long it has been idle (**IdleTimer**) and how long it’s supposed to be idle (**CooldownTime**). Every frame we need to update that timer based on the time that has passed (**UpdateIdleTimerSystem**). Then when it’s finished, the guard returns to Patrolling, selecting the next waypoint (**NextWaypointIndex**). The **CheckReachedWaypointSystem** is where the guard will transition from Patrolling to Idling and the **LookForPlayerSystem** is where the guard can transition from Chasing to Idling.

**Chasing:** The guard needs to know the Player’s position. To determine they are in the Chasing state, we need some information about what the guard can see. We chose a sight distance and view angle (**VisionCone**) to simplify the math, as opposed to a Raycast which would be more expensive for our simple operation. The **LookForPlayerSystem** is where the gaurd will transition in and out of the Chasing state.

Since Patrolling and Chase both need to move the guard towards its target (either a waypoint or the Player respectively), that’s effectively the same data transformation (**MoveTowardsTargetSystem**). Therefore, the guard just needs to store the position they are moving towards (**TargetPosition**) and how quickly they move (**MovementSpeed**). In order to tell if the guard is in the Patrol state or the Chase state, we use a tag (**IsChasingTag**) to differentiate the two.

In all states, the guard needs to check to see if the Player is in range (**LookForPlayerSystem**) and update its state accordingly.

### Guard Data
#### Persistent Data
* CooldownTime
* VisionCone
* MovementSpeed
* DynamicBuffer \<WaypointPosition\>
* NextWaypointIndex
* Translation
* Rotation
#### Chase or Patrol State
* TargetPosition
#### Idle State Only
* IdleTimer
#### Chase State Only
* IsChasingTag

### Player Data
#### Persistent Data
* PlayerTag
* MovementSpeed
* UserInputData

### MoveTowardTargetSystem
* MoveTowardTarget Entities.ForEach
#### Inputs
* Translation
* Rotation
* TargetPosition
* MovementSpeed
#### Outputs
* Translation
* Rotation

### UpdateIdleTimerSystem
* UpdateTimer Entities.ForEach
#### Inputs
* IdleTimer
* NextWaypointIndex
* CooldownTime
#### Outputs
* IdleTimer
* NextWaypointIndex
* Add TargetPosition, Remove IdleTimer if CooldownTime is reached

### CheckReachedWaypointSystem
* CheckReachedWaypoint Entities.ForEach
#### Inputs
* Translation
* TargetPosition
#### Outputs
* Add IdleTimer, Remove TargetPosition if target is reached

### LookForPlayerSystem
* LookForPlayer Entities.ForEach
#### Inputs
* Translation
* Rotation
* VisionCone
* ComponentDataFromEntity \<TargetPosition\>
* ComponentDataFromEntity \<IsChasingTag\>
* ComponentDataFromEntity \<IdleTimer\>
#### Outputs
* ComponentDataFromEntity <TargetPosition> update TargetPosition if the guard is in Chase state
* Add IsChasingTag if Player is in range and guard is not already in Chase state
* Remove IdleTimer if Player is in range and guard was in Idle state
* Remove IsChasingTag, Remove TargetPosition, Add IdleTimer if guard was Chasing and can no longer see the Player

### MovePlayerSystem
* MovePlayer Entities.ForEach
#### Inputs
* Translation
* UserInputData
* MovementSpeed
#### Outputs
* Translation

## Problem Considerations/Tradeoffs
### Designing the state machine
* First, we looked at a few facts about the problem:
  * Guards spend most of their time in the Patrol state or Idle state
  * The frequency of state changes is authoring data dependent, but typically rather low (it will typically be multiple seconds in between state changes for each guard).
* Due to these factors, we elected to use changing archetypes and specific EntityQueries to store and process the guard entity data
* Because our systems only operate on entities which have the required data types, ECS handles the sorting of these entities such that entities in the same state are stored together (in chunks). This makes processing of all of the entities in a particular state more cache-efficient.
  * Example: the UpdateIdleTimerSystem queries only for entities that have an IdleTimer. We don’t have to process and skip entities that are chasing or patrolling because they will be in different chunks than idle guard entities.
* Low state change frequency keeps the amortized cost of changing archetypes (which requires copying component data from chunk to chunk) rather low.
### Other solutions we considered:
* Say we created a SharedComponent with an enum to represent the three states. When we change the state of a guard by updating the SharedComponent value, that guard entity moves to a different chunk. Using a shared component in this case does not provide any advantages over the per-archetype solution (both require moving chunks). The Entities.ForEach would need to use a SharedComponent filter so that all of the states weren’t running for all of the guards at a time. Since querying with filtering is more expensive than separating chunks by archetype, this would result in a performance regression.
* We could also keep that enum value inside of a normal IComponentData and never change the archetype (i.e. all guards would have the necessary data for every state). Thus, all guards would be in the same chunk and no moving operations need to be performed. Each system would check the state for every entity and skip those that are not in the state(s) that system processes. Performing this conditional logic every frame when the state changes at most once every few seconds is a lot of unnecessary work for the CPU. Skipping entities in other states also results in poor cache performance, since cache lines still need to load the data for the entities around them.

## Suggestions for Modifications and Learning
### Changes to existing data and the scene
* Add additional waypoints for the guard in the scene
* Add more guards with different waypoints in the scene
* Create guards with different movement speed values, vision cone sizes, and idle cooldowns
* Modify the player’s movement speed
### Expanding the problem, more things to consider outside of this sample
* What if we wanted walls in the level to block a guard’s view of the player?
* What if we wanted the guards to avoid each other?
* What if we wanted thousands of guards all looking for the player?
  * If there is a large area, do all the guards need to check if they can see the player? What if they are nowhere near the player? What kind of acceleration structure could you add to make this more efficient?
* Finish the game!
