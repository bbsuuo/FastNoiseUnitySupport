using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Engine.Rendering
{
    public class NoiseGeneratorEditorWindow : EditorWindow
    {
        [MenuItem("Tools/Rendering/Noise Master")]
        public static NoiseGeneratorEditorWindow CreateWindows()
        {
            var instance = EditorWindow.CreateWindow<NoiseGeneratorEditorWindow>();
            instance.titleContent = new GUIContent("Noise Master");
            instance.Show();
            return instance;
        }

        public static NoiseGeneratorEditorWindow CreateWindowFromData(NoiseScriptableData data) {
            var instance = CreateWindows();
            instance.LoadFromData(data);
            return instance;
        }

 
        private GUIContent textureContent;
        private IntPopupSpace resolutionPop;
        private int resolution = 512;
        private int showResolution = 512;
        private NoiseSpace noiseSpace;
        private Vector2 scroll;
        private static string cachePath;


        private NoiseTexture2DComputeShaderHandler noise2DMaker;
        private NoiseTexture3DComputeShaderHandler noise3DMaker;
        private NoiseScriptableData noise2DMakerInstance;
        private NoiseScriptableData noise3DMakerInstance;
 
        public NoiseScriptableData SpaceMakerInstance { get { if (noiseSpace == NoiseSpace.Noise2D) return noise2DMakerInstance; else return noise3DMakerInstance; } }


        public NoiseState NoiseState
        {
            get
            {
                if (noiseSpace == NoiseSpace.Noise2D)
                    return noise2DMaker.NoiseState;
                else return noise3DMaker.NoiseState;
            }
        }
        public bool AutoUpdate
        {
            get
            {
                if (noiseSpace == NoiseSpace.Noise2D)
                    return noise2DMaker.autoUpdata;
                else return noise3DMaker.autoUpdata;
            }
            set
            {
                if (noiseSpace == NoiseSpace.Noise2D)
                    noise2DMaker.autoUpdata = value;
                else noise3DMaker.autoUpdata = value;
            }
        }
        public double CalculateTime
        {
            get
            {
                if (noiseSpace == NoiseSpace.Noise2D)
                    return noise2DMaker.CalculateTimeMs;
                else
                    return noise3DMaker.CalculateTimeMs;
            }
        }
        private Texture RenderTexture
        {
            get
            {
                if (noiseSpace == NoiseSpace.Noise2D)
                {
                    return noise2DMaker.GetResult();
                }
                else
                    return noise3DMaker.Get2DResult();
            }
        }

 

        private void OnEnable()
        {
 
        }

        private void OnDisable()
        {
            if (noise2DMaker != null) { noise2DMaker.Dispose(); noise2DMaker = null; }
            if (noise3DMaker != null) { noise3DMaker.Dispose(); noise3DMaker = null; }
        }

        void ReadyIfNeed(bool force = false)
        {
            if (noise2DMaker == null || !noise2DMaker.IsVaild() || force)
            {
                noise2DMaker = NoiseMaster.CreateNoise2DGenHandler(resolution);
                noise2DMaker.autoUpdata = true;
                noise2DMaker.PlayWithReady();
            }


            if (noise3DMaker == null || !noise3DMaker.IsVaild() || force)
            {
                noise3DMaker = NoiseMaster.CreateNoise3DGenHandler(resolution);
                noise3DMaker.autoUpdata = true;
                noise3DMaker.calculateShowRenderTexture = true;
                noise3DMaker.PlayWithReady();
            }


            if (textureContent == null) { textureContent = new GUIContent(RenderTexture); }
            if (resolutionPop == null) { resolutionPop = new IntPopupSpace(new string[6] { "32", "64", "128", "256", "512", "1024" }, new int[6] { 32, 64, 128, 256, 512, 1024 }); }
        }

        private void OnGUI()
        {
            ReadyIfNeed();
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.BeginVertical();
            EditorGUI.BeginChangeCheck();
            RenderToolbar();
            RenderNoiseTexture();
            GeneralOptions();
            FractaOptions();
            //DomainWarp();
            RenderButton();
            if (EditorGUI.EndChangeCheck())
            {

            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        void RenderToolbar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Load Data",EditorStyles.toolbarButton)) {
                string findPath = string.IsNullOrEmpty(cachePath) ? Application.dataPath : cachePath;
                string path = UnityEditor.EditorUtility.OpenFilePanel("Open Noise Maker", findPath, "asset");
                if (!string.IsNullOrEmpty(path)) {
                    var relativepath = "Assets" + path.Substring(Application.dataPath.Length);
                    var instance = AssetDatabase.LoadAssetAtPath<NoiseScriptableData>(relativepath);
                    LoadFromData(instance);
                    cachePath = path;
                }
            }

            GUILayout.Label("Resolution");
            var resolutionValue = resolutionPop.Draw(resolution, GUILayout.Width(50));
            if (resolutionValue != resolution)
            {
                if (noiseSpace == NoiseSpace.Noise3D && resolutionValue > 512)
                {
                    resolutionValue = 512;
                    Debug.LogError("3D Noise Not Support Resolution More Than 512");
                }
                resolution = resolutionValue;
                noise2DMaker.ChangedResolution(resolution);
                noise2DMaker.PlayWithReady();
                noise3DMaker.ChangedResolution(resolution);
                noise3DMaker.PlayWithReady();
            }
            GUILayout.Label("Show Resolution");
            showResolution = resolutionPop.Draw(showResolution, GUILayout.Width(50));
            GUILayout.Label("Noise Space");
            noiseSpace = (NoiseSpace)EditorGUILayout.EnumPopup(noiseSpace, EditorStyles.toolbarPopup, GUILayout.Width(80));
            GUILayout.Label("Auto Updata :");
            AutoUpdate = EditorGUILayout.Toggle(AutoUpdate, GUILayout.Width(10));
            GUILayout.Label("Invert :");
            NoiseState.InvertColor = EditorGUILayout.Toggle(NoiseState.InvertColor, GUILayout.Width(10));
            GUILayout.Label($"Time Spend : {CalculateTime} ms" );
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
        }

        void RenderNoiseTexture()
        {
            GUILayout.BeginHorizontal();
            textureContent.image = RenderTexture;
            float imageWidth = showResolution / 2f;
            GUILayout.Space(position.width / 2f - imageWidth);
            GUILayout.Box(textureContent, GUILayout.Width(showResolution), GUILayout.Height(showResolution));
 
            GUILayout.EndHorizontal();
        }

        void GeneralOptions()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("General");
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Data :", SpaceMakerInstance, SpaceMakerInstance == null ? typeof(NoiseScriptableData) : SpaceMakerInstance.GetType(),false);
            EditorGUI.EndDisabledGroup();

            NoiseState.NoiseType = (NoiseType)EditorGUILayout.EnumPopup("Noise Type :", NoiseState.NoiseType);
            NoiseState.Seed = EditorGUILayout.IntField("Seed :", NoiseState.Seed);
            NoiseState.Frequency = EditorGUILayout.Slider("Frequency", NoiseState.Frequency, 0, 1);
            if (noiseSpace == NoiseSpace.Noise3D)
            {
                NoiseState.RotationType = (Rotation3DType)EditorGUILayout.EnumPopup("Rotation Type 3D :", NoiseState.RotationType);
            }

            if (NoiseState.NoiseType == NoiseType.Cellular)
            {
                NoiseState.CellularDisType = (CellularDisType)EditorGUILayout.EnumPopup("Distance Function :", NoiseState.CellularDisType);
                NoiseState.CellularReturnType = (CellularReturnType)EditorGUILayout.EnumPopup("Return Type :", NoiseState.CellularReturnType);
                NoiseState.CellularJitterMod = EditorGUILayout.FloatField("Jitter:", NoiseState.CellularJitterMod);
            }

            NoiseState.Offset = EditorGUILayout.Vector3Field("Offset :", NoiseState.Offset);
            GUILayout.EndVertical();
        }

        void FractaOptions()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Fractal");
            NoiseState.FractalType = (FractalType)EditorGUILayout.EnumPopup("Fractal Type:", NoiseState.FractalType);
            if (NoiseState.FractalType != FractalType.None)
            {
                NoiseState.Octaves = EditorGUILayout.IntField("Octaves :", NoiseState.Octaves);
                NoiseState.Lacunarity = EditorGUILayout.FloatField("Lacunarity :", NoiseState.Lacunarity);
                NoiseState.Gain = EditorGUILayout.FloatField("Gain :", NoiseState.Gain);
                NoiseState.WeightedStrength = EditorGUILayout.FloatField("Weighted Strength :", NoiseState.WeightedStrength);
            }
            if (NoiseState.FractalType == FractalType.PingPong)
            {
                NoiseState.PingPongStrength = EditorGUILayout.FloatField("PingPong Strength :", NoiseState.PingPongStrength);
            }
            GUILayout.EndVertical();
        }

        void DomainWarp()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Domain Warp");
            NoiseState.DomainWarpType = (DomainWarpType)EditorGUILayout.EnumPopup("Domain Warp Type:", NoiseState.DomainWarpType);
            NoiseState.DomainWarpAmp = EditorGUILayout.FloatField("Amplitude :", NoiseState.DomainWarpAmp);
            GUILayout.EndVertical();
        }

        void RenderButton()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);

            if (GUILayout.Button("Update"))
            {
                noise2DMaker.PlayWithReady();
            }
            if (GUILayout.Button("Save As (Only Data)")) {
                OnSaveAsDataButtonClick();
            }
 
            if (GUILayout.Button("Save As (Only Texture)"))
            {
                OnSaveAsTextureButtonClick();
            }

            GUILayout.EndVertical();
        }

        void OnSaveAsDataButtonClick() {
            string fileName;
            if (noiseSpace == NoiseSpace.Noise2D)
            {
                fileName = string.Format("Noise_{0}_2D", noise2DMaker.NoiseState.NoiseType.ToString());
            }
            else {
                fileName = string.Format("Noise_{0}_3D", noise3DMaker.NoiseState.NoiseType.ToString());
            }

            string path = UnityEditor.EditorUtility.SaveFilePanel("Save As Noise Data ?",string.IsNullOrEmpty(cachePath) ? Application.dataPath : cachePath, fileName, "asset");
            if (!string.IsNullOrEmpty(path))
            {
                cachePath = path;
                var relativepath = "Assets" + path.Substring(Application.dataPath.Length);
                NoiseScriptableData instance = null;
                if (noiseSpace == NoiseSpace.Noise2D)
                {
                    instance = Noise2DScriptableData.CreateInstance<Noise2DScriptableData>();
                    (instance as Noise2DScriptableData).maker = noise2DMaker;
                }else if (noiseSpace == NoiseSpace.Noise3D)
                {
                    instance = Noise2DScriptableData.CreateInstance<Noise3DScriptableData>();
                    (instance as Noise3DScriptableData).maker = noise3DMaker;
                }
                if (instance != null)
                {
                    AssetDatabase.CreateAsset(instance, relativepath);
                    AssetDatabase.Refresh();
                }
            }
        }

        void OnSaveAsTextureButtonClick() {
            string findPath = string.IsNullOrEmpty(cachePath) ? Application.dataPath : cachePath;
            if (noiseSpace == NoiseSpace.Noise2D)
            {
                cachePath = Utilities.CallPanelToSaveTexture("Save As Texture2D ?", findPath,
                    string.Format("Noise_{0}_2D", noise2DMaker.NoiseState.NoiseType.ToString()), ()=> {  return Utilities.ConvertFromRenderTexture2D(noise2DMaker.GetResult()); });
            }

            if (noiseSpace == NoiseSpace.Noise3D)
            {
                cachePath = Utilities.CallPanelToSaveTexture("Save As Texture3D Png ?", findPath,
                    string.Format("Noise_{0}_3D", noise3DMaker.NoiseState.NoiseType.ToString()), ()=> { return Utilities.ConvertFromRenderTexture3D(noise3DMaker.GetResult()); });
            }
        }

        public void LoadFromData(NoiseScriptableData data) {
            var space = data.GetSpace();
            if (space == NoiseSpace.Noise2D)
            {
                noise2DMakerInstance = data;
                if (noise2DMaker != null) { noise2DMaker.Dispose(); noise2DMaker = null; }
                noise2DMaker = (data as Noise2DScriptableData).maker;
                noise2DMaker.PlayWithReady();
            }
            else
            {
                noise3DMakerInstance = data;
                if (noise3DMaker != null) { noise3DMaker.Dispose(); noise3DMaker = null; }
                noise3DMaker = (data as Noise3DScriptableData).maker;
                noise3DMaker.PlayWithReady();
            }
            this.noiseSpace = space;
        }
    }

    public class IntPopupSpace
    {
        public string[] options;
        public int[] values;

        public IntPopupSpace(string[] options, int[] values)
        {
            this.options = options;
            this.values = values;
        }

        public int Draw(int selected, params GUILayoutOption[] layoutOptions)
        {
            return EditorGUILayout.IntPopup(selected, options, values, EditorStyles.toolbarPopup, layoutOptions);
        }
        public int Draw(string label, int selected, params GUILayoutOption[] layoutOptions)
        {
            return EditorGUILayout.IntPopup(label, selected, options, values, EditorStyles.toolbarPopup, layoutOptions);
        }
    }
}