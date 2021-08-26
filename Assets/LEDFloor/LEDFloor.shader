Shader "Unlit/LEDFloor"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off
	    Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "./CgIncludes/Random.cginc"
            #include "./CgIncludes/Noise.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 posWS : TEXCOORD1;
            };

            struct Ripple{
                float3 centerPos;
                float4 color;
                float value;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            static const int ripplesMaxCount = 50;

            float bloomIntencity;
            int effectIdx;
            float lEDFloorOpacity;
            float panelWidth;
            float panelHeight;
            float hue;
            float saturation;
            float value;
            float rippleVelocity;
            float rippleThickness;
            float4 effectCenter;
            float effectRange;
            sampler2D gradTex01;
            RWStructuredBuffer<Ripple> ripplesBuffer;

            float3 hsv2rgb(float3 c)
            {
                float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
            }

            float map(float value, float min1, float max1, float min2, float max2) {
                return min2 + (value - min1) * (max2 - min2) / (max1 - min1);
            }

            float4 stripe(float2 ledPanelPos, float3 posWS){
                float2 beginPos = float2(-5, 0);
                float2 endPos = float2(5, 0);
                float2 vec = endPos - beginPos;
                float vecLength = length(vec);
                float2 vecNormalize = normalize(vec);
                float2 vec1 = ledPanelPos - beginPos;
                float k = dot(vecNormalize, vec1) / vecLength;
                float3 hsvColor = hsv2rgb(float3(frac(k+_Time.y) * hue, saturation, value));
                return float4(hsvColor,1) * step(frac(posWS.z * panelHeight), 0.9) * step(frac(posWS.x * panelWidth), 0.9);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.posWS = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float4 c = float4(0,0,0,0);
                
                float2 ledPanelIdx = float2(floor(i.posWS.x * panelWidth), floor(i.posWS.z * panelHeight));
                
                // int effectIdx = 3;

                [branch] switch(effectIdx)
                {
                    case 0:
                    c = stripe(ledPanelIdx, i.posWS);

                    //for Self-made gradation texture
                    // float4 gradColor = tex2D(gradTex01, float2(frac(k+_Time.y), 0));
                    // c = gradColor * step(frac(i.posWS.z * panelHeight), 0.9) * step(frac(i.posWS.x * panelWidth), 0.9);
                    //
                    break;
                    
                    case 1:
                    ////random effect////
                    float v = srand(ledPanelIdx, floor(_Time.y));
                    if (v > 0.5) {
                        c = float4(hsv2rgb(float3(srand(float2(1,1), floor(_Time.y))* hue, saturation, value)),1) * step(frac(i.posWS.z * panelHeight), 0.9) * step(frac(i.posWS.x * panelWidth), 0.9);
                    }
                    break;

                    case 2:
                    //random color effect////
                    float h = srand(ledPanelIdx, floor(_Time.y));
                    c = float4(hsv2rgb(float3(h* hue, saturation, value)),1) * step(frac(i.posWS.z * panelHeight), 0.9) * step(frac(i.posWS.x * panelWidth), 0.9);
                    break;

                    case 3:
                    ////circle effect////
                    float d = distance(ledPanelIdx, effectCenter.xz);
                    float direction = 1; //1 or -1
                    d = d * 0.1;
                    c = float4(hsv2rgb(float3(frac(d+_Time.y)*direction* hue,saturation,value)), 1) * step(frac(i.posWS.z * panelHeight), 0.9) * step(frac(i.posWS.x * panelWidth), 0.9);
                    break;

                    ////ripples effect////
                    case 4:
                    //c = stripe(ledPanelIdx, i.posWS);
                    for(int k=0; k<ripplesMaxCount; k++){
                        float d = distance(ripplesBuffer[k].centerPos.xz * panelWidth, ledPanelIdx);
                        float _value = ripplesBuffer[k].value * rippleVelocity;
                        if(d < _value && d > _value - rippleThickness){
                            c = ripplesBuffer[k].color  * step(frac(i.posWS.z * panelHeight), 0.9) * step(frac(i.posWS.x * panelWidth), 0.9);
                        }
                    }
                    break;

                    ////texture effect////
                    case 5:
                    c = stripe(ledPanelIdx, i.posWS);

                    float3x3 rotateMatrix = float3x3(
                        cos(_Time.y), -sin(_Time.y), 0,
                        sin(_Time.y), cos(_Time.y), 0,
                        0, 0, 1
                    );

                    float _u = map(ledPanelIdx.x, effectCenter.x-effectRange*0.5, effectCenter.x+effectRange*0.5, 0, 1);
                    float _v = map(ledPanelIdx.y, effectCenter.z-effectRange*0.5, effectCenter.z+effectRange*0.5, 0, 1);
                    float3x3 m = rotateMatrix;
                    c += tex2D(_MainTex, mul(m, float2(_u, _v))+_SinTime.x) * step(frac(i.posWS.z * panelHeight), 0.9) * step(frac(i.posWS.x * panelWidth), 0.9);
                    break;

                    ////line effect////
                    case 6:
                    c = float4(hsv2rgb(float3(srand(float2(1,1), floor(_Time.y))* hue, saturation, value)),1) *  (1-step(frac(i.posWS.z * panelHeight), 0.9) * step(frac(i.posWS.x * panelWidth), 0.9));
                    break;

                    default:
                    break;
                }
                
                c.a *= lEDFloorOpacity;
                c.rgb *= bloomIntencity;
                return c ;
            }
            ENDHLSL
        }
    }
}
