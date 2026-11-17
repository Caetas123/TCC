Shader "Luta/ContornoObjeto"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Cor", Color) = (1, 0.83, 0.08, 1)
        _Largura ("Largura em pixels", Float) = 8
        _Pulso ("Intensidade", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="False" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _Largura, _Pulso;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                // Dilata somente o alfa: a borda segue a silhueta, inclusive
                // as reentrâncias do teclado/alças, sem desenhar uma caixa.
                float centro = tex2D(_MainTex, i.uv).a;
                float borda = 0, halo = 0;
                [unroll] for (int n = 0; n < 16; n++)
                {
                    float angulo = n * 0.3926990817;
                    float2 passo = float2(cos(angulo), sin(angulo)) * _MainTex_TexelSize.xy * _Largura;
                    borda = max(borda, tex2D(_MainTex, i.uv + passo * 0.5).a);
                    borda = max(borda, tex2D(_MainTex, i.uv + passo).a);
                    halo = max(halo, tex2D(_MainTex, i.uv + passo * 2.2).a);
                }
                borda = smoothstep(0.12, 0.5, borda);
                halo = smoothstep(0.12, 0.5, halo);
                float fora = 1 - smoothstep(0.08, 0.4, centro);
                return fixed4(lerp(_Color.rgb, fixed3(1, 0.96, 0.55), borda * _Pulso * 0.4),
                    fora * max(borda * (0.75 + _Pulso * 0.25), halo * (0.12 + _Pulso * 0.15)));
            }
            ENDCG
        }
    }
}
