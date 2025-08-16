Shader "UI/BottomTintFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color            ("Tint", Color) = (1,1,1,1)
        _OverlayColor     ("Overlay Color", Color) = (1,0,0,1)
        _Fill             ("Fill (0-1, bottom->up)", Range(0,1)) = 0
        _Softness         ("Edge Softness", Range(0,0.25)) = 0.05
        _BlendMode        ("BlendMode 0=Multiply 1=Add", Float) = 0

        [PerRendererData] _StencilComp ("Stencil Comparison", Float) = 8
        [PerRendererData] _Stencil ("Stencil ID", Float) = 0
        [PerRendererData] _StencilOp ("Stencil Operation", Float) = 0
        [PerRendererData] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [PerRendererData] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [PerRendererData] _ColorMask ("Color Mask", Float) = 15
        [PerRendererData] _ClipRect ("Clip Rect", Vector) = (0,0,0,0)
        [PerRendererData] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off Lighting Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t { float4 vertex:POSITION; float4 color:COLOR; float2 texcoord:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float4 color:COLOR; float2 worldPos:TEXCOORD1; };

            sampler2D _MainTex; float4 _MainTex_ST;
            fixed4 _Color, _OverlayColor;
            float _Fill, _Softness, _BlendMode;
            float4 _ClipRect; float _UseUIAlphaClip;

            v2f vert(appdata_t v){
                v2f o; o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord,_MainTex);
                o.color = v.color * _Color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
                return o;
            }

            fixed4 frag(v2f i):SV_Target{
                fixed4 baseCol = tex2D(_MainTex,i.uv) * i.color;
                #ifdef UNITY_UI_CLIP_RECT
                baseCol.a *= UnityGet2DClipping(i.worldPos,_ClipRect);
                #endif
                if(_UseUIAlphaClip>0.5 && baseCol.a<=0) discard;

                float edgeStart = saturate(_Fill - _Softness);
                float t = 1.0 - smoothstep(edgeStart, _Fill, i.uv.y); // ‰º¨ã

                fixed3 overlay = _OverlayColor.rgb;
                if(_BlendMode<0.5){
                    baseCol.rgb = lerp(baseCol.rgb, baseCol.rgb*overlay, t); // æZ
                }else{
                    baseCol.rgb = baseCol.rgb + overlay * t * _OverlayColor.a; // ‰ÁZ
                }
                return baseCol;
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}
