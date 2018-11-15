# Status of Unity's Data-Oriented Tech Stack

## Entity iteration

- We have implemented various approaches (foreach vs arrays, injection vs API). Right now we expose all possible ways of doing it, so that users can give us feedback on which one they like by actually trying them. Later on we will decide on the best way and delete all others.

## Job API using ECS

- We believe we can make it significantly simpler. The next thing to try out is Async / Await and see if there are some nice patterns that are both fast & simple.

## Entities vs. GameObjects

Our goal is to be able to make entities editable just like GameObjects are. Scenes are either full of Entities or full of GameObjects. Right now we have no tooling for editing Entities without GameObjects. So in the future we want to:

- Display & edit Entities in Hierarchy window and Inspector window.
- Save Scene / Open Scene / Prefabs for Entities.

