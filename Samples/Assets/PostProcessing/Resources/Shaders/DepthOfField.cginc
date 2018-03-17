#ifndef __DEPTH_OF_FIELD__
#define __DEPTH_OF_FIELD__

#include "UnityCG.cginc"
#include "Common.cginc"

half4 Frag(VaryingsDefault i) : SV_Target
{
    return tex2D(_MainTex, i.uv);
}

#endif // __DEPTH_OF_FIELD__
