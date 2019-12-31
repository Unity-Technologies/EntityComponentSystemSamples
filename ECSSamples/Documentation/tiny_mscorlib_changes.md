# How to modify the Tiny mscorlib source code

The code for mscorlib used with Tiny lives in the [IL2CPP
repo](https://github.cds.internal.unity3d.com/unity/il2cpp/).

To modify it, first clone the full IL2CPP repo locally. Then modify the code in the
Il2CppCustomLocation.bee.cs file (part of the Bee package) to point to the location
of the locally cloned IL2CPP repo.

Next, modify the mscorlib code in that IL2CPP clone, and run `perl build.pl` from
the IL2CPP root directory. Your changes will be used in the DOTS/Tiny clone now!

To commit changes to the mscorlib code, open a PR to the IL2CPP repo. Once that PR
lands, the IL2CPP package will need to be updated in the DOTS repo.
