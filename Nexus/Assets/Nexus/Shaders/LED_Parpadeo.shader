// Shader de LEDs para Nexus (URP, sin iluminación).
// Todo el parpadeo se calcula en la GPU: no hace falta ningún script por LED,
// así cientos de LEDs no cuestan CPU (importante en Android y WebGL).
//
// - Si "Usar color de vértice" = 1, cada LED toma su color del color de vértice (RGB)
//   y su desfase de parpadeo del canal alfa. Así un solo mesh combinado contiene
//   LEDs verdes, azules y ámbar que parpadean a destiempo.
// - La posición del objeto en el mundo añade un desfase extra, para que dos racks
//   iguales no parpadeen sincronizados.
Shader "Nexus/LED Parpadeo"
{
    Properties
    {
        [HDR] _Color ("Color (si no se usa el color de vértice)", Color) = (0.2, 1.5, 4, 1)
        [Toggle] _UsarColorVertice ("Usar color de vértice", Float) = 1
        _Intensidad ("Intensidad (HDR, >1 genera bloom)", Float) = 3
        [Toggle] _Parpadeo ("Parpadear", Float) = 1
        _Velocidad ("Velocidad de parpadeo", Float) = 0.8
        _Encendido ("Fracción del ciclo encendido", Range(0, 1)) = 0.7
        _Minimo ("Brillo mínimo (apagado)", Range(0, 1)) = 0.06
        _Pulso ("Pulso suave (0 = no)", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Color;
            half _UsarColorVertice;
            half _Intensidad;
            half _Parpadeo;
            half _Velocidad;
            half _Encendido;
            half _Minimo;
            half _Pulso;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; half3 color : TEXCOORD0; half fog : TEXCOORD1; };

            float Hash(float2 p) { return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453); }

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);

                // Desfase: el del vértice (cada LED) + el de la posición del objeto (cada rack)
                float3 origen = TransformObjectToWorld(float3(0, 0, 0));
                float fase = v.color.a + Hash(origen.xz);
                half3 color = lerp(_Color.rgb, v.color.rgb * _Intensidad, _UsarColorVertice);

                // Ciclo encendido/apagado con velocidad algo distinta por LED
                float velocidad = _Velocidad * (0.6 + 0.8 * frac(fase * 7.31));
                half encendido = step(frac(_Time.y * velocidad + fase), _Encendido);
                // Destellos rápidos de "actividad de disco"
                encendido *= step(0.18, frac(sin(floor(_Time.y * 11.0 + fase * 53.0)) * 43758.5453));
                encendido = lerp(1.0, encendido, _Parpadeo);

                half pulso = 1.0 + _Pulso * 0.45 * sin(_Time.y * 3.0 + fase * 6.2831);
                o.color = color * max(encendido, _Minimo) * pulso;
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(MixFog(i.color, i.fog), 1);
            }
            ENDHLSL
        }

        // Profundidad (necesaria para Forward+ y efectos que usan el depth buffer)
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct Attributes { float4 positionOS : POSITION; };
            float4 vert(Attributes v) : SV_POSITION { return TransformObjectToHClip(v.positionOS.xyz); }
            half frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
    Fallback Off
}
