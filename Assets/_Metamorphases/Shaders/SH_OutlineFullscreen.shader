// Contorno de "simulación" a pantalla completa.
// Detecta bordes comparando la PROFUNDIDAD (siluetas, objetos delante de otros) y las NORMALES
// (esquinas de un mismo objeto) de los píxeles vecinos, y pinta ahí una línea.
// Uso: Full Screen Pass Renderer Feature en el renderer de URP (ver guía 02b, C6).
// Lee los globales de color del modo (_MM_ModeColor / _MM_ColorAmount) para poder teñir las líneas.
Shader "Metamorphases/OutlineFullscreen"
{
    Properties
    {
        _OutlineColor ("Color del contorno", Color) = (0.08, 0.08, 0.1, 1)
        _Thickness ("Grosor (píxeles)", Range(0.5, 4)) = 1.2
        _DepthThreshold ("Sensibilidad a la profundidad", Range(0.005, 1)) = 0.08
        _NormalThreshold ("Sensibilidad a las esquinas", Range(0.05, 2)) = 0.4
        _ModeColorMix ("Teñir con el color del modo", Range(0, 1)) = 0
        _GrazingStart ("Ángulo rasante: desde dónde corrige", Range(0, 1)) = 0.5
        _GrazingBoost ("Ángulo rasante: fuerza de la corrección", Range(0, 50)) = 15
        _NearFade ("Sin líneas más cerca de (m)", Range(0, 2)) = 0.4
        _FadeStart ("Empieza a desvanecerse (m)", Float) = 25
        _FadeEnd ("Desaparece del todo (m)", Float) = 70
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "Outline"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            half4 _OutlineColor;
            float _Thickness, _DepthThreshold, _NormalThreshold, _ModeColorMix, _FadeStart, _FadeEnd;
            float _GrazingStart, _GrazingBoost, _NearFade;
            // Globales del modo (los envía WorldColorController)
            half4 _MM_ModeColor;
            float _MM_ColorAmount;

            float Depth01ToMeters(float2 uv) { return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams); }

            half4 Frag (Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;
                half4 col = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                // Tamaño de un píxel en UV, multiplicado por el grosor
                float2 px = (_ScreenParams.zw - 1.0) * _Thickness;
                float2 uvA = uv + float2( px.x,  px.y);
                float2 uvB = uv + float2(-px.x, -px.y);
                float2 uvC = uv + float2(-px.x,  px.y);
                float2 uvD = uv + float2( px.x, -px.y);

                // --- Bordes por profundidad (cruz de Roberts) ---
                float d0 = Depth01ToMeters(uv);
                float dA = Depth01ToMeters(uvA), dB = Depth01ToMeters(uvB);
                float dC = Depth01ToMeters(uvC), dD = Depth01ToMeters(uvD);
                float depthDiff = sqrt((dA - dB) * (dA - dB) + (dC - dD) * (dC - dD));
                float dMin = min(d0, min(min(dA, dB), min(dC, dD)));
                // Corrección de ángulo rasante: una pared vista muy de lado cambia mucho de profundidad
                // entre píxeles vecinos sin ser un borde. Cuanto más de lado, más alto el umbral.
                float3 n0 = SampleSceneNormals(uv);
                float3 nView = mul((float3x3)UNITY_MATRIX_V, n0);
                float facing = saturate(nView.z);                       // 1 = de frente, 0 = de lado
                float grazing = saturate((1.0 - facing - _GrazingStart) / max(1.0 - _GrazingStart, 0.001));
                float threshold = _DepthThreshold * dMin * (1.0 + grazing * _GrazingBoost);
                float depthEdge = step(threshold, depthDiff);            // relativo a la distancia

                // --- Bordes por normales (esquinas) ---
                float3 nA = SampleSceneNormals(uvA), nB = SampleSceneNormals(uvB);
                float3 nC = SampleSceneNormals(uvC), nD = SampleSceneNormals(uvD);
                float normalDiff = sqrt(dot(nA - nB, nA - nB) + dot(nC - nD, nC - nD));
                float normalEdge = step(_NormalThreshold, normalDiff);

                float edge = max(depthEdge, normalEdge);

                // Las líneas lejanas se desvanecen (como la niebla); el cielo no tiene líneas
                float fade = 1.0 - saturate((dMin - _FadeStart) / max(_FadeEnd - _FadeStart, 0.001));
                // Muy pegado a la cámara tampoco hay líneas (evita manchas al rozar una pared)
                fade *= saturate(dMin / max(_NearFade, 0.001));

                half3 lineCol = lerp(_OutlineColor.rgb, _MM_ModeColor.rgb, saturate(_MM_ColorAmount) * _ModeColorMix);
                col.rgb = lerp(col.rgb, lineCol, edge * fade * _OutlineColor.a);
                return col;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
