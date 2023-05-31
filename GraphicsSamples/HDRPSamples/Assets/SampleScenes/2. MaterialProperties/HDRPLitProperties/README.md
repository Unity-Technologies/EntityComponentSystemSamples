# HDRPLitProperties

This sample demonstrates material property overrides for different HDRP Lit material properties on Entities.

<img src="../../../../READMEimages/HDRPLitProperties.PNG" width="600">

## What does it show?

The scene contains spheres which use the HDRP Lit shader. The spheres are in a Subscene.

The material override authoring components attached to the MeshRenderers of the spheres override values for the spheres' color, smoothness, and metallic properties.

## How to use this sample scene?

1. In the Hierarchy, select the Subscene
2. In the Inspector, click Open
3. In the Hierarchy, select a sphere
4. In the Inspector, note that there are several HDRP Material Property Authoring components. If you want to override other HDRP material properties, you can add the other HDRP Material Property Authoring components

## More information

For more information about material property overrides, see the [documentation](https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.0/manual/material-overrides-code.html).