# Is the Entity Component System for you?

The Entity Component System is in preview. It is not recommended for production.

At the moment there are two good reasons to use it:

### You want to experiment

This is exciting new technology, and the promise of massive performance boosts is seducing. Try it out. Give us your feedback. We would love to talk to you on the forums.

### You are trying to build a game that can't be done without the Entity Component System

We'd love to know more about your game. Please, do feel free to post on the forum about your game, what you are trying to achieve, and what you think the Entity Component System offers that can't be achieved otherwise.


## Trying the Entity Component System

You've heard that ECS not only improves performance, but also helps you write cleaner, clearer, and more maintainable code. You'd like to see how it works for you in practice.

This is a fun scenario, because you get to write straightforward code from the beginning. There are a few things to keep in mind:

### You will probably want to use hybrid ECS at first

Right now, most of Unity's existing systems are only designed to be used with __GameObjects__. This means that making a "pure" ECS game requires that you write a lot of what you need yourself. To get started, it is often useful to have access to Unity's built-in physics, audio, and rendering systems. To do this, you will need GameObjects with __Colliders__, Rigidbody components, and (crucially) __GameObjectEntity__ scripts.

The important thing to remember is that this is totally fine, and Unity ECS was designed to work with both traditional GameObject/component and lightweight __Entity__/__ComponentData__ setups. You won't be able to use the job system with traditional GameObject code, but you'll probably discover lots of places where you are able to just use Entities.
