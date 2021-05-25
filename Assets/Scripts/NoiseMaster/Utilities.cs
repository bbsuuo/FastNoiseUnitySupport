using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace Engine.Rendering
{
    public static class Utilities
    {
        #region Resources
        public const string NoiseComputerShaderPath = "Assets/Scripts/NoiseMaster/Shader/Compute/NoiseLit.compute";
        public const string Copy3DComputerShaderPath = "Assets/Scripts/NoiseMaster/Shader/Compute/CopyTexture3D.compute";
        public static ComputeShader LoadComputeShader(string path)
        {
#if UNITY_EDITOR
            var cs = UnityEditor.AssetDatabase.LoadAssetAtPath<ComputeShader>(path);
            return cs;
#endif
            throw new System.NotImplementedException();
        }
        #endregion

        #region Helpful
        public static void SetKeywordsByArray(this ComputeShader cs, string[] keywords, int selected)
        {

            for (int i = 0; i < keywords.Length; i++)
            {
                if (i == selected)
                {
                    if (!cs.IsKeywordEnabled(keywords[i]))
                        cs.EnableKeyword(keywords[i]);
                }
                else
                {
                    cs.DisableKeyword(keywords[i]);
                }
            }
        }
        #endregion

        #region Shader
        internal static class ShaderLibrary 
        {
            public static int SeedShaderId = Shader.PropertyToID("seed");
            public static int FrequencyShaderId = Shader.PropertyToID("frequency");
            public static int OctavesShaderId = Shader.PropertyToID("octaves");
            public static int LacunarityShaderId = Shader.PropertyToID("lacunarity");
            public static int GainShaderId = Shader.PropertyToID("gain");
            public static int WeightedStrengthShaderId = Shader.PropertyToID("weightedStrength");
            public static int PingPongStrengthShaderId = Shader.PropertyToID("pingPongStrength");
            public static int NoiseTypeShaderId = Shader.PropertyToID("noiseType");
            public static int RotationTypeShaderId = Shader.PropertyToID("rotationType");
            public static int FractalTypeShaderId = Shader.PropertyToID("fractalType");
            public static int CellularDisTypeShaderId = Shader.PropertyToID("cellularDisType");
            public static int CellularReturnTypeShaderId = Shader.PropertyToID("cellularReturnType");
            public static int CellularJitterModShaderId = Shader.PropertyToID("cellularJitterMod");
            public static int DomainWarpTypeShaderId = Shader.PropertyToID("domainWarpType");
            public static int DomainWarpAmpShaderId = Shader.PropertyToID("domainWarpAmp");
            public static int NoiseOffsetShaderId = Shader.PropertyToID("noiseOffsetWithResolution");

            public static string[] NoiseTypeKeywords = new string[5] { "_OPENSIMPLEX2S", "_CELLULAR", "_PERLIN", "_VALUE_CUBIC", "_VALUE" };
            public static string[] FractalTypeKeywords = new string[5] { "_FractalFBM", "_FractalRIDGED", "_FractalPINGPONG", "_Fractal_DOMAIN_WARP_PROGRESSIVE", "_Fractal_DOMAIN_WARP_INDEPENDENT"};
            public static string[] CellularDisTypeKeywords = new string[3] { "_CELLULAR_EUCLIDEANSQ", "_CELLULAR_MANHATTAN", "_CELLULAR_HYBRID" };
            public static string InvertValueKeyword = "_InvertValue";
        }
        private static ComputeShader copyToTexture3D; 
        private static ComputeShader CopyToTexture3D
        {
            get { if (copyToTexture3D == null) { copyToTexture3D = LoadComputeShader(Copy3DComputerShaderPath); } return copyToTexture3D; }
        }
        #endregion

        #region Texture 
        public static RenderTexture Create2DRenderTexture(int resolution)
        {
            RenderTexture rt = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
            rt.enableRandomWrite = true;
            rt.Create();
            return rt;
        }

        public static RenderTexture Create3DRenderTexture(int resolution)
        {
            RenderTexture rt = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32);
            rt.enableRandomWrite = true;
            rt.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            rt.volumeDepth = resolution;
            rt.Create();
            return rt;
        }

        public static Texture2D ConvertFromRenderTexture2D(RenderTexture rt)
        {
            Texture2D output = new Texture2D(rt.width, rt.height);
            RenderTexture.active = rt;
            output.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            output.Apply();
            return output;
        }

        public static Texture3D ConvertFromRenderTexture3D(RenderTexture rt) {
            if (rt.dimension != UnityEngine.Rendering.TextureDimension.Tex3D) throw new Exception("Render Texture Type must be a tex3D");
            int resolution = rt.width;
            RenderTexture[] layers = new RenderTexture[resolution];
            for (int i = 0; i < resolution; i++)
            {
                layers[i] = CreateRenderTextureCopy3DLayer(rt, i);
            }
            //Write RenderTexture slices to static textures
            Texture2D[] finalSlices = new Texture2D[resolution];
            for (int i = 0; i < resolution; i++)
            {
                finalSlices[i] = ConvertFromRenderTexture2D(layers[i]);
            }
            //Build 3D Texture from 2D slices
            Texture3D output = new Texture3D(resolution, resolution, resolution, TextureFormat.ARGB32, true);
            output.filterMode = FilterMode.Trilinear;
            Color[] outputPixels = output.GetPixels();
            for (int k = 0; k < resolution; k++)
            {
                Color[] layerPixels = finalSlices[k].GetPixels();
                for (int i = 0; i < resolution; i++)
                {
                    for (int j = 0; j < resolution; j++)
                    {
                        outputPixels[i + j * resolution + k * resolution * resolution] = layerPixels[i + j * resolution];
                    }
                }
            }
            output.SetPixels(outputPixels);
            output.Apply();
            return output;
        }

        public static RenderTexture CreateRenderTextureCopy3DLayer(RenderTexture rt,int layer) {
            if (rt.dimension != UnityEngine.Rendering.TextureDimension.Tex3D) throw new Exception("Render Texture Type must be a tex3D");
            int resolution = rt.width;
            RenderTexture layerRt = Create2DRenderTexture(resolution);
            return Copy3DLayer(rt,layerRt,layer);
        }

        public static RenderTexture Copy3DLayer(RenderTexture source, RenderTexture des, int layer) {
            if (des == null) { return CreateRenderTextureCopy3DLayer(source,layer);}
            int resolution = source.width;
            int kernelIndex = CopyToTexture3D.FindKernel("CopyLayer");
            CopyToTexture3D.SetTexture(kernelIndex, "Source", source);
            CopyToTexture3D.SetInt("layer", layer);
            CopyToTexture3D.SetTexture(kernelIndex, "Destination", des);
            CopyToTexture3D.Dispatch(kernelIndex, resolution, resolution, 1);
            return des;
        }


        public static void WriteTexture2D(string path, Texture2D texture)
        {
            byte[] bytes = texture.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh();
#endif
        }

#if UNITY_EDITOR
        public static string CallPanelToSaveTexture(string title,string directiory,string defaultFileName ,Func<Texture> actionCallback)
        {
            string path = UnityEditor.EditorUtility.SaveFilePanel(title, directiory, defaultFileName, "asset");
            if (!string.IsNullOrEmpty(path))
            {
               var relativepath = "Assets" + path.Substring(Application.dataPath.Length);
               UnityEditor.AssetDatabase.CreateAsset(actionCallback?.Invoke(), relativepath);
               UnityEditor.AssetDatabase.Refresh();
            }
            return path;
        }
#endif
        #endregion
    }
}