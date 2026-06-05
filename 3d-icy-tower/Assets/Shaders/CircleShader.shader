Shader "Custom/CircleMask"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Center  ("Center (viewport UV)", Vector) = (0.5, 0.5, 0, 0)
        _Radius  ("Radius", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float2    _Center;
            float     _Radius;

            fixed4 frag (v2f_img i) : SV_Target
            {
                // Correct for aspect ratio so the mask is a true circle
                float2 uv = i.uv;
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 delta = (uv - _Center) * float2(aspect, 1.0);
                float dist = length(delta);

                // Soft edge (2 px feather)
                float feather = 2.0 / _ScreenParams.y;
                float alpha   = 1.0 - smoothstep(_Radius - feather, _Radius + feather, dist);

                // Black overlay; punch a hole where alpha == 0
                return fixed4(0, 0, 0, alpha);
            }
            ENDCG
        }
    }
}