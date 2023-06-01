# Unity.Mathematics cheat sheet

*Most of the methods in Mathematics have many overloads for different combinations of types. For example, `math.abs()` takes vector arguments, not just scalars, e.g. `math.abs(new int3(5, -7, -1))` returns `new int3(5, 7, 1)`. This cheat sheet does not exhaustively demonstrate all of the overloads. Consult the [API reference](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/) for the full list.*

In this page:

* [Types](#types)
* [Vector creation and copying](#vector-creation-and-copying)
* [Matrix creation and copying](#matrix-creation-and-copying)
* [Vector and matrix operators](#vector-and-matrix-operators)
* [Arithmetic](#arithmetic)
* [Exponents and logarithms](#exponents-and-logarithms)
* [Rounding and signs](#rounding-and-signs)
* [Value checks](#value-checks)
* [Conversion](#conversion)
* [Interpolation and clamping](#interpolation-and-clamping)
* [Picking between two values](#picking-between-two-values)
* [Hashing](#hashing)
* [Bitwise and boolean operations](#bitwise-and-boolean-operations)
* [Trig, degrees, and radians](#trig-degrees-and-radians)
* [Vector geometry](#vector-geometry)
* [Cardinal direction vectors](#cardinal-direction-vectors)
* [Rotations and transforms](#rotations-and-transforms)
* [Generating random numbers](#generating-random-numbers)
* [Generating noise](#generating-noise)

<br>

## Types

|||
| ----- | ----------- |
|[`math`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.math.html)| Class containing many static mathematical constants and methods.|
|[`noise`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.noise.html)|Class containing static methods for generating noise.|
|[`quaternion`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.quaternion.html)|Struct representing a rotation.|
|[`Random`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.Random.html)|Struct for generating random numbers.|
|[`RigidTransform`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.RigidTransform.html)| Struct representing a transform matrix.| 

<br>

### Scalar types:

|||
| ----- | ----------- |
| [`half`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.half.html) | A 16-bit floating-point number. |

<br>

### Vector types:

||
| ----- | 
|[`bool2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.bool2.html), [`bool3`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.bool3.html), [`bool4`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.bool4.html) | 
|[`int2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.int2.html), [`int3`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.int3.html), [`int4`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.int4.html) |
|[`uint2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.uint2.html), [`uint3`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.uint3.html), [`uint4`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.uint4.html) |
|[`float2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.float2.html), [`float3`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.float3.html), [`float4`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.float4.html) |
|[`half2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.half2.html), [`half3`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.half3.html), [`half4`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.half4.html) |
|[`double2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.double2.html), [`double3`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.double3.html), [`double4`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.double4.html) |
  
<br>

### Matrix types:

||
| ---- |
|[`bool2x2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.bool2x2.html), [`bool2x3`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.bool2x3.html), [`bool2x4`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.bool2x4.html), [`bool3x2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.bool3x2.html), [`bool3x3`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.bool3x3.html), [`bool3x4`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.bool3x4.html), [`bool4x2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.bool4x2.html), [`bool4x3`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.bool4x3.html), [`bool4x4`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.bool4x4.html) |
|[`int2x2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.int2x2.html), [`int2x3`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.int2x3.html), [`int2x4`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.int2x4.html), [`int3x2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.int3x2.html), [`int3x3`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.int3x3.html), [`int3x4`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.int3x4.html), [`int4x2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.int4x2.html), [`int4x3`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.int4x3.html), [`int4x4`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.int4x4.html) |
|[`uint2x2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.uint2x2.html), [`uint2x3`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.uint2x3.html), [`uint2x4`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.uint2x4.html), [`uint3x2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.uint3x2.html), [`uint3x3`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.uint3x3.html), [`uint3x4`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.uint3x4.html), [`uint4x2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.uint4x2.html), [`uint4x3`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.uint4x3.html), [`uint4x4`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.uint4x4.html) |
|[`float2x2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.float2x2.html), [`float2x3`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.float2x3.html), [`float2x4`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.float2x4.html), [`float3x2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.float3x2.html), [`float3x3`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.float3x3.html), [`float3x4`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.float3x4.html), [`float4x2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.float4x2.html), [`float4x3`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.float4x3.html), [`float4x4`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.float4x4.html) |
|[`double2x2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.double2x2.html), [`double2x3`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.double2x3.html), [`double2x4`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.double2x4.html), [`double3x2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.double3x2.html), [`double3x3`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.double3x3.html), [`double3x4`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.double3x4.html), [`double4x2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.double4x2.html), [`double4x3`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.double4x3.html), [`double4x4`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.double4x4.html) |

<br>

## Vector creation and copying

[!code-no-using](../Projects/MarkdownSrc/Assets/Examples/Mathematics.cs#vector_creation)

<br>

## Matrix creation and copying

[!code-no-using](../Projects/MarkdownSrc/Assets/Examples/Mathematics.cs#matrix_creation)

<br>

## Vector and matrix operators

*The vector and matrix operators (`+`, `-`, `*`, `/`, `%`, `--`, `++`, `==`, `!=`, `<`, `>`, `<=`, `>=`) operate upon corresponding component pairs:*

[!code-no-using](../Projects/MarkdownSrc/Assets/Examples/Mathematics.cs#vector_matrix_operators)

*The integer vector and matrix types also have bitwise operators: `&`, `|`, `~`, `<<`, `>>`.*

<br>

## Arithmetic 

|||
| ----- | ----------- |
|[`math.fmod`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.fmod.html)| The floating point remainder of x/y.|
|[`math.mad`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.mad.html)| Componentwise (a * b + c) on three scalars or vectors. |
|[`math.modf`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.modf.html)| Modulus and fractional component.|
|[`math.csum`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.csum.html)| Horizontal sum of components of a vector.|
|[`math.rcp`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.rcp.html)| Reciprocal (1 divided by the value).|

<br>

## Exponents and logarithms

|||
| ----- | ----------- |
|[`math.ceillog2`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.ceillog2.html)| Ceiling of the base-2 logarithm. |
|[`math.ceilpow2`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.ceilpow2.html)| Smallest power of two greater than or equal to the input.|
|[`math.exp`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.exp.html)| The constant e raised to a power. |
|[`math.exp10`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.exp10.html)| The value 10 raised to a power. |
|[`math.exp2`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.exp2.html)| The value 2 raised to a power. |
|[`math.floorlog2`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.floorlog2.html)| Floor of the base-2 logarithm. |
|[`math.log`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.log.html)| Natural logarithm. |
|[`math.log10`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.log10.html)| Base-10 logarithm |
|[`math.log2`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.log2.html)| Base-2 logarithm. |
|[`math.pow`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.pow.html)| Raised to a power. |
|[`math.rsqrt`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.rsqrt.html)| Reciprocal of the square root. |
|[`math.sqrt`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.sqrt.html)| Square root. |

<br>

## Rounding and signs

|||
| ----- | ----------- |
|[`math.abs`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.abs.html)| Absolute value. |
|[`math.ceil`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.ceil.html)| Round up. |
|[`math.floor`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.floor.html)| Round down. |
|[`math.round`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.round.html)| Round to nearest. |
|[`math.sign`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.sign.html)| The sign of a value: +1, 0 , or -1 |

<br>

## Value checks

|||
| ----- | ----------- |
|[`math.isfinite`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.isfinite.html)| Is finite floating-point value? |
|[`math.isinf`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.isinf.html)| Is infinite floating-point value? |
|[`math.isnan`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.isnan.html)| Is NaN? |
|[`math.ispow2`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.ispow2.html)| Is power of 2? |

<br>

## Conversion

|||
| ----- | ----------- |
|[`math.asdouble`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.asdouble.html)| Reinterpret the bits of a 64-bit integer as a double. |
|[`math.asfloat`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.asfloat.html)| Reinterpret the bits of a 32-bit integer as a float. |
|[`math.asint`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.asint.html)| Reinterpret the bits of a float or uint as an int. |
|[`math.aslong`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.aslong.html)| Reinterpret the bits of a double or 64-bit integer as a long. |
|[`math.asulong`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.asulong.html)| Reinterpret the bits of a double or 64-bit integer as a ulong.  |
|[`math.asuint`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.asuint.html)| Reinterpret the bits of a float or uint as a uint. |
|[`math.f16tof32`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.f16tof32.html)| The floating point representation of a half-precision floating-point value. |
|[`math.f32tof16`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.f32tof16.html)| Nearest half-precision floating-point representation of a floating-point  value. |
|[`math.frac`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.frac.html)| Fractional part of a floating-point value. |
|[`math.trunc`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.trunc.html)| Integral part of a floating-point value (rounded towards zero). |

<br>

## Interpolation and clamping

|||
| ----- | ----------- |
|[`math.clamp`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.clamp.html)| Clamp a value into an interval. |
|[`math.lerp`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.lerp.html)| Linear interpolation between two values. |
|[`math.nlerp`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.nlerp.html)| Normalized linear interpolation between two quaternions. |
|[`math.remap`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.remap.html)| Non-clamping, linear remapping of a value from a source range to a  destination range.|
|[`math.saturate`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.saturate.html)| Clamp a value into the interval [0, 1]. |
|[`math.slerp`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.slerp.html)| Spherical interpolation between two quaternions. |
|[`math.smoothstep`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.smoothstep.html)| Smooth Hermite interpolation between 0.0f and 1.0f. |
|[`math.step`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.step.html)| Return 1.0f if x >= y, otherwise returns 0.0f. |
|[`math.unlerp`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.unlerp.html)| Normalize a value into a range. (Opposite of lerp.) |

<br>

## Picking between two values

|||
| ----- | ----------- |
|[`math.shuffle`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.shuffle.html)| Pick one or more specific components from two vectors. |
|[`math.select`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.select.html)| Pick between two values based on a boolean. Similar to  the ternary operator, but you can also pick specific components of two vectors. |
|[`math.cmax`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.cmax.html)| Largest component of a vector. |
|[`math.cmin`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.cmin.html)| Smallest component of a vector. |
|[`math.max`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.max.html)| Largest of two values. |
|[`math.min`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.min.html)| Smallest of two values. |

<br>

## Hashing

*See the Collections package for more hashing options.*

|||
| ----- | ----------- |
|[`math.hash`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.hash.html)| Hash of a value. |
|[`math.hashwide`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.hashwide.html)| When hashing multiple values together, it's often more efficient to separately pass them to hashwide(), combine the results, then hash() the combination. |

<br>

## Bitwise and boolean operations

|||
| ----- | ----------- |
|[`math.all`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.all.html)| True if all booleans are true.|
|[`math.any`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.any.html)| True if any boolean is true.|
|[`math.bitmask`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.bitmask.html)| Bitmask of a bool4: one bit per component (4 bits in total) in LSB order (lower to higher). |
|[`math.countbits`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.countbits.html)| Count of the 1-bits.|
|[`math.compress`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.compress.html)|  Packs mask-enabled components of a vector to the left.|
|[`math.lzcnt`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.lzcnt.html)|  Leading zero count of the bits.|
|[`math.reversebits`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.reversebits.html)|  Reverse the order of the bits.|
|[`math.rol`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.rol.html)|  Rotate bits left.|
|[`math.ror`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.ror.html)|  Rotate bits right.|
|[`math.tzcnt`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.tzcnt.html)| Trailing zero count of the bits.|

<br>

## Trig, degrees, and radians

|||
| ----- | ----------- |
|[`math.acos`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.acos.html)|Arccosine.|
|[`math.asin`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.asin.html)|Arcsine.|
|[`math.atan`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.atan.html)|Arctangent
|[`math.atan2`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.atan2.html)|Arctangent for 2 arguments. |
|[`math.cos`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.cos.html)|Cosine.|
|[`math.cosh`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.cosh.html)|Hyperbolic cosine.|
|[`math.math.degrees`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.degrees.html)| Degrees from radians.|
|[`math.radians`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.radians.html)| Radians from degrees.|
|[`math.sin`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.sin.html)|Sine.|
|[`math.sincos`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.sincos.html)|Sinecosine.|
|[`math.sinh`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.sinh.html)|Hyberbolic sine.|
|[`math.tan`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.tan.html)|Tangent.|
|[`math.tanh`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.tanh.html)|Hyberbolic tangent.|

<br>

## Vector geometry

|||
| ----- | ----------- |
|[`math.cross`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.cross.html)| Cross product. |
|[`math.distance`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.distance.html)| Distance between two points (1 to 4 dimensions). |
|[`math.distancesq`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.distancesq.html)| Square root of distance between two points (1 to 4 dimensions). |
|[`math.dot`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.dot.html)| Dot product. |
|[`math.faceforward`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.faceforward.html)| Flips a vector if two other vectors point in the same direction (i.e. the angle between them is less than or equal to 90 degrees). |
|[`math.length`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.length.html)| Distance of a point from the origin. |
|[`math.lengthsq`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.lengthsq.html)| Square root of the distance of a point from the origin. |
|[`math.normalize`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.normalize.html)| Normalized vector. |
|[`math.normalizesafe`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.normalizesafe.html)| Normalized vector. Returns the default value if the normalized  vector is not finite. |
|[`math.project`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.project.html)| Project a vector on to another. |
|[`math.projectsafe`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.projectsafe.html)| Project a vector on to another. Returns the default value if the projected vector is not finite. |
|[`math.reflect`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.reflect.html)| Reflection of an incident vector and a normal vector. |
|[`math.refract`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.refract.html)| Refraction of an incident vector and a normal vector. |
|[`math.transform`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.transform.html)| Transform a 3-dimensional vector with a 4x4 matrix. |

<br>

## Cardinal direction vectors

|||
| ----- | ----------- |
|[`math.forward`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.forward.html)| The forward axis in Unity coordinates. |
|[`math.back`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.back.html)| The back axis in Unity coordinates. |
|[`math.up`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.up.html)| The up axis in Unity coordinates. |
|[`math.down`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.down.html)| The down axis in Unity coordinates. |
|[`math.left`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.left.html)| The left axis in Unity coordinates. |
|[`math.right`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.right.html)| The right axis in Unity coordinates. |

<br>

## Matrix ops

|||
| ----- | ----------- |
|[`math.determinant`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.determinant.html)| Determinant of a matrix.  |
|[`math.fastinverse`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.fastinverse.html)| Fast matrix inverse for rigid transforms (orthonormal basis and  translation). |
|[`math.inverse`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.inverse.html)| Inverse of a matrix or quaternion.  |
|[`math.mul`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.mul.html)| Matrix multiplication.|
|[`math.transpose`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.transpose.html)| Transpose of a matrix.  |
|[`math.unitlog`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.unitlog.html)| Natural logarithm of a unit length quaternion. |

<br>

## Rotations and transforms

|||
| ----- | ----------- |
|[`math.conjugate`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.conjugate.html)| Conjugate a quaternion. (Flips the signs of x, y, and z but not w.) |
|[`math.orthonormalize`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.orthonormalize.html)| Orthonormalize a float3x3 matrix. |
|[`math.rotate`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.rotate.html)| Rotate a vector by a unit quaternion. |
|[`math.unitexp`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.math.unitexp.html)| Natural exponent of a quaternion. (Assumes w is zero.) |
|[`quaternion.AxisAngle`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.quaternion.AxisAngle.html)| Quaternion representation of an axis-angle rotation. |
|[`quaternion.Euler`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.quaternion.Euler.html)| Quaternion representation of an Euler angle rotation (axis order specified by argument). |
|[`quaternion.EulerXYZ`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.quaternion.EulerXYZ.html)| Quaternion representation of an Euler angle rotation (axis order XYZ). |
|[`quaternion.EulerXZY`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.quaternion.EulerXZY.html)| Quaternion representation of an Euler angle rotation (axis order XZY). |
|[`quaternion.EulerYXZ`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.quaternion.EulerYXZ.html)| Quaternion representation of an Euler angle rotation (axis order YXZ). |
|[`quaternion.EulerYZX`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.quaternion.EulerYZX.html)| Quaternion representation of an Euler angle rotation (axis order YZX). |
|[`quaternion.EulerZXY`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.quaternion.EulerZXY.html)| Quaternion representation of an Euler angle rotation (axis order ZXY). |
|[`quaternion.EulerZYX`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.quaternion.EulerZYX.html)| Quaternion representation of an Euler angle rotation (axis order ZYX). |
|[`quaternion.LookRotation`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.quaternion.LookRotation.html)| Quaternion representing a rotation derived from a unit-length forward vector and a unit-length upwards vector. |
|[`quaternion.LookRotationSafe`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.quaternion.LookRotationSafe.html)| Quaternion representing a rotation derived from a forward vector and an upwards vector. |
|[`quaternion.RotateX`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.quaternion.RotateX.html)| Quaternion representation of a rotation around the X axis. |
|[`quaternion.RotateY`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.quaternion.RotateY.html)| Quaternion representation of a rotation around the Y axis. |
|[`quaternion.RotateZ`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.quaternion.RotateZ.html)| Quaternion representation of a rotation around the Z axis. |
|[`float4x4.LookAt`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.float4x4.LookAt.html)| View matrix derived from an eye position, a target point, and a uni-length upwards vector. |
|[`float4x4.Ortho`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.float4x4.Ortho.html)| Orthographic projection matrix |
|[`float4x4.OrthoOffCenter`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.float4x4.OrthoOffCenter.html)| Off-center orthographic projection matrix. |
|[`float4x4.PerspectiveFov`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.float4x4.PerspectiveFov.html)| Perspective projection matrix based on field of view. |
|[`float4x4.PerspectiveOffCenter`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.float4x4.PerspectiveOffCenter.html)| Off-center perspective projection matrix |
|[`float4x4.Scale`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.float4x4.Scale.html)| Matrix representing a scale transform. | 
|[`float4x4.Translate`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.float4x4.Translate.html)| Matrix representing a translation transform. | 
|[`float4x4.TRS`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.float4x4.TRS.html)| Matrix representing a combined translation, rotation, and scale transform. |
|[`float3x3.Scale`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.float3x3.Scale.html)| Matrix representing a scale transform. | 
|[`RigidTransform.Translate`](https://docs.unity3d.com/Packages/com.unity.mathematics@1.2/api/Unity.Mathematics.RigidTransform.Translate.html)| Matrix representing a translation transform. |

*`RigidTransform`, `float3x3`, and `float4x4` also have most of the same rotation methods as `quaternion`.*

<br>

## Generating random numbers

[!code-no-using](../Projects/MarkdownSrc/Assets/Examples/Mathematics.cs#random)

<br>

## Generating noise

|||
| ----- | ----------- |
| [`noise.cellular(float2)`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.noise.cellular.html) | 2D Cellular noise ("Worley noise") with standard 3x3 search window for good feature point values. |
| [`noise.cellular(float3)`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.noise.cellular.html) | 3D Cellular noise ("Worley noise") with 3x3x3 search region for good F2 everywhere, but a lot slower than the 2x2x2 version. |
| [`noise.cellular2x2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.noise.cellular2x2.html) | 2D Cellular noise ("Worley noise") with a 2x2 search window.  |
| [`noise.cellular2x2x2`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.noise.cellular2x2x2.html) | 3D Cellular noise ("Worley noise") with a 2x2x2 search window. |
| [`noise.cnoise`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.noise.cnoise.html) | Classic Perlin noise. |
| [`noise.pnoise`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.noise.pnoise.html) | Classic Perlin noise, periodic variant.  |
| [`noise.psrdoise`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.noise.psrdoise.html) | 2-D tiling simplex noise with fixed or rotating gradients and analytical derivative. |
| [`noise.psrnoise`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.noise.psrnoise.html) | 2-D tiling simplex noise with fixed or rotating gradients, but without the analytical derivative.  |
| [`noise.snoise`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.noise.snoise.html) | Simplex noise. |
| [`noise.srdnoise`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.noise.srdnoise.html) | 2-D non-tiling simplex noise with fixed or rotating gradients and analytical derivative. |
| [`noise.srnoise`](https://docs.unity3d.com/Packages/com.unity.mathematics@latest/index.html?subfolder=/api/Unity.Mathematics.noise.srnoise.html) | 2-D non-tiling simplex noise with fixed or rotating gradients, without the analytical derivative. |
