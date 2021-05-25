using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Engine.Rendering
{
    [CustomEditor(typeof(NoiseScriptableData),true)]
    public class NoiseScriptableDataInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Noise Master Panel")) {
               var window = NoiseGeneratorEditorWindow.CreateWindowFromData(target as NoiseScriptableData);
            }
        }
    }

}