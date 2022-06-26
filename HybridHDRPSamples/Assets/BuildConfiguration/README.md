# HybridHDRPSamplesBuildSettings

To include subscenes in a Standalone Player, you must use the BuildConfiguration asset.

## Building a Standalone Player

1. Select HybridHDRPSamplesBuildSettings
2. In the Inspector, configure Scene List and Class Build Profile settings
3. Click Build and Run on top-right corner

NOTE: On MacOS, if you get errors such as:

The instruction `Unity.Burst.Intrinsics.X86.Sse.set1_ps(float a)` requires CPU feature `SSE` but the current block only supports `None` and the target CPU for this method is `ARM_AARCH64_ADVSIMD_AndLower`. Consider enclosing the usage of this instruction with an if test for the appropriate `IsXXXSupported` property.
The instruction `Unity.Burst.Intrinsics.X86.Sse2.srai_epi32(Unity.Burst.Intrinsics.v128 a, int imm8)` requires CPU feature `SSE2` but the current block only supports `None` and the target CPU for this method is `ARM_AARCH64_ADVSIMD_AndLower`. Consider enclosing the usage of this instruction with an if test for the appropriate `IsXXXSupported` property.

The solution is to change the File > BuildSettings > Architecture that set to "Intel 64-bit + Apple silicon" to "Intel 64-bit" only, and click "Build and Run" on the BuildConfiguration asset.
