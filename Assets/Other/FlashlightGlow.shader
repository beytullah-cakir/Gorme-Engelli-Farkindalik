Shader "Custom/FlashlightGlow" {
    Properties {
        _MainTex ("Tex", 2D) = "white" {}
        [Header(Glow Settings)]
        [HDR] _GlowColor ("Glow Color", Color) = (1, 1, 0, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 10)) = 1.0
        
        [Header(Light Reactivity)]
        _LightThreshold ("Light Threshold", Range(0, 1)) = 0.5
        _GlowSharpness ("Glow Sharpness", Range(0.1, 10)) = 5.0
    }
    
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Surface shader
        #pragma surface surf Standard fullforwardshadows

        // Shader model
        #pragma target 3.0

        struct Input {
            float2 uv_MainTex;
            float3 worldNormal;
            float3 viewDir;
            INTERNAL_DATA
        };

        sampler2D _MainTex;
        fixed4 _GlowColor;
        float _GlowIntensity;
        float _LightThreshold;
        float _GlowSharpness;

        void surf (Input IN, inout SurfaceOutputStandard o) {
            // Ana rengi (Doku rengi) al
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb;

            // Görüş açımıza ve normal açısına göre hafif bir Fresnel (Kenar) efekti hesapla
            float fresnel = dot(normalize(IN.viewDir), normalize(IN.worldNormal));
            fresnel = saturate(1.0 - fresnel);
            fresnel = pow(fresnel, 3.0); // Kenarları daha keskin yap

            // Unity'nin Global aydınlatmasından (Işık kaynaklarından) gelen veriyi manuel simüle ediyoruz
            // Normalde Surface Shader'da Işık verisine direkt Albedo içinden ulaşılmıyor (Custom Lighting gerekir)
            // Ama basit ve etkili bir yöntem olarak: Eğer parlama (Emission) açacaksak
            
            // Eğer ışık objenin üstüne vuruyorsa Parlaklığı (Glow) artırıyoruz.
            // Bu basit bir yaklaşım: Objenin kendi rengi veya kenar efekti ile _GlowColor'u çarpıp Emission'a ekle.
            o.Emission = _GlowColor.rgb * _GlowIntensity * fresnel;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
