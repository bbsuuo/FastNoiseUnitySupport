using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Engine.Rendering
{
    public abstract class NoiseScriptableData : ScriptableObject {
        public NoiseSpace GetSpace() {
            if (this is Noise2DScriptableData)
            {
                return NoiseSpace.Noise2D;
            }
            else {
                return NoiseSpace.Noise3D;
            }
        }

 
    }

    public class Noise2DScriptableData : NoiseScriptableData
    {
        public NoiseTexture2DComputeShaderHandler maker; 
    }

    public class Noise3DScriptableData : NoiseScriptableData
    {
        public NoiseTexture3DComputeShaderHandler maker;
    }
}