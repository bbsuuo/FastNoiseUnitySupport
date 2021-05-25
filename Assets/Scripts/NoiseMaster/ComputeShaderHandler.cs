using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Engine.Rendering
{
    #region ComputeShaderHandler

    [System.Serializable]
    public abstract class ComputeShaderHandler<T> : IDisposable, ISerializationCallbackReceiver
    {
        [SerializeField]
        private ComputeShader computeShader;
        [SerializeField]
        private string kernelName;
        [SerializeField]
        private Vector3Int threadGroup;
        [SerializeField]
        private List<IComputerParameter> parameters = new List<IComputerParameter>();

        private double calculateTimeMs;
        private System.Diagnostics.Stopwatch stopwatch;
        public ComputeShaderHandler(ComputeShader computerShader, string kernelName, Vector3Int threadGroup)
        {
            this.computeShader = computerShader;
            this.kernelName = kernelName;
            this.threadGroup = threadGroup;
 
        }
        public ComputeShader ComputeShader { get => computeShader; set => computeShader = value; }
        public string KernelName { get => kernelName; set => kernelName = value; }
        public Vector3Int ThreadGroup { get => threadGroup; set => threadGroup = value; }
        public List<IComputerParameter> Parameters { get { return parameters; } set { parameters = value; } }

        public double CalculateTimeMs { get { return calculateTimeMs; } }

        public virtual void PlayWithReady(bool recordTime = true) {
 
            if (recordTime) {
                if (stopwatch == null) { stopwatch = new System.Diagnostics.Stopwatch(); }
                stopwatch.Restart();
            }
            int kernel = ComputeShader.FindKernel(KernelName);
            SetParameters(kernel, ComputeShader);
            GetThreadGroup();
            var threadGroup = GetThreadGroup();
            ComputeShader.Dispatch(kernel, threadGroup.x, threadGroup.y, threadGroup.z);
            AfterPlay();
            if (recordTime)
            {
                stopwatch.Stop();
                TimeSpan ts = stopwatch.Elapsed;
                calculateTimeMs = (ts.TotalMilliseconds);
            }

        }

        public abstract T GetResult();

        protected virtual void SetParameters(int kernel,ComputeShader computerShader) {
            if (Parameters == null) return;
            for (int i = 0; i < Parameters.Count; i++) {
                Parameters[i].Apply(kernel, computerShader, GetResolution());
            }
        }

        protected virtual int GetResolution() {
            return 0;
        }
        protected virtual Vector3Int GetThreadGroup() {
            return threadGroup;
        }

        protected virtual void AfterPlay() { }
        public virtual void Dispose() { 
        
        }

        public virtual bool IsVaild() {
            return ComputeShader != null;
        }
        [SerializeField]
        private string parameterJson;
        public virtual void OnBeforeSerialize()
        {
            if (Parameters != null && Parameters.Count > 0)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                for (int i = 0; i < Parameters.Count; i++)
                {
                    if (!CanSerializeParameter(parameters[i])) { continue; }
                    var typeParameter = parameters[i].GetType().ToString();
                    var parameter = JsonUtility.ToJson(parameters[i]);
                    sb.Append(typeParameter);
                    sb.Append('_');
                    sb.Append(parameter);
                    sb.Append('|');
                }
                if (sb.Length > 1)
                {
                    sb.Remove(sb.Length - 1, 1);
                    parameterJson = sb.ToString();
                }
            }
        }

        protected virtual bool CanSerializeParameter(IComputerParameter element ) { return true; }

        public virtual void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(parameterJson) &&(Parameters == null || Parameters.Count == 0))
            {
                Parameters = new List<IComputerParameter>();
                string[] parameters = parameterJson.Split('|');
                for (int i = 0; i < parameters.Length; i++)
                {
                    string[] kv = parameters[i].Split('_');
                    if (kv.Length != 2 || string.IsNullOrEmpty(kv[0]) || string.IsNullOrEmpty(kv[1])) continue;
                    Type type = Type.GetType(kv[0]);
                    if (type == null) continue;
                    var obj = JsonUtility.FromJson(kv[1], type) as IComputerParameter;
                    Parameters.Add(obj);
                }
            }
        }
    }

    [System.Serializable]
    public class Texture2DComputeShaderHandler : ComputeShaderHandler<RenderTexture>
    {
        [SerializeField]
        protected string textureShaderName;
        [SerializeField]
        protected int resolution;
        protected RenderTexture renderTexture;
        protected int textureShaderID;
 
        public Texture2DComputeShaderHandler(string textureShaderName,int resolution, ComputeShader computerShader, string kernelName, Vector3Int threadGroup)
        :base(computerShader,kernelName,threadGroup)
        {
            this.textureShaderName = textureShaderName;
            this.resolution = resolution;
            textureShaderID = Shader.PropertyToID(this.textureShaderName);
        }

        protected override int GetResolution()
        {
            return resolution;
        }
        protected virtual void CreateRenderTextureIfNeed()
        {
            if (renderTexture == null)
            {
                renderTexture = Utilities.Create2DRenderTexture(resolution);
            }
        }
        protected override void SetParameters(int kernel, ComputeShader computerShader)
        {
            CreateRenderTextureIfNeed();
            computerShader.SetTexture(kernel,textureShaderID,renderTexture);
            base.SetParameters(kernel, computerShader);
        }
        protected override Vector3Int GetThreadGroup()
        {
            return new Vector3Int(resolution / ThreadGroup.x, resolution / ThreadGroup.y,1);
        }
        public override void Dispose()
        {
            base.Dispose();
            if (renderTexture) { renderTexture.Release(); GameObject.DestroyImmediate(renderTexture); }
        }
        public override RenderTexture GetResult()
        {
            return renderTexture;
        }

        public virtual void ChangedResolution(int resolution) {
            if (renderTexture) { renderTexture.Release(); GameObject.DestroyImmediate(renderTexture); renderTexture = null; }
            this.resolution = resolution;
            CreateRenderTextureIfNeed();
        }
    }

    [System.Serializable]
    public class Texture3DComputeShaderHandler : Texture2DComputeShaderHandler
    {
        public Texture3DComputeShaderHandler(string textureShaderName, int resolution, ComputeShader computerShader, string kernelName, Vector3Int threadGroup)
    : base(textureShaderName, resolution,computerShader, kernelName, threadGroup)
        {
        }

        protected override void CreateRenderTextureIfNeed()
        {
            if (renderTexture == null)
            {
                renderTexture = Utilities.Create3DRenderTexture(resolution);
            }
        }
        protected override Vector3Int GetThreadGroup()
        {
            return new Vector3Int(resolution / ThreadGroup.x, resolution / ThreadGroup.y, resolution / ThreadGroup.z);
        }
    }

    [System.Serializable]
    public class NoiseTexture2DComputeShaderHandler : Texture2DComputeShaderHandler
    {
        [SerializeField]
        public bool autoUpdata;
        [SerializeField]
        protected NoiseState noiseState;
 
        public NoiseState NoiseState { get { return noiseState; } }
        public NoiseTexture2DComputeShaderHandler(string textureShaderName, int resolution, ComputeShader computerShader, string kernelName, Vector3Int threadGroup)
        : base(textureShaderName, resolution,computerShader, kernelName, threadGroup)
        {
            noiseState = new NoiseState();
            noiseState.OnValueChangedEvent += OnValueChanged;
            noiseState.OnKeywordsChangedEvent += OnKeywordsChanged;
        }

        void OnValueChanged() { if (autoUpdata) { PlayWithReady(); } }

        void OnKeywordsChanged() { if (!autoUpdata) { noiseState.SetKeywords(ComputeShader); } }

        protected override void SetParameters(int kernel, ComputeShader computeShader)
        {
            base.SetParameters(kernel, computeShader);
            noiseState.Apply(kernel, computeShader, resolution);
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            noiseState.OnValueChangedEvent += OnValueChanged;
            noiseState.OnKeywordsChangedEvent += OnKeywordsChanged;
        }


    }

    [System.Serializable]
    public class NoiseTexture3DComputeShaderHandler : Texture3DComputeShaderHandler
    {
        [SerializeField]
        public bool autoUpdata;
        [SerializeField]
        protected NoiseState noiseState;
        [SerializeField]
        protected int showLayer = 1;
        [SerializeField]
        public bool calculateShowRenderTexture;

        private RenderTexture showRenderTexture;
 
        public NoiseState NoiseState { get { return noiseState; } }
        public NoiseTexture3DComputeShaderHandler(string textureShaderName, int resolution, ComputeShader computerShader, string kernelName, Vector3Int threadGroup)
        : base(textureShaderName, resolution, computerShader, kernelName, threadGroup)
        {
            noiseState = new NoiseState();
            noiseState.OnValueChangedEvent += OnValueChanged;
            noiseState.OnKeywordsChangedEvent += OnKeywordsChanged;
        }

        void OnValueChanged() { if (autoUpdata) {   PlayWithReady(); } }

        void OnKeywordsChanged() { if (!autoUpdata) { noiseState.SetKeywords(ComputeShader); } }

        protected override void AfterPlay()
        {
            base.AfterPlay();
            if (calculateShowRenderTexture)
            {
                showRenderTexture = Utilities.Copy3DLayer(renderTexture, showRenderTexture, showLayer);
            }
        }

        public override void ChangedResolution(int resolution)
        {
            base.ChangedResolution(resolution);
            if (showRenderTexture) { showRenderTexture.Release(); GameObject.DestroyImmediate(showRenderTexture); showRenderTexture = null; }
        }

        public RenderTexture Get2DResult() {
            return showRenderTexture;
        }

        public override void Dispose()
        {
            base.Dispose();
            if (showRenderTexture) { showRenderTexture.Release(); GameObject.DestroyImmediate(showRenderTexture); }
        }
        protected override void SetParameters(int kernel, ComputeShader computeShader)
        {
            base.SetParameters(kernel, computeShader);
            noiseState.Apply(kernel, computeShader, resolution);
 
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            noiseState.OnValueChangedEvent += OnValueChanged;
            noiseState.OnKeywordsChangedEvent += OnKeywordsChanged;
        }
    }

    #endregion

    #region Parameter
    public interface IComputerParameter 
    {
        void Apply(int kernal,ComputeShader computeShader,int resolution);
    }

    [System.Serializable]
    public abstract class ComputeParameter : IComputerParameter , ISerializationCallbackReceiver
    {
        [SerializeField]
        public string shaderName;
        [System.NonSerialized]
        protected int shaderID;

 
        public ComputeParameter(string shaderName) {
            this.shaderName = shaderName;
            this.shaderID = Shader.PropertyToID(shaderName);
        }

        public abstract void Apply(int kernel, ComputeShader shader,int resolution);

        public void OnAfterDeserialize()
        {
            this.shaderID = Shader.PropertyToID(shaderName);
        }

        public void OnBeforeSerialize()
        {
          
        }
    }

    [System.Serializable]
    public class ComputeParameterFloat : ComputeParameter
    {
        public ComputeParameterFloat(string shaderName,float value) : base(shaderName) { this.value = value; }

        public float value;

        public override void Apply(int kernel, ComputeShader shader, int resolution)
        {
            shader.SetFloat(shaderID,value);
        }
    }

    [System.Serializable]
    public class ComputeRWTexture : ComputeParameter
    {
        public ComputeRWTexture(string shaderName, RenderTexture value) : base(shaderName) { this.value = value; }

        [HideInInspector]
        public RenderTexture value;

        public override void Apply(int kernel, ComputeShader shader, int resolution)
        {
            shader.SetTexture(kernel,shaderID, value);
        }
    }

    [System.Serializable]
    public class ComputeParameterInt  : ComputeParameter
    {
        public ComputeParameterInt(string shaderName, int value) : base(shaderName) { this.value = value; }

        public int value;

        public override void Apply(int kernel, ComputeShader shader, int resolution)
        {
            shader.SetInt(shaderID, value);
        }
    }
    [System.Serializable]
    public class ComputeParameterVector : ComputeParameter
    {
        public ComputeParameterVector(string shaderName, Vector4 value) : base(shaderName) { this.value = value; }

        public Vector4 value;

        public override void Apply(int kernel, ComputeShader shader, int resolution)
        {
            shader.SetVector(shaderID, value);
        }
    }
    #endregion
}