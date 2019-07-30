WORK IN PROGRESS!

this sample is going to be merged into ForEach

# HelloCube_1b_ForEachWithEntityChanges

This sample demonstrates a simple ECS System that uses queries to select the correct set of entities to update.  It then modifies these entities inside of the ForEach lambda function.

The example defines two tag components, named MovingCube_ForEachWithEntityChanges and MoveUp_ForEachWithEntityChanges. One query in the system selects all entities that have both a MoveUp_ForEachWithEntityChanges component and a Translation component. The ForEach lambda function associated with this query moves each selected entity upwards until it reaches a certain height. At that point, the function removes the MoveUp_ForEachWithEntityChanges component so the next time the system updates, the entity will not be selected and thus it will not be moved upwards any farther.

A second query selects all entities that have a Translation component, but do not have a MoveUp_ForEachWithEntityChanges component. The ForEach function associated with the second query moves the entity down to its starting location and adds a new MoveUp_ForEachWithEntityChanges component. Since the entity has a MoveUp_ForEachWithEntityChanges component once again, the next time the system updates, the entity is moved upward by the first ForEach function and skipped by the second query.

The MovingCube_ForEachWithEntityChanges is a tag component used to ensure that the system only works on components marked for this sample.  Both queries in the sample require a MovingCube_ForEachWithEntityChanges component.

## What does it show?

This sample demonstrates a simple ECS System that uses queries to select a set of entities to move upwards.  When they reach a certain height, the system removes a component and uses another query to respawn them at a lower height.  It also demonstrates the use of "tag" components to provide a means of selecting specific sets of entites with marker components to be processed.  Finally, this sample demonstrates how entities can be modified inside of a ForEach lambda function.

## Component Systems and Entities.ForEach

MovementSystem_ForEachWithEntityChanges is a ComponentSystem and uses an Entities.ForEach lambda function to iterate through the Entities. This example uses the WithAll and WithNone constraints to select specific sets of entities to work on.

Note: Component Systems using Entities.ForEach run on the main thread. To take advantage of multiple cores, you can use a JobComponentSystem (as shown in other HelloCube examples).  This also allows for changes to entities inside of the ForEach lambda function.
