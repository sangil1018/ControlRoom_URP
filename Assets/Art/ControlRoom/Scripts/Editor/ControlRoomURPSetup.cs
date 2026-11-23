// Creates a URP asset + Universal Renderer if none is assigned, then runs the ControlRoom batch build.
// Batch: Unity.exe -batchmode -projectPath <proj> -executeMethod ControlRoomURPSetup.SetupAndBuild -quit
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ControlRoomURPSetup
{
    const string DIR = "Assets/ControlRoom/Settings";

    [MenuItem("ControlRoom/0. Setup URP Asset")]
    public static void EnsureURP()
    {
        if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset) { Debug.Log("[ControlRoom] URP already assigned"); return; }
        Directory.CreateDirectory(DIR);
        var rd = ScriptableObject.CreateInstance<UniversalRendererData>();
        rd.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
        AssetDatabase.CreateAsset(rd, DIR + "/ControlRoom_Renderer.asset");
        ResourceReloader.TryReloadAllNullIn(rd, UniversalRenderPipelineAsset.packagePath);
        var asset = UniversalRenderPipelineAsset.Create(rd);
        // light-weight defaults for a low-spec mini PC
        asset.msaaSampleCount = 2;
        asset.renderScale = 1f;
        asset.supportsHDR = true;
        asset.shadowDistance = 20f;
        asset.supportsCameraDepthTexture = false;
        asset.supportsCameraOpaqueTexture = false;
        AssetDatabase.CreateAsset(asset, DIR + "/ControlRoom_URP.asset");
        GraphicsSettings.defaultRenderPipeline = asset;
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = asset;
        }
        EditorUtility.SetDirty(rd); EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        Debug.Log("[ControlRoom] URP asset created and assigned");
    }

    public static void SetupAndBuild()
    {
        EnsureURP();
        ControlRoomBuilder.BatchBuild();
    }
}
