WORK IN PROGRESS!

this sample is going to be merged into ForEach


# HelloCube_07_ForEach

This sample demonstrates a simple ECS System that uses fluent queries to select the correct set of entities to update.

The example defines two tag component, named MovingCube and MoveUp. One fluent query in the system selects all entities that have both a MoveUp component and a Translation component. The ForEach lambda function associated with this query moves each selected entity upwards until it reaches a certain height. At that point, the function removes the MoveUp component so the next time the system updates, the entity will not be selected and thus it will not be moved upwards any farther.

A second fluid query selects all entities that have a Translation component, but do not have a MoveUp component. The ForEach function associated with the second query moves the entity down to its starting location and adds a new MoveUp component. Since the entity has a MoveUp component once again, the next time the system updates, the entity is moved upward by the first ForEach function and skipped by the second query.

The MovingCube is another tag component that ensures that the system only works on components marked for this sample.

## What does it show?

This sample demonstrates a simple ECS System that uses fluent queries to select a set of entities to move upwards.  When they reach a certain height, the system removes a component and uses another query to respawn them at a lower height.  It also demonstrates the use of "tag" components to provide a means of selecting specific sets of entites with marker components to be processed.

## Component Systems and Entities.ForEach

MovementSystem is a ComponentSystem and uses an Entities.ForEach delegate to iterate through the Entities. This example uses the WithAll and WithNone constraints to select specific sets of entities to work on.

Note: Component Systems using Entities.ForEach run on the main thread. To take advantage of multiple cores, you can use a JobComponentSystem (as shown in other HelloCube examples).
