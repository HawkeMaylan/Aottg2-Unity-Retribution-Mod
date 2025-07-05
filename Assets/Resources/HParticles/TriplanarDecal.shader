Shader "Custom/TriplanarDecal"
{
    Properties
    {
        _MainTex ("Decal Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Scale ("Projection Scale", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        CGPROGRAM
        #pragma surface surf Lambert alpha:fade

        sampler2D _MainTex;
        float4 _Color;
        float _Scale;

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        void surf(Input IN, inout SurfaceOutput o)
        {
            float3 wpos = IN.worldPos * _Scale;
            float3 blend = abs(normalize(IN.worldNormal));
            blend = pow(blend, 4.0);
            blend /= dot(blend, float3(1.0, 1.0, 1.0));

            float4 x = tex2D(_MainTex, wpos.yz);
            float4 y = tex2D(_MainTex, wpos.xz);
            float4 z = tex2D(_MainTex, wpos.xy);

            float4 tex = x * blend.x + y * blend.y + z * blend.z;
            o.Albedo = tex.rgb * _Color.rgb;
            o.Alpha = tex.a * _Color.a;
        }
        ENDCG
    }
    FallBack Off
}
