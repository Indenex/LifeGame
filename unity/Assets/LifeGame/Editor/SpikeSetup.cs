using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LifeGame.Spike.Editor
{
    // Sets the project up the first time it opens (unity-1), so nothing hand-written in
    // YAML can break: linear colour, a URP asset tuned to match the Godot client
    // (HDR, MSAA 4x, 60 m of 4-cascade soft shadows like Godot's sun, SSAO, depth
    // texture for the water), and Assets/LifeGame/Spike.unity with the SpikeWorld
    // component. Run it again from the menu: LifeGame > Set up spike.
    [InitializeOnLoad]
    static class SpikeSetup
    {
        const string SettingsDir = "Assets/LifeGame/Settings";
        const string AssetPath = SettingsDir + "/SpikeURP.asset";
        const string RendererPath = SettingsDir + "/SpikeURP_Renderer.asset";
        const string ScenePath = "Assets/LifeGame/Spike.unity";

        static SpikeSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ScenePath) || GraphicsSettings.defaultRenderPipeline == null)
                    Setup();
            };
        }

        [MenuItem("LifeGame/Set up spike")]
        public static void Setup()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            Directory.CreateDirectory(SettingsDir);

            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetPath);
            if (asset == null)
            {
                asset = UniversalRenderPipelineAsset.Create();
                AssetDatabase.CreateAsset(asset, AssetPath);
                // URP's own way to make a renderer with its default post-process resources.
                asset.LoadBuiltinRendererData();
                AssetDatabase.SaveAssets();
                AssetDatabase.MoveAsset("Assets/UniversalRenderer.asset", RendererPath);
            }
            asset.supportsHDR = true;
            asset.msaaSampleCount = 4;
            asset.supportsCameraDepthTexture = true;
            asset.supportsMainLightShadows = true;
            asset.mainLightShadowmapResolution = 4096;
            asset.shadowDistance = 60f;           // Godot: directional_shadow_max_distance 60
            asset.shadowCascadeCount = 4;
            asset.supportsSoftShadows = true;
            asset.additionalLightsRenderingMode = LightRenderingMode.PerPixel;
            asset.colorGradingMode = ColorGradingMode.HighDynamicRange;
            asset.colorGradingLutSize = 64;       // also the Godot-grade LUT's size
            EditorUtility.SetDirty(asset);

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (rendererData != null) AddSsao(rendererData);

            GraphicsSettings.defaultRenderPipeline = asset;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
            }
            QualitySettings.SetQualityLevel(current, false);
            IncludeShaders();
            AssetDatabase.SaveAssets();

            if (!File.Exists(ScenePath))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camGo = new GameObject("Camera", typeof(Camera));
                camGo.tag = "MainCamera";
                camGo.AddComponent<UniversalAdditionalCameraData>();
                new GameObject("Spike", typeof(SpikeWorld));
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath);
            Debug.Log("[spike] Set up: URP asset, SSAO, linear colour and Spike.unity. Press Play.");
        }

        // Godot: ssao_enabled, radius 1, intensity 3, light_affect 0 (it darkens only
        // the ambient light). URP's SSAO, tuned to sit near that.
        static void AddSsao(UniversalRendererData data)
        {
            foreach (var f in data.rendererFeatures)
                if (f is ScreenSpaceAmbientOcclusion) return;
            var ssao = ScriptableObject.CreateInstance<ScreenSpaceAmbientOcclusion>();
            ssao.name = "SSAO";
            AssetDatabase.AddObjectToAsset(ssao, data);
            var so = new SerializedObject(ssao);
            so.FindProperty("m_Settings.Intensity").floatValue = 1.5f;
            so.FindProperty("m_Settings.Radius").floatValue = 0.5f;
            so.FindProperty("m_Settings.DirectLightingStrength").floatValue = 0f;
            so.FindProperty("m_Settings.Falloff").floatValue = 60f;
            so.ApplyModifiedPropertiesWithoutUndo();

            var dso = new SerializedObject(data);
            var features = dso.FindProperty("m_RendererFeatures");
            var map = dso.FindProperty("m_RendererFeatureMap");
            features.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = ssao;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ssao, out _, out long localId);
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            dso.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
        }

        // Shader.Find needs the shaders in a player build too.
        static void IncludeShaders()
        {
            var gs = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
            if (gs == null) return;
            var so = new SerializedObject(gs);
            var list = so.FindProperty("m_AlwaysIncludedShaders");
            foreach (var name in new[] { "LifeGame/Painted", "LifeGame/Grass", "LifeGame/Water", "LifeGame/Chibi", "LifeGame/Sky" })
            {
                var shader = Shader.Find(name);
                if (shader == null) continue;
                bool found = false;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader) found = true;
                if (found) continue;
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
