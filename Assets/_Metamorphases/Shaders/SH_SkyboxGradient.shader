// Fondo "infinito" de la simulación: blanco arriba y en el horizonte, vacío oscuro abajo.
// Uso: crea un material con este shader y ponlo en Window > Rendering > Lighting > Environment > Skybox Material.
// (Los skybox no se pueden hacer bien con Shader Graph en URP; por eso este va en código.)
// Se tiñe con el color del modo activo usando los mismos valores globales que SG_MonoWorld
// (_MM_ModeColor y _MM_ColorAmount, que envía WorldColorController).
Shader "Metamorphases/SkyboxGradient"
{
    Properties
    {
        _TopColor ("Arriba", Color) = (1, 1, 1, 1)
        _HorizonColor ("Horizonte", Color) = (0.94, 0.94, 0.94, 1)
        _BottomColor ("Abajo (vacío)", Color) = (0.04, 0.04, 0.05, 1)
        _HorizonHeight ("Altura del horizonte", Range(-0.5, 0.5)) = -0.05
        _BlendUp ("Suavidad hacia arriba", Range(0.01, 1)) = 0.4
        _BlendDown ("Suavidad hacia abajo", Range(0.01, 1)) = 0.25
        _TintStrength ("Fuerza del tinte (cielo)", Range(0, 1)) = 0.6
        _VoidTintStrength ("Fuerza del tinte (vacío)", Range(0, 1)) = 0.3
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            half4 _TopColor, _HorizonColor, _BottomColor;
            float _HorizonHeight, _BlendUp, _BlendDown, _TintStrength, _VoidTintStrength;
            // Globales (NO van en Properties, para que el material no las tape)
            half4 _MM_ModeColor;
            float _MM_ColorAmount;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert (Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.dir = i.positionOS.xyz;   // en un skybox, la posición = la dirección de la mirada
                return o;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float y = normalize(i.dir).y - _HorizonHeight;  // >0 arriba, <0 abajo
                float up = saturate(y / _BlendUp);
                float down = saturate(-y / _BlendDown);
                // Tinte del modo: si no hay color (ColorAmount = 0) no cambia nada
                half3 skyTint  = lerp(half3(1, 1, 1), _MM_ModeColor.rgb, saturate(_MM_ColorAmount) * _TintStrength);
                half3 voidTint = lerp(half3(1, 1, 1), _MM_ModeColor.rgb, saturate(_MM_ColorAmount) * _VoidTintStrength);

                half3 c = lerp(_HorizonColor.rgb, _TopColor.rgb, up) * skyTint;
                c = lerp(c, _BottomColor.rgb * voidTint, down);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
