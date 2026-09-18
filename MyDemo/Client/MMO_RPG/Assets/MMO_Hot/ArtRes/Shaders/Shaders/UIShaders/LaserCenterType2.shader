Shader "IndieImpulse/Unlit/LaserCenterType2"
{
    Properties
    {
        [HDR]Color_18d890ee80824248a51ee0a2b47a330e("Color", Color) = (1, 1, 1, 1)
        [NoScaleOffset]Texture2D_bc1d9eb6d4344d4084d949d7b33e529a("Mask", 2D) = "white" {}
        [NoScaleOffset]Texture2D_4f7017334adb4c31b376db39df7ead2d("MainText", 2D) = "white" {}
        Vector2_47b54501113b4c409f7770bf21e02d97("MainTextSpeed", Vector) = (0, 0, 0, 0)
        _CenterMove("CenterMove", Float) = 0
        _RadialScale("RadialScale", Float) = 1
        _LengthScale("LengthScale", Float) = 0
        _GlowPower("GlowPower", Float) = 0
        _TextureUV("TextureUV", Vector) = (0, 0, 0, 0)
        _Offset("Offset", Vector) = (0, 0, 0, 0)
        [HideInInspector]_BUILTIN_QueueOffset("Float", Float) = 0
        [HideInInspector]_BUILTIN_QueueControl("Float", Float) = -1
    }
    SubShader
    {
        Tags
        {
            // RenderPipeline: <None>
            "RenderType"="Transparent"
            "BuiltInMaterialType" = "Unlit"
            "Queue"="Transparent"
            "ShaderGraphShader"="true"
            "ShaderGraphTargetId"="BuiltInUnlitSubTarget"
        }
        Pass
        {
            Name "Pass"
            Tags
            {
                "LightMode" = "ForwardBase"
            }
        
        // Render State
        Cull Back
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ZTest LEqual
        ZWrite Off
        ColorMask RGB
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 3.0
        #pragma multi_compile_instancing
        #pragma multi_compile_fog
        #pragma multi_compile_fwdbase
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_UNLIT
        #define BUILTIN_TARGET_API 1
        #define _BUILTIN_SURFACE_TYPE_TRANSPARENT 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        #ifdef _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #define _SURFACE_TYPE_TRANSPARENT _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #endif
        #ifdef _BUILTIN_ALPHATEST_ON
        #define _ALPHATEST_ON _BUILTIN_ALPHATEST_ON
        #endif
        #ifdef _BUILTIN_AlphaClip
        #define _AlphaClip _BUILTIN_AlphaClip
        #endif
        #ifdef _BUILTIN_ALPHAPREMULTIPLY_ON
        #define _ALPHAPREMULTIPLY_ON _BUILTIN_ALPHAPREMULTIPLY_ON
        #endif
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacySurfaceVertex.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderGraphFunctions.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
             float3 TimeParameters;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 interp0 : INTERP0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.interp0.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.interp0.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 Color_18d890ee80824248a51ee0a2b47a330e;
        float4 Texture2D_bc1d9eb6d4344d4084d949d7b33e529a_TexelSize;
        float4 Texture2D_4f7017334adb4c31b376db39df7ead2d_TexelSize;
        float2 Vector2_47b54501113b4c409f7770bf21e02d97;
        float _CenterMove;
        float _RadialScale;
        float _LengthScale;
        float _GlowPower;
        float2 _TextureUV;
        float2 _Offset;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(Texture2D_bc1d9eb6d4344d4084d949d7b33e529a);
        SAMPLER(samplerTexture2D_bc1d9eb6d4344d4084d949d7b33e529a);
        TEXTURE2D(Texture2D_4f7017334adb4c31b376db39df7ead2d);
        SAMPLER(samplerTexture2D_4f7017334adb4c31b376db39df7ead2d);
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // Graph Functions
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_PolarCoordinates_float(float2 UV, float2 Center, float RadialScale, float LengthScale, out float2 Out)
        {
            float2 delta = UV - Center;
            float radius = length(delta) * 2 * RadialScale;
            float angle = atan2(delta.x, delta.y) * 1.0/6.28 * LengthScale;
            Out = float2(radius, angle);
        }
        
        void Unity_Add_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A + B;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float3 BaseColor;
            float Alpha;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            float4 _Property_01bbe1e7680b401e874750d026308a46_Out_0 = IsGammaSpace() ? LinearToSRGB(Color_18d890ee80824248a51ee0a2b47a330e) : Color_18d890ee80824248a51ee0a2b47a330e;
            UnityTexture2D _Property_5891bba0aebe4bf2b31acc2e171b012e_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_bc1d9eb6d4344d4084d949d7b33e529a);
            float2 _Property_ab841cbd569f4e04a26ed3c5cb81b536_Out_0 = _TextureUV;
            float2 _Property_fc0085f8f562417086ae555779a1a5e9_Out_0 = _Offset;
            float2 _TilingAndOffset_1380d1065c91459d87f391d42d863d5b_Out_3;
            Unity_TilingAndOffset_float(IN.uv0.xy, _Property_ab841cbd569f4e04a26ed3c5cb81b536_Out_0, _Property_fc0085f8f562417086ae555779a1a5e9_Out_0, _TilingAndOffset_1380d1065c91459d87f391d42d863d5b_Out_3);
            float4 _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0 = SAMPLE_TEXTURE2D(_Property_5891bba0aebe4bf2b31acc2e171b012e_Out_0.tex, _Property_5891bba0aebe4bf2b31acc2e171b012e_Out_0.samplerstate, _Property_5891bba0aebe4bf2b31acc2e171b012e_Out_0.GetTransformedUV(_TilingAndOffset_1380d1065c91459d87f391d42d863d5b_Out_3));
            float _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_R_4 = _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0.r;
            float _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_G_5 = _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0.g;
            float _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_B_6 = _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0.b;
            float _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_A_7 = _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0.a;
            UnityTexture2D _Property_e53f115c5350438db818ee2e25e84f66_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_4f7017334adb4c31b376db39df7ead2d);
            float _Property_9387fc1c17c047f4be709f020e55ec34_Out_0 = _CenterMove;
            float _Multiply_a373401042c648f6ac4b2ed2422bc8bc_Out_2;
            Unity_Multiply_float_float(IN.TimeParameters.x, _Property_9387fc1c17c047f4be709f020e55ec34_Out_0, _Multiply_a373401042c648f6ac4b2ed2422bc8bc_Out_2);
            float _Property_37fc07ab4bc24dca91dd91c05783885a_Out_0 = _RadialScale;
            float _Property_083539affd1242babdd9157da68bf4cb_Out_0 = _LengthScale;
            float2 _PolarCoordinates_24cc83dddfd0418c9ba9f8b355330f3d_Out_4;
            Unity_PolarCoordinates_float(IN.uv0.xy, float2 (0.5, 0.5), _Property_37fc07ab4bc24dca91dd91c05783885a_Out_0, _Property_083539affd1242babdd9157da68bf4cb_Out_0, _PolarCoordinates_24cc83dddfd0418c9ba9f8b355330f3d_Out_4);
            float2 _Add_6b18f81639294ea7b3c26d03ec7e3346_Out_2;
            Unity_Add_float2((_Multiply_a373401042c648f6ac4b2ed2422bc8bc_Out_2.xx), _PolarCoordinates_24cc83dddfd0418c9ba9f8b355330f3d_Out_4, _Add_6b18f81639294ea7b3c26d03ec7e3346_Out_2);
            float4 _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0 = SAMPLE_TEXTURE2D(_Property_e53f115c5350438db818ee2e25e84f66_Out_0.tex, _Property_e53f115c5350438db818ee2e25e84f66_Out_0.samplerstate, _Property_e53f115c5350438db818ee2e25e84f66_Out_0.GetTransformedUV(_Add_6b18f81639294ea7b3c26d03ec7e3346_Out_2));
            float _SampleTexture2D_b792db77a11748a88494980d164a77ed_R_4 = _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0.r;
            float _SampleTexture2D_b792db77a11748a88494980d164a77ed_G_5 = _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0.g;
            float _SampleTexture2D_b792db77a11748a88494980d164a77ed_B_6 = _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0.b;
            float _SampleTexture2D_b792db77a11748a88494980d164a77ed_A_7 = _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0.a;
            float4 _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2;
            Unity_Multiply_float4_float4(_SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0, _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0, _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2);
            float4 _Multiply_073d50ba797e42daa7dfb3574c85ef1f_Out_2;
            Unity_Multiply_float4_float4(_Property_01bbe1e7680b401e874750d026308a46_Out_0, _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2, _Multiply_073d50ba797e42daa7dfb3574c85ef1f_Out_2);
            float _Split_57fa424231f8454c962dff75f2eb208a_R_1 = _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2[0];
            float _Split_57fa424231f8454c962dff75f2eb208a_G_2 = _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2[1];
            float _Split_57fa424231f8454c962dff75f2eb208a_B_3 = _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2[2];
            float _Split_57fa424231f8454c962dff75f2eb208a_A_4 = _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2[3];
            surface.BaseColor = (_Multiply_073d50ba797e42daa7dfb3574c85ef1f_Out_2.xyz);
            surface.Alpha = _Split_57fa424231f8454c962dff75f2eb208a_R_1;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            
        
        
        
        
        
            output.uv0 = input.texCoord0;
            output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        void BuildAppDataFull(Attributes attributes, VertexDescription vertexDescription, inout appdata_full result)
        {
            result.vertex     = float4(attributes.positionOS, 1);
            result.tangent    = attributes.tangentOS;
            result.normal     = attributes.normalOS;
            result.texcoord   = attributes.uv0;
            result.vertex     = float4(vertexDescription.Position, 1);
            result.normal     = vertexDescription.Normal;
            result.tangent    = float4(vertexDescription.Tangent, 0);
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
        }
        
        void VaryingsToSurfaceVertex(Varyings varyings, inout v2f_surf result)
        {
            result.pos = varyings.positionCS;
            // World Tangent isn't an available input on v2f_surf
        
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if UNITY_SHOULD_SAMPLE_SH
            #endif
            #if defined(LIGHTMAP_ON)
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogCoord = varyings.fogFactorAndVertexLight.x;
                COPY_TO_LIGHT_COORDS(result, varyings.fogFactorAndVertexLight.yzw);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyings, result);
        }
        
        void SurfaceVertexToVaryings(v2f_surf surfVertex, inout Varyings result)
        {
            result.positionCS = surfVertex.pos;
            // viewDirectionWS is never filled out in the legacy pass' function. Always use the value computed by SRP
            // World Tangent isn't an available input on v2f_surf
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if UNITY_SHOULD_SAMPLE_SH
            #endif
            #if defined(LIGHTMAP_ON)
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogFactorAndVertexLight.x = surfVertex.fogCoord;
                COPY_FROM_LIGHT_COORDS(result.fogFactorAndVertexLight.yzw, surfVertex);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(surfVertex, result);
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/UnlitPass.hlsl"
        
        ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
        
        // Render State
        Cull Back
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        ZTest LEqual
        ZWrite On
        ColorMask 0
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 3.0
        #pragma multi_compile_shadowcaster
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        #pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW
        // GraphKeywords: <None>
        
        // Defines
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SHADERPASS_SHADOWCASTER
        #define BUILTIN_TARGET_API 1
        #define _BUILTIN_SURFACE_TYPE_TRANSPARENT 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        #ifdef _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #define _SURFACE_TYPE_TRANSPARENT _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #endif
        #ifdef _BUILTIN_ALPHATEST_ON
        #define _ALPHATEST_ON _BUILTIN_ALPHATEST_ON
        #endif
        #ifdef _BUILTIN_AlphaClip
        #define _AlphaClip _BUILTIN_AlphaClip
        #endif
        #ifdef _BUILTIN_ALPHAPREMULTIPLY_ON
        #define _ALPHAPREMULTIPLY_ON _BUILTIN_ALPHAPREMULTIPLY_ON
        #endif
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacySurfaceVertex.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderGraphFunctions.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
             float3 TimeParameters;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 interp0 : INTERP0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.interp0.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.interp0.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 Color_18d890ee80824248a51ee0a2b47a330e;
        float4 Texture2D_bc1d9eb6d4344d4084d949d7b33e529a_TexelSize;
        float4 Texture2D_4f7017334adb4c31b376db39df7ead2d_TexelSize;
        float2 Vector2_47b54501113b4c409f7770bf21e02d97;
        float _CenterMove;
        float _RadialScale;
        float _LengthScale;
        float _GlowPower;
        float2 _TextureUV;
        float2 _Offset;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(Texture2D_bc1d9eb6d4344d4084d949d7b33e529a);
        SAMPLER(samplerTexture2D_bc1d9eb6d4344d4084d949d7b33e529a);
        TEXTURE2D(Texture2D_4f7017334adb4c31b376db39df7ead2d);
        SAMPLER(samplerTexture2D_4f7017334adb4c31b376db39df7ead2d);
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // Graph Functions
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_PolarCoordinates_float(float2 UV, float2 Center, float RadialScale, float LengthScale, out float2 Out)
        {
            float2 delta = UV - Center;
            float radius = length(delta) * 2 * RadialScale;
            float angle = atan2(delta.x, delta.y) * 1.0/6.28 * LengthScale;
            Out = float2(radius, angle);
        }
        
        void Unity_Add_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A + B;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_5891bba0aebe4bf2b31acc2e171b012e_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_bc1d9eb6d4344d4084d949d7b33e529a);
            float2 _Property_ab841cbd569f4e04a26ed3c5cb81b536_Out_0 = _TextureUV;
            float2 _Property_fc0085f8f562417086ae555779a1a5e9_Out_0 = _Offset;
            float2 _TilingAndOffset_1380d1065c91459d87f391d42d863d5b_Out_3;
            Unity_TilingAndOffset_float(IN.uv0.xy, _Property_ab841cbd569f4e04a26ed3c5cb81b536_Out_0, _Property_fc0085f8f562417086ae555779a1a5e9_Out_0, _TilingAndOffset_1380d1065c91459d87f391d42d863d5b_Out_3);
            float4 _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0 = SAMPLE_TEXTURE2D(_Property_5891bba0aebe4bf2b31acc2e171b012e_Out_0.tex, _Property_5891bba0aebe4bf2b31acc2e171b012e_Out_0.samplerstate, _Property_5891bba0aebe4bf2b31acc2e171b012e_Out_0.GetTransformedUV(_TilingAndOffset_1380d1065c91459d87f391d42d863d5b_Out_3));
            float _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_R_4 = _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0.r;
            float _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_G_5 = _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0.g;
            float _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_B_6 = _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0.b;
            float _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_A_7 = _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0.a;
            UnityTexture2D _Property_e53f115c5350438db818ee2e25e84f66_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_4f7017334adb4c31b376db39df7ead2d);
            float _Property_9387fc1c17c047f4be709f020e55ec34_Out_0 = _CenterMove;
            float _Multiply_a373401042c648f6ac4b2ed2422bc8bc_Out_2;
            Unity_Multiply_float_float(IN.TimeParameters.x, _Property_9387fc1c17c047f4be709f020e55ec34_Out_0, _Multiply_a373401042c648f6ac4b2ed2422bc8bc_Out_2);
            float _Property_37fc07ab4bc24dca91dd91c05783885a_Out_0 = _RadialScale;
            float _Property_083539affd1242babdd9157da68bf4cb_Out_0 = _LengthScale;
            float2 _PolarCoordinates_24cc83dddfd0418c9ba9f8b355330f3d_Out_4;
            Unity_PolarCoordinates_float(IN.uv0.xy, float2 (0.5, 0.5), _Property_37fc07ab4bc24dca91dd91c05783885a_Out_0, _Property_083539affd1242babdd9157da68bf4cb_Out_0, _PolarCoordinates_24cc83dddfd0418c9ba9f8b355330f3d_Out_4);
            float2 _Add_6b18f81639294ea7b3c26d03ec7e3346_Out_2;
            Unity_Add_float2((_Multiply_a373401042c648f6ac4b2ed2422bc8bc_Out_2.xx), _PolarCoordinates_24cc83dddfd0418c9ba9f8b355330f3d_Out_4, _Add_6b18f81639294ea7b3c26d03ec7e3346_Out_2);
            float4 _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0 = SAMPLE_TEXTURE2D(_Property_e53f115c5350438db818ee2e25e84f66_Out_0.tex, _Property_e53f115c5350438db818ee2e25e84f66_Out_0.samplerstate, _Property_e53f115c5350438db818ee2e25e84f66_Out_0.GetTransformedUV(_Add_6b18f81639294ea7b3c26d03ec7e3346_Out_2));
            float _SampleTexture2D_b792db77a11748a88494980d164a77ed_R_4 = _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0.r;
            float _SampleTexture2D_b792db77a11748a88494980d164a77ed_G_5 = _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0.g;
            float _SampleTexture2D_b792db77a11748a88494980d164a77ed_B_6 = _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0.b;
            float _SampleTexture2D_b792db77a11748a88494980d164a77ed_A_7 = _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0.a;
            float4 _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2;
            Unity_Multiply_float4_float4(_SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0, _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0, _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2);
            float _Split_57fa424231f8454c962dff75f2eb208a_R_1 = _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2[0];
            float _Split_57fa424231f8454c962dff75f2eb208a_G_2 = _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2[1];
            float _Split_57fa424231f8454c962dff75f2eb208a_B_3 = _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2[2];
            float _Split_57fa424231f8454c962dff75f2eb208a_A_4 = _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2[3];
            surface.Alpha = _Split_57fa424231f8454c962dff75f2eb208a_R_1;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            
        
        
        
        
        
            output.uv0 = input.texCoord0;
            output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        void BuildAppDataFull(Attributes attributes, VertexDescription vertexDescription, inout appdata_full result)
        {
            result.vertex     = float4(attributes.positionOS, 1);
            result.tangent    = attributes.tangentOS;
            result.normal     = attributes.normalOS;
            result.texcoord   = attributes.uv0;
            result.vertex     = float4(vertexDescription.Position, 1);
            result.normal     = vertexDescription.Normal;
            result.tangent    = float4(vertexDescription.Tangent, 0);
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
        }
        
        void VaryingsToSurfaceVertex(Varyings varyings, inout v2f_surf result)
        {
            result.pos = varyings.positionCS;
            // World Tangent isn't an available input on v2f_surf
        
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if UNITY_SHOULD_SAMPLE_SH
            #endif
            #if defined(LIGHTMAP_ON)
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogCoord = varyings.fogFactorAndVertexLight.x;
                COPY_TO_LIGHT_COORDS(result, varyings.fogFactorAndVertexLight.yzw);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyings, result);
        }
        
        void SurfaceVertexToVaryings(v2f_surf surfVertex, inout Varyings result)
        {
            result.positionCS = surfVertex.pos;
            // viewDirectionWS is never filled out in the legacy pass' function. Always use the value computed by SRP
            // World Tangent isn't an available input on v2f_surf
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if UNITY_SHOULD_SAMPLE_SH
            #endif
            #if defined(LIGHTMAP_ON)
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogFactorAndVertexLight.x = surfVertex.fogCoord;
                COPY_FROM_LIGHT_COORDS(result.fogFactorAndVertexLight.yzw, surfVertex);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(surfVertex, result);
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShadowCasterPass.hlsl"
        
        ENDHLSL
        }
        Pass
        {
            Name "SceneSelectionPass"
            Tags
            {
                "LightMode" = "SceneSelectionPass"
            }
        
        // Render State
        Cull Off
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 3.0
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS SceneSelectionPass
        #define BUILTIN_TARGET_API 1
        #define SCENESELECTIONPASS 1
        #define _BUILTIN_SURFACE_TYPE_TRANSPARENT 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        #ifdef _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #define _SURFACE_TYPE_TRANSPARENT _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #endif
        #ifdef _BUILTIN_ALPHATEST_ON
        #define _ALPHATEST_ON _BUILTIN_ALPHATEST_ON
        #endif
        #ifdef _BUILTIN_AlphaClip
        #define _AlphaClip _BUILTIN_AlphaClip
        #endif
        #ifdef _BUILTIN_ALPHAPREMULTIPLY_ON
        #define _ALPHAPREMULTIPLY_ON _BUILTIN_ALPHAPREMULTIPLY_ON
        #endif
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacySurfaceVertex.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderGraphFunctions.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
             float3 TimeParameters;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 interp0 : INTERP0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.interp0.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.interp0.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 Color_18d890ee80824248a51ee0a2b47a330e;
        float4 Texture2D_bc1d9eb6d4344d4084d949d7b33e529a_TexelSize;
        float4 Texture2D_4f7017334adb4c31b376db39df7ead2d_TexelSize;
        float2 Vector2_47b54501113b4c409f7770bf21e02d97;
        float _CenterMove;
        float _RadialScale;
        float _LengthScale;
        float _GlowPower;
        float2 _TextureUV;
        float2 _Offset;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(Texture2D_bc1d9eb6d4344d4084d949d7b33e529a);
        SAMPLER(samplerTexture2D_bc1d9eb6d4344d4084d949d7b33e529a);
        TEXTURE2D(Texture2D_4f7017334adb4c31b376db39df7ead2d);
        SAMPLER(samplerTexture2D_4f7017334adb4c31b376db39df7ead2d);
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // Graph Functions
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_PolarCoordinates_float(float2 UV, float2 Center, float RadialScale, float LengthScale, out float2 Out)
        {
            float2 delta = UV - Center;
            float radius = length(delta) * 2 * RadialScale;
            float angle = atan2(delta.x, delta.y) * 1.0/6.28 * LengthScale;
            Out = float2(radius, angle);
        }
        
        void Unity_Add_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A + B;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_5891bba0aebe4bf2b31acc2e171b012e_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_bc1d9eb6d4344d4084d949d7b33e529a);
            float2 _Property_ab841cbd569f4e04a26ed3c5cb81b536_Out_0 = _TextureUV;
            float2 _Property_fc0085f8f562417086ae555779a1a5e9_Out_0 = _Offset;
            float2 _TilingAndOffset_1380d1065c91459d87f391d42d863d5b_Out_3;
            Unity_TilingAndOffset_float(IN.uv0.xy, _Property_ab841cbd569f4e04a26ed3c5cb81b536_Out_0, _Property_fc0085f8f562417086ae555779a1a5e9_Out_0, _TilingAndOffset_1380d1065c91459d87f391d42d863d5b_Out_3);
            float4 _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0 = SAMPLE_TEXTURE2D(_Property_5891bba0aebe4bf2b31acc2e171b012e_Out_0.tex, _Property_5891bba0aebe4bf2b31acc2e171b012e_Out_0.samplerstate, _Property_5891bba0aebe4bf2b31acc2e171b012e_Out_0.GetTransformedUV(_TilingAndOffset_1380d1065c91459d87f391d42d863d5b_Out_3));
            float _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_R_4 = _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0.r;
            float _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_G_5 = _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0.g;
            float _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_B_6 = _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0.b;
            float _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_A_7 = _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0.a;
            UnityTexture2D _Property_e53f115c5350438db818ee2e25e84f66_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_4f7017334adb4c31b376db39df7ead2d);
            float _Property_9387fc1c17c047f4be709f020e55ec34_Out_0 = _CenterMove;
            float _Multiply_a373401042c648f6ac4b2ed2422bc8bc_Out_2;
            Unity_Multiply_float_float(IN.TimeParameters.x, _Property_9387fc1c17c047f4be709f020e55ec34_Out_0, _Multiply_a373401042c648f6ac4b2ed2422bc8bc_Out_2);
            float _Property_37fc07ab4bc24dca91dd91c05783885a_Out_0 = _RadialScale;
            float _Property_083539affd1242babdd9157da68bf4cb_Out_0 = _LengthScale;
            float2 _PolarCoordinates_24cc83dddfd0418c9ba9f8b355330f3d_Out_4;
            Unity_PolarCoordinates_float(IN.uv0.xy, float2 (0.5, 0.5), _Property_37fc07ab4bc24dca91dd91c05783885a_Out_0, _Property_083539affd1242babdd9157da68bf4cb_Out_0, _PolarCoordinates_24cc83dddfd0418c9ba9f8b355330f3d_Out_4);
            float2 _Add_6b18f81639294ea7b3c26d03ec7e3346_Out_2;
            Unity_Add_float2((_Multiply_a373401042c648f6ac4b2ed2422bc8bc_Out_2.xx), _PolarCoordinates_24cc83dddfd0418c9ba9f8b355330f3d_Out_4, _Add_6b18f81639294ea7b3c26d03ec7e3346_Out_2);
            float4 _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0 = SAMPLE_TEXTURE2D(_Property_e53f115c5350438db818ee2e25e84f66_Out_0.tex, _Property_e53f115c5350438db818ee2e25e84f66_Out_0.samplerstate, _Property_e53f115c5350438db818ee2e25e84f66_Out_0.GetTransformedUV(_Add_6b18f81639294ea7b3c26d03ec7e3346_Out_2));
            float _SampleTexture2D_b792db77a11748a88494980d164a77ed_R_4 = _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0.r;
            float _SampleTexture2D_b792db77a11748a88494980d164a77ed_G_5 = _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0.g;
            float _SampleTexture2D_b792db77a11748a88494980d164a77ed_B_6 = _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0.b;
            float _SampleTexture2D_b792db77a11748a88494980d164a77ed_A_7 = _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0.a;
            float4 _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2;
            Unity_Multiply_float4_float4(_SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0, _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0, _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2);
            float _Split_57fa424231f8454c962dff75f2eb208a_R_1 = _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2[0];
            float _Split_57fa424231f8454c962dff75f2eb208a_G_2 = _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2[1];
            float _Split_57fa424231f8454c962dff75f2eb208a_B_3 = _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2[2];
            float _Split_57fa424231f8454c962dff75f2eb208a_A_4 = _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2[3];
            surface.Alpha = _Split_57fa424231f8454c962dff75f2eb208a_R_1;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            
        
        
        
        
        
            output.uv0 = input.texCoord0;
            output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        void BuildAppDataFull(Attributes attributes, VertexDescription vertexDescription, inout appdata_full result)
        {
            result.vertex     = float4(attributes.positionOS, 1);
            result.tangent    = attributes.tangentOS;
            result.normal     = attributes.normalOS;
            result.texcoord   = attributes.uv0;
            result.vertex     = float4(vertexDescription.Position, 1);
            result.normal     = vertexDescription.Normal;
            result.tangent    = float4(vertexDescription.Tangent, 0);
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
        }
        
        void VaryingsToSurfaceVertex(Varyings varyings, inout v2f_surf result)
        {
            result.pos = varyings.positionCS;
            // World Tangent isn't an available input on v2f_surf
        
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if UNITY_SHOULD_SAMPLE_SH
            #endif
            #if defined(LIGHTMAP_ON)
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogCoord = varyings.fogFactorAndVertexLight.x;
                COPY_TO_LIGHT_COORDS(result, varyings.fogFactorAndVertexLight.yzw);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyings, result);
        }
        
        void SurfaceVertexToVaryings(v2f_surf surfVertex, inout Varyings result)
        {
            result.positionCS = surfVertex.pos;
            // viewDirectionWS is never filled out in the legacy pass' function. Always use the value computed by SRP
            // World Tangent isn't an available input on v2f_surf
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if UNITY_SHOULD_SAMPLE_SH
            #endif
            #if defined(LIGHTMAP_ON)
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogFactorAndVertexLight.x = surfVertex.fogCoord;
                COPY_FROM_LIGHT_COORDS(result.fogFactorAndVertexLight.yzw, surfVertex);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(surfVertex, result);
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"
        
        ENDHLSL
        }
        Pass
        {
            Name "ScenePickingPass"
            Tags
            {
                "LightMode" = "Picking"
            }
        
        // Render State
        Cull Back
        
        // Debug
        // <None>
        
        // --------------------------------------------------
        // Pass
        
        HLSLPROGRAM
        
        // Pragmas
        #pragma target 3.0
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag
        
        // DotsInstancingOptions: <None>
        // HybridV1InjectedBuiltinProperties: <None>
        
        // Keywords
        // PassKeywords: <None>
        // GraphKeywords: <None>
        
        // Defines
        #define ATTRIBUTES_NEED_NORMAL
        #define ATTRIBUTES_NEED_TANGENT
        #define ATTRIBUTES_NEED_TEXCOORD0
        #define VARYINGS_NEED_TEXCOORD0
        #define FEATURES_GRAPH_VERTEX
        /* WARNING: $splice Could not find named fragment 'PassInstancing' */
        #define SHADERPASS ScenePickingPass
        #define BUILTIN_TARGET_API 1
        #define SCENEPICKINGPASS 1
        #define _BUILTIN_SURFACE_TYPE_TRANSPARENT 1
        /* WARNING: $splice Could not find named fragment 'DotsInstancingVars' */
        #ifdef _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #define _SURFACE_TYPE_TRANSPARENT _BUILTIN_SURFACE_TYPE_TRANSPARENT
        #endif
        #ifdef _BUILTIN_ALPHATEST_ON
        #define _ALPHATEST_ON _BUILTIN_ALPHATEST_ON
        #endif
        #ifdef _BUILTIN_AlphaClip
        #define _AlphaClip _BUILTIN_AlphaClip
        #endif
        #ifdef _BUILTIN_ALPHAPREMULTIPLY_ON
        #define _ALPHAPREMULTIPLY_ON _BUILTIN_ALPHAPREMULTIPLY_ON
        #endif
        
        
        // custom interpolator pre-include
        /* WARNING: $splice Could not find named fragment 'sgci_CustomInterpolatorPreInclude' */
        
        // Includes
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Shim/Shims.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/Lighting.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/LegacySurfaceVertex.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderGraphFunctions.hlsl"
        
        // --------------------------------------------------
        // Structs and Packing
        
        // custom interpolators pre packing
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPrePacking' */
        
        struct Attributes
        {
             float3 positionOS : POSITION;
             float3 normalOS : NORMAL;
             float4 tangentOS : TANGENT;
             float4 uv0 : TEXCOORD0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : INSTANCEID_SEMANTIC;
            #endif
        };
        struct Varyings
        {
             float4 positionCS : SV_POSITION;
             float4 texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        struct SurfaceDescriptionInputs
        {
             float4 uv0;
             float3 TimeParameters;
        };
        struct VertexDescriptionInputs
        {
             float3 ObjectSpaceNormal;
             float3 ObjectSpaceTangent;
             float3 ObjectSpacePosition;
        };
        struct PackedVaryings
        {
             float4 positionCS : SV_POSITION;
             float4 interp0 : INTERP0;
            #if UNITY_ANY_INSTANCING_ENABLED
             uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
             uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
             uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
             FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
        };
        
        PackedVaryings PackVaryings (Varyings input)
        {
            PackedVaryings output;
            ZERO_INITIALIZE(PackedVaryings, output);
            output.positionCS = input.positionCS;
            output.interp0.xyzw =  input.texCoord0;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        Varyings UnpackVaryings (PackedVaryings input)
        {
            Varyings output;
            output.positionCS = input.positionCS;
            output.texCoord0 = input.interp0.xyzw;
            #if UNITY_ANY_INSTANCING_ENABLED
            output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
            output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
            output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
            output.cullFace = input.cullFace;
            #endif
            return output;
        }
        
        
        // --------------------------------------------------
        // Graph
        
        // Graph Properties
        CBUFFER_START(UnityPerMaterial)
        float4 Color_18d890ee80824248a51ee0a2b47a330e;
        float4 Texture2D_bc1d9eb6d4344d4084d949d7b33e529a_TexelSize;
        float4 Texture2D_4f7017334adb4c31b376db39df7ead2d_TexelSize;
        float2 Vector2_47b54501113b4c409f7770bf21e02d97;
        float _CenterMove;
        float _RadialScale;
        float _LengthScale;
        float _GlowPower;
        float2 _TextureUV;
        float2 _Offset;
        CBUFFER_END
        
        // Object and Global properties
        SAMPLER(SamplerState_Linear_Repeat);
        TEXTURE2D(Texture2D_bc1d9eb6d4344d4084d949d7b33e529a);
        SAMPLER(samplerTexture2D_bc1d9eb6d4344d4084d949d7b33e529a);
        TEXTURE2D(Texture2D_4f7017334adb4c31b376db39df7ead2d);
        SAMPLER(samplerTexture2D_4f7017334adb4c31b376db39df7ead2d);
        
        // -- Property used by ScenePickingPass
        #ifdef SCENEPICKINGPASS
        float4 _SelectionID;
        #endif
        
        // -- Properties used by SceneSelectionPass
        #ifdef SCENESELECTIONPASS
        int _ObjectId;
        int _PassValue;
        #endif
        
        // Graph Includes
        // GraphIncludes: <None>
        
        // Graph Functions
        
        void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
        {
            Out = UV * Tiling + Offset;
        }
        
        void Unity_Multiply_float_float(float A, float B, out float Out)
        {
            Out = A * B;
        }
        
        void Unity_PolarCoordinates_float(float2 UV, float2 Center, float RadialScale, float LengthScale, out float2 Out)
        {
            float2 delta = UV - Center;
            float radius = length(delta) * 2 * RadialScale;
            float angle = atan2(delta.x, delta.y) * 1.0/6.28 * LengthScale;
            Out = float2(radius, angle);
        }
        
        void Unity_Add_float2(float2 A, float2 B, out float2 Out)
        {
            Out = A + B;
        }
        
        void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
        {
            Out = A * B;
        }
        
        // Custom interpolators pre vertex
        /* WARNING: $splice Could not find named fragment 'CustomInterpolatorPreVertex' */
        
        // Graph Vertex
        struct VertexDescription
        {
            float3 Position;
            float3 Normal;
            float3 Tangent;
        };
        
        VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
        {
            VertexDescription description = (VertexDescription)0;
            description.Position = IN.ObjectSpacePosition;
            description.Normal = IN.ObjectSpaceNormal;
            description.Tangent = IN.ObjectSpaceTangent;
            return description;
        }
        
        // Custom interpolators, pre surface
        #ifdef FEATURES_GRAPH_VERTEX
        Varyings CustomInterpolatorPassThroughFunc(inout Varyings output, VertexDescription input)
        {
        return output;
        }
        #define CUSTOMINTERPOLATOR_VARYPASSTHROUGH_FUNC
        #endif
        
        // Graph Pixel
        struct SurfaceDescription
        {
            float Alpha;
        };
        
        SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
        {
            SurfaceDescription surface = (SurfaceDescription)0;
            UnityTexture2D _Property_5891bba0aebe4bf2b31acc2e171b012e_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_bc1d9eb6d4344d4084d949d7b33e529a);
            float2 _Property_ab841cbd569f4e04a26ed3c5cb81b536_Out_0 = _TextureUV;
            float2 _Property_fc0085f8f562417086ae555779a1a5e9_Out_0 = _Offset;
            float2 _TilingAndOffset_1380d1065c91459d87f391d42d863d5b_Out_3;
            Unity_TilingAndOffset_float(IN.uv0.xy, _Property_ab841cbd569f4e04a26ed3c5cb81b536_Out_0, _Property_fc0085f8f562417086ae555779a1a5e9_Out_0, _TilingAndOffset_1380d1065c91459d87f391d42d863d5b_Out_3);
            float4 _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0 = SAMPLE_TEXTURE2D(_Property_5891bba0aebe4bf2b31acc2e171b012e_Out_0.tex, _Property_5891bba0aebe4bf2b31acc2e171b012e_Out_0.samplerstate, _Property_5891bba0aebe4bf2b31acc2e171b012e_Out_0.GetTransformedUV(_TilingAndOffset_1380d1065c91459d87f391d42d863d5b_Out_3));
            float _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_R_4 = _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0.r;
            float _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_G_5 = _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0.g;
            float _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_B_6 = _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0.b;
            float _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_A_7 = _SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0.a;
            UnityTexture2D _Property_e53f115c5350438db818ee2e25e84f66_Out_0 = UnityBuildTexture2DStructNoScale(Texture2D_4f7017334adb4c31b376db39df7ead2d);
            float _Property_9387fc1c17c047f4be709f020e55ec34_Out_0 = _CenterMove;
            float _Multiply_a373401042c648f6ac4b2ed2422bc8bc_Out_2;
            Unity_Multiply_float_float(IN.TimeParameters.x, _Property_9387fc1c17c047f4be709f020e55ec34_Out_0, _Multiply_a373401042c648f6ac4b2ed2422bc8bc_Out_2);
            float _Property_37fc07ab4bc24dca91dd91c05783885a_Out_0 = _RadialScale;
            float _Property_083539affd1242babdd9157da68bf4cb_Out_0 = _LengthScale;
            float2 _PolarCoordinates_24cc83dddfd0418c9ba9f8b355330f3d_Out_4;
            Unity_PolarCoordinates_float(IN.uv0.xy, float2 (0.5, 0.5), _Property_37fc07ab4bc24dca91dd91c05783885a_Out_0, _Property_083539affd1242babdd9157da68bf4cb_Out_0, _PolarCoordinates_24cc83dddfd0418c9ba9f8b355330f3d_Out_4);
            float2 _Add_6b18f81639294ea7b3c26d03ec7e3346_Out_2;
            Unity_Add_float2((_Multiply_a373401042c648f6ac4b2ed2422bc8bc_Out_2.xx), _PolarCoordinates_24cc83dddfd0418c9ba9f8b355330f3d_Out_4, _Add_6b18f81639294ea7b3c26d03ec7e3346_Out_2);
            float4 _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0 = SAMPLE_TEXTURE2D(_Property_e53f115c5350438db818ee2e25e84f66_Out_0.tex, _Property_e53f115c5350438db818ee2e25e84f66_Out_0.samplerstate, _Property_e53f115c5350438db818ee2e25e84f66_Out_0.GetTransformedUV(_Add_6b18f81639294ea7b3c26d03ec7e3346_Out_2));
            float _SampleTexture2D_b792db77a11748a88494980d164a77ed_R_4 = _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0.r;
            float _SampleTexture2D_b792db77a11748a88494980d164a77ed_G_5 = _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0.g;
            float _SampleTexture2D_b792db77a11748a88494980d164a77ed_B_6 = _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0.b;
            float _SampleTexture2D_b792db77a11748a88494980d164a77ed_A_7 = _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0.a;
            float4 _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2;
            Unity_Multiply_float4_float4(_SampleTexture2D_fe0d963923a94d97b5d0da26f130eab5_RGBA_0, _SampleTexture2D_b792db77a11748a88494980d164a77ed_RGBA_0, _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2);
            float _Split_57fa424231f8454c962dff75f2eb208a_R_1 = _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2[0];
            float _Split_57fa424231f8454c962dff75f2eb208a_G_2 = _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2[1];
            float _Split_57fa424231f8454c962dff75f2eb208a_B_3 = _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2[2];
            float _Split_57fa424231f8454c962dff75f2eb208a_A_4 = _Multiply_5c340ccb4c5f44d7b13f7b4357b90fc6_Out_2[3];
            surface.Alpha = _Split_57fa424231f8454c962dff75f2eb208a_R_1;
            return surface;
        }
        
        // --------------------------------------------------
        // Build Graph Inputs
        
        VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
        {
            VertexDescriptionInputs output;
            ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
            output.ObjectSpaceNormal =                          input.normalOS;
            output.ObjectSpaceTangent =                         input.tangentOS.xyz;
            output.ObjectSpacePosition =                        input.positionOS;
        
            return output;
        }
        SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
        {
            SurfaceDescriptionInputs output;
            ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
            
        
        
        
        
        
            output.uv0 = input.texCoord0;
            output.TimeParameters = _TimeParameters.xyz; // This is mainly for LW as HD overwrite this value
        #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN output.FaceSign =                    IS_FRONT_VFACE(input.cullFace, true, false);
        #else
        #define BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        #endif
        #undef BUILD_SURFACE_DESCRIPTION_INPUTS_OUTPUT_FACESIGN
        
                return output;
        }
        
        void BuildAppDataFull(Attributes attributes, VertexDescription vertexDescription, inout appdata_full result)
        {
            result.vertex     = float4(attributes.positionOS, 1);
            result.tangent    = attributes.tangentOS;
            result.normal     = attributes.normalOS;
            result.texcoord   = attributes.uv0;
            result.vertex     = float4(vertexDescription.Position, 1);
            result.normal     = vertexDescription.Normal;
            result.tangent    = float4(vertexDescription.Tangent, 0);
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
        }
        
        void VaryingsToSurfaceVertex(Varyings varyings, inout v2f_surf result)
        {
            result.pos = varyings.positionCS;
            // World Tangent isn't an available input on v2f_surf
        
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if UNITY_SHOULD_SAMPLE_SH
            #endif
            #if defined(LIGHTMAP_ON)
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogCoord = varyings.fogFactorAndVertexLight.x;
                COPY_TO_LIGHT_COORDS(result, varyings.fogFactorAndVertexLight.yzw);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(varyings, result);
        }
        
        void SurfaceVertexToVaryings(v2f_surf surfVertex, inout Varyings result)
        {
            result.positionCS = surfVertex.pos;
            // viewDirectionWS is never filled out in the legacy pass' function. Always use the value computed by SRP
            // World Tangent isn't an available input on v2f_surf
        
            #if UNITY_ANY_INSTANCING_ENABLED
            #endif
            #if UNITY_SHOULD_SAMPLE_SH
            #endif
            #if defined(LIGHTMAP_ON)
            #endif
            #ifdef VARYINGS_NEED_FOG_AND_VERTEX_LIGHT
                result.fogFactorAndVertexLight.x = surfVertex.fogCoord;
                COPY_FROM_LIGHT_COORDS(result.fogFactorAndVertexLight.yzw, surfVertex);
            #endif
        
            DEFAULT_UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(surfVertex, result);
        }
        
        // --------------------------------------------------
        // Main
        
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/Varyings.hlsl"
        #include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/Editor/ShaderGraph/Includes/DepthOnlyPass.hlsl"
        
        ENDHLSL
        }
    }
    CustomEditorForRenderPipeline "UnityEditor.Rendering.BuiltIn.ShaderGraph.BuiltInUnlitGUI" ""
    CustomEditor "UnityEditor.ShaderGraph.GenericShaderGraphMaterialGUI"
    FallBack "Hidden/Shader Graph/FallbackError"
}