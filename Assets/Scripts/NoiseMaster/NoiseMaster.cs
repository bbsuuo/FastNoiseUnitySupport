using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Engine.Rendering
{

    public static class NoiseMaster
    {
        public static NoiseTexture2DComputeShaderHandler CreateNoise2DGenHandler(int resolution)
        {
            var computeShader = Utilities.LoadComputeShader(Utilities.NoiseComputerShaderPath);
            NoiseTexture2DComputeShaderHandler csHandler = new NoiseTexture2DComputeShaderHandler("Result", resolution, computeShader, "Noise2DGen", new Vector3Int(8, 8, 1));
            return csHandler;
        }

        public static NoiseTexture3DComputeShaderHandler CreateNoise3DGenHandler(int resolution)
        {
            var computeShader = Utilities.LoadComputeShader(Utilities.NoiseComputerShaderPath);
            NoiseTexture3DComputeShaderHandler csHandler = new NoiseTexture3DComputeShaderHandler("Result3D", resolution, computeShader, "Noise3DGen", new Vector3Int(8, 8, 8));
            return csHandler;
        }


 
    }

    public enum NoiseSpace { Noise2D, Noise3D }
    public enum NoiseType { Open_Simplex_2 = 0, Open_Simplex_2S = 1, Cellular = 2, Perlin = 3, Value_Cubic = 4, Value = 5 }

    public enum Rotation3DType { None = 0, Rotation_Improve_XY_Planes = 1, Rotation_Improve_XZ_Planes = 2 }

    public enum FractalType { None = 0, FBM = 1, Ridged = 2, PingPong = 3, Domain_Warp_Progessive = 4, Domain_Warp_Independent = 5 }

    public enum CellularDisType { Euclidean = 0, EuclideanSquare = 1, Manhattan = 2, Hybrid = 3 }

    public enum CellularReturnType { CellValue = 0, Distance = 1, Distance2 = 2, Distance2Add = 3, Distance2Sub = 4, Distance2Mul = 5, Distance2DIV = 6 }

    public enum DomainWarpType { OpenSimplex2 = 0, OpenSimplex2_Reduced = 1, Basicgid = 2 }

    [System.Serializable]
    public class NoiseState : IComputerParameter
    {
        #region Properties

        [SerializeField]
        private int seed = 0;
        [SerializeField]
        private float frequency = 0.01f;
        [SerializeField]
        private int octaves = 3;
        [SerializeField]
        private float lacunarity = 2.0f;
        [SerializeField]
        private float gain = 0.5f;
        [SerializeField]
        private float weightedStrength = 0.0f;
        [SerializeField]
        private float pingPongStrength = 2.0f;
        [SerializeField]
        private float cellularJitterMod = 1.0f;
        [SerializeField]
        private float domainWarpAmp = 30.0f;
        [SerializeField]
        private NoiseType noiseType = NoiseType.Perlin;
        [SerializeField]
        private Rotation3DType rotationType = Rotation3DType.None;
        [SerializeField]
        private FractalType fractalType = FractalType.None;
        [SerializeField]
        private CellularDisType cellularDisType = CellularDisType.EuclideanSquare;
        [SerializeField]
        private CellularReturnType cellularReturnType = CellularReturnType.Distance;
        [SerializeField]
        private DomainWarpType domainWarpType = DomainWarpType.OpenSimplex2;
        [SerializeField]
        private bool invertColor;
        [SerializeField]
        private Vector3 offset;
        public int Seed { get { return seed; } set { if (seed != value) { seed = value; OnValueChanged(); } } }
        public float Frequency { get { return frequency; } set { if (frequency != value) { frequency = value; OnValueChanged(); } } }
        public int Octaves { get { return octaves; } set { if (octaves != value) { octaves = value; OnValueChanged(); } } }
        public float Lacunarity { get { return lacunarity; } set { if (lacunarity != value) { lacunarity = value; OnValueChanged(); } } }
        public float Gain { get { return gain; } set { if (gain != value) { gain = value; OnValueChanged(); } } }
        public float WeightedStrength { get { return weightedStrength; } set { if (weightedStrength != value) { weightedStrength = value; OnValueChanged(); } } }
        public float PingPongStrength { get { return pingPongStrength; } set { if (pingPongStrength != value) { pingPongStrength = value; OnValueChanged(); } } }
        public float CellularJitterMod { get { return cellularJitterMod; } set { if (cellularJitterMod != value) { cellularJitterMod = value; OnValueChanged(); } } }
        public float DomainWarpAmp { get { return domainWarpAmp; } set { if (domainWarpAmp != value) { domainWarpAmp = value; OnValueChanged(); } } }
        public NoiseType NoiseType { get { return noiseType; } set { if (noiseType != value) { noiseType = value; OnValueChanged(); OnKeywordsValueChanged(); } } }
        public Vector3 Offset { get { return offset; } set { if (offset != value) { offset = value; OnValueChanged(); } } }
        public Rotation3DType RotationType
        {
            get { return rotationType; }
            set { if (rotationType != value) { rotationType = value; OnValueChanged();} }
        }
        public FractalType FractalType
        {
            get { return fractalType; }
            set
            {
                if (fractalType != value)
                {
                    fractalType = value; OnValueChanged();
                    OnKeywordsValueChanged();
                }
            }
        }
        public CellularDisType CellularDisType
        {
            get { return cellularDisType; }
            set
            {
                if (cellularDisType != value)
                {
                    cellularDisType = value; OnValueChanged();
                    OnKeywordsValueChanged();
                }
            }
        }
        public CellularReturnType CellularReturnType
        {
            get
            {
                return cellularReturnType;
            }
            set
            {
                if (cellularReturnType != value)
                {
                    cellularReturnType = value;
                    OnValueChanged();
                }
            }
        }
        public DomainWarpType DomainWarpType
        {
            get
            {
                return domainWarpType;
            }
            set
            {
                if (domainWarpType != value)
                {
                    domainWarpType = value;
                    OnValueChanged();
                }
            }
        }

        public bool InvertColor 
        {
            get {
                return invertColor;
            }
            set {
                if (invertColor != value) {
                    invertColor = value;
                    OnValueChanged();
                    OnKeywordsValueChanged();
                }
            }
        }

        public System.Action OnValueChangedEvent;
        public System.Action OnKeywordsChangedEvent;
        #endregion
       
        public void Apply(int kernal, ComputeShader computeShader,int resolution)
        {
            SetKeywords(computeShader);

            computeShader.SetInt(Utilities.ShaderLibrary.SeedShaderId, seed);
            computeShader.SetFloat(Utilities.ShaderLibrary.FrequencyShaderId, frequency);
            computeShader.SetInt(Utilities.ShaderLibrary.OctavesShaderId, octaves);
            computeShader.SetFloat(Utilities.ShaderLibrary.LacunarityShaderId, lacunarity);
            computeShader.SetFloat(Utilities.ShaderLibrary.GainShaderId, gain);
            computeShader.SetFloat(Utilities.ShaderLibrary.WeightedStrengthShaderId, weightedStrength);
            computeShader.SetFloat(Utilities.ShaderLibrary.PingPongStrengthShaderId, pingPongStrength);
            computeShader.SetInt(Utilities.ShaderLibrary.RotationTypeShaderId, (int)rotationType);
            computeShader.SetInt(Utilities.ShaderLibrary.CellularReturnTypeShaderId, (int)cellularReturnType);
            computeShader.SetFloat(Utilities.ShaderLibrary.CellularJitterModShaderId, cellularJitterMod);
            computeShader.SetInt(Utilities.ShaderLibrary.DomainWarpTypeShaderId, (int)domainWarpType);
            computeShader.SetFloat(Utilities.ShaderLibrary.DomainWarpAmpShaderId, domainWarpAmp);
            computeShader.SetVector(Utilities.ShaderLibrary.NoiseOffsetShaderId,new Vector4(offset.x,offset.y,offset.z, resolution));
        }

        public void SetKeywords(ComputeShader computeShader)
        {
            var noiseTypeKeywords = Utilities.ShaderLibrary.NoiseTypeKeywords;
            computeShader.SetKeywordsByArray(noiseTypeKeywords, (int)noiseType - 1);
            var fractalTypeKeywords = Utilities.ShaderLibrary.FractalTypeKeywords;
            computeShader.SetKeywordsByArray(fractalTypeKeywords, (int)fractalType - 1);
            var cellularDisKeywords = Utilities.ShaderLibrary.CellularDisTypeKeywords;
            computeShader.SetKeywordsByArray(cellularDisKeywords, (int)cellularDisType - 1);
 
            if (invertColor)
            {
                computeShader.EnableKeyword(Utilities.ShaderLibrary.InvertValueKeyword);
            }
            else {
                computeShader.DisableKeyword(Utilities.ShaderLibrary.InvertValueKeyword);
            }
 
        }


        void OnValueChanged() { OnValueChangedEvent?.Invoke(); }

        void OnKeywordsValueChanged() { OnKeywordsChangedEvent?.Invoke(); }

    }


}
