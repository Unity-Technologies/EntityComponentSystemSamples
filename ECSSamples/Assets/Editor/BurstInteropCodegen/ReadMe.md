# Burst Interop Generator
This is the Burst interop generator that is used by the entities package to automatically generate code for (1) calling into Mono code from Burst or (2) for generating code that is callable from Mono and Burst (and always get the benefits of Burst compilation). It generates new entry-points for existing methods. As an example for (1), it takes a non Burst-compatible method `MyType._DoManagedThingsThatCannotHappenInBurst(void* thing)` and generates a new entry point `MyType.DoManagedThingsThatCannotHappenInBurst(void* thing)` that can be called from Burst (- the method itself will still run on Mono, but at least it is callable from within Burst).

## Running the generator
To run the generator, execute the menu item `DOTS/Regenerate Burst Interop`. This will scan the project for methods with `BurstInterop` attributes and perform the necessary codegen. There is a `tt` file in this folder, but you DO NOT have to run it to run the Burst Interop Generator. The `tt` is used to build the generator itself, not to execute it.

Some notes:
 - Any type that contains methods that should be picked up by codegen needs to be marked with `[GenerateBurstMonoInterop(<fileName>)]`
 - Any method that should be picked up during codegen needs to be marked with `[BurstMonoInteropMethod]`
 - Any method that has `[BurstMonoInteropMethod]` and `[BurstDiscard]` is a method that will be run in Mono and requires codegen to generate a callsite that can be called from Burst
 - Any method to be included needs to have Burst-compatible arguments and have a name that starts with `_` (that leading `_` will be removed for the generated entrypoint.)

## Making changes to the interop generator
The interop generator consists of two parts: An explicitly written one, and one that is generated. Yes, you read that right, the interop generator is partially generated. The generated code lives in `BurstInteropCodeGenerator.gen.tt` and contains the logic for writing out the code that the generator generates. `BurstInteropCodeGenerator.cs` contains the code for scanning what methods need to be generated. If you make a change to the `tt` file, make sure to regenerate it.
