Shader "Custom/IsometricHeight"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BrightnessPerUnit ("Z 1당 밝기 변화량", Float) = 0.05
        _MinBrightness ("최소 밝기", Range(0,1)) = 0.3
        _MaxBrightness ("최대 밝기", Range(0,1)) = 1.0
        _OutlineColor ("외곽선 색상", Color) = (0,0,0,1)
        _OutlineThickness ("외곽선 두께", Float) = 1.0
    }
    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float worldZ : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;  // 텍스처 픽셀 크기 (자동 제공)
            float _BrightnessPerUnit;
            float _MinBrightness;
            float _MaxBrightness;
            fixed4 _OutlineColor;
            float _OutlineThickness;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldZ = worldPos.z;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;

                // ── 외곽선 ──────────────────────────────────────
                float2 offset = _MainTex_TexelSize.xy * _OutlineThickness;

                // 상하좌우 4방향 이웃 픽셀의 알파 샘플링
                float alphaUp    = tex2D(_MainTex, i.uv + float2(0,  offset.y)).a;
                float alphaDown  = tex2D(_MainTex, i.uv + float2(0, -offset.y)).a;
                float alphaLeft  = tex2D(_MainTex, i.uv + float2(-offset.x, 0)).a;
                float alphaRight = tex2D(_MainTex, i.uv + float2( offset.x, 0)).a;

                // 자신은 투명 + 이웃 중 불투명이 있으면 → 외곽선 픽셀
                float isOutline = (1 - col.a) *
                    saturate(alphaUp + alphaDown + alphaLeft + alphaRight);

                // ── 밝기 ────────────────────────────────────────
                float brightness = clamp(
                    _MaxBrightness - i.worldZ * _BrightnessPerUnit,
                    _MinBrightness,
                    _MaxBrightness
                );
                col.rgb *= brightness;

                // ── 합성 ────────────────────────────────────────
                col = lerp(col, _OutlineColor, isOutline);
                col.a = max(col.a, isOutline * _OutlineColor.a);

                return col;
            }
            ENDCG
        }
    }
}