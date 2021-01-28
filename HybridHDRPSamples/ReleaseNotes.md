# HybridHDRPSamples Version 3

##
* Updated minimum Unity Editor version to `2020.2.3f1`.
* Updated HDRP version to `10.2.2`.

# HybridHDRPSamples Version 2

##
* Updated minimum Unity Editor version to `2020.2.0b15`.

# HybridHDRPSamples Version 1

## Changes
* Upgraded Project Version to 2020.2.0b12
* Upgraded HDRP version from 10.1.0 to 10.2.0 (materials are also automatically upgraded)
* Changed manifest.json registry from upm-dots-candidates to upm-candidates (this resolves an issue I got where 7.2.2-preview graphics test framework was breaking)
* Updated ShaderGraphOverride readme since 10.2 uses now Override Property Declaration checkbox + Shader Declaration (description same as URP)
* Updated GraphicsTest_001 scene to use Vector 1/2/3/4 overrides
* Updated project readme minimum used Unity version and HDRP version
* Added missing EntityCreationAPI readme screenshot
* Removed GraphicsTestCustom scripts form Lightmaps and Lightprobes scenes to avoid "serialization layout when loading" errors