// ControlRoom project builder (generated). Menu: ControlRoom > Build All
// Batch: Unity.exe -batchmode -projectPath <proj> -executeMethod ControlRoomBuilder.BatchBuild -quit
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class ControlRoomBuilder
{
    const string ROOT = "Assets/ControlRoom";
    const string SCENE = ROOT + "/Scenes/ControlRoom.unity";
    const float LED_INTENSITY = 40f;   // baked rect lights, tune here

    [System.Serializable] class MatItem { public string name; public float[] baseColor; public float metallic; public float smoothness; public bool hasEmission; public float[] emission; public float emissionIntensity; public string texture; public float tiling; public bool cutout; public bool screen; }
    [System.Serializable] class MatList { public MatItem[] items; }
    [System.Serializable] class ScrItem { public string name; public int group; public string tier; public int frames; public float width; public float height; public string material; }
    [System.Serializable] class ScrList { public ScrItem[] items; }
    [System.Serializable] class MapItem { public string obj; public string asset; }
    [System.Serializable] class MapList { public MapItem[] items; }

    static string Log = "";
    static void L(string s) { Debug.Log("[ControlRoom] " + s); Log += s + "\n"; }

    public static void BatchBuild()
    {
        BuildAll();
        BakeLighting();
        Screenshots();
        File.WriteAllText(Path.Combine(Application.dataPath, "../BuildLog_ControlRoom.txt"), Log);
    }

    [MenuItem("ControlRoom/1. Build All (materials, prefabs, scene)")]
    public static void BuildAll()
    {
        PlayerSettings.colorSpace = ColorSpace.Linear;
        if (GraphicsSettings.defaultRenderPipeline == null) L("WARNING: no URP asset assigned in Graphics settings");
        else L("Render pipeline: " + GraphicsSettings.defaultRenderPipeline.name);
        ConfigureImporters();
        var mats = BuildMaterials();
        BuildPrefabs(mats);
        BuildScene(mats);
    }

    // ---------------------------------------------------------------- importers
    static void ConfigureImporters()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ROOT + "/FBX" }))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            var mi = AssetImporter.GetAtPath(p) as ModelImporter; if (mi == null) continue;
            bool layout = p.EndsWith("Layout.fbx");
            bool screen = p.Contains("Screen");
            mi.globalScale = 1f; mi.useFileScale = true;
            mi.importCameras = false; mi.importLights = false; mi.importAnimation = false;
            mi.animationType = ModelImporterAnimationType.None;
            mi.importNormals = ModelImporterNormals.Import;
            mi.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            mi.materialLocation = ModelImporterMaterialLocation.InPrefab;
            mi.generateSecondaryUV = !layout && !screen;
            mi.SaveAndReimport();
        }
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ROOT + "/Textures" }))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            var ti = AssetImporter.GetAtPath(p) as TextureImporter; if (ti == null) continue;
            bool scr = p.Contains("/Screens/");
            ti.sRGBTexture = true;
            ti.wrapMode = scr ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            ti.alphaIsTransparency = p.Contains("Chair_Mesh");
            ti.mipmapEnabled = true;
            ti.anisoLevel = scr ? 4 : 2;
            ti.maxTextureSize = scr ? 1024 : 1024;
            ti.SaveAndReimport();
        }
        L("Importers configured");
    }

    static Texture2D FindTex(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets(name + " t:Texture2D", new[] { ROOT + "/Textures" }))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(p) == name) return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
        }
        L("WARNING: texture not found " + name); return null;
    }

    // ---------------------------------------------------------------- materials
    static Dictionary<string, Material> BuildMaterials()
    {
        var dict = new Dictionary<string, Material>();
        var list = JsonUtility.FromJson<MatList>(File.ReadAllText(ROOT + "/Data/materials_unity.json"));
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        Directory.CreateDirectory(ROOT + "/Materials");
        foreach (var it in list.items)
        {
            string path = ROOT + "/Materials/" + it.name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(lit); AssetDatabase.CreateAsset(m, path); }
            m.shader = lit;
            var c = new Color(it.baseColor[0], it.baseColor[1], it.baseColor[2], 1f);
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Metallic", it.metallic);
            m.SetFloat("_Smoothness", Mathf.Clamp01(it.smoothness));
            m.enableInstancing = true;
            Texture2D tex = string.IsNullOrEmpty(it.texture) ? null : FindTex(it.texture);
            if (it.screen)
            {
                m.SetColor("_BaseColor", new Color(0.01f, 0.01f, 0.012f, 1f));
                m.SetFloat("_Smoothness", 0.8f);
                if (tex != null) m.SetTexture("_EmissionMap", tex);
                m.SetColor("_EmissionColor", Color.white);
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }
            else
            {
                if (tex != null)
                {
                    m.SetTexture("_BaseMap", tex);
                    m.SetTextureScale("_BaseMap", new Vector2(it.tiling, it.tiling));
                    m.SetColor("_BaseColor", Color.white);
                }
                if (it.hasEmission)
                {
                    var e = new Color(it.emission[0], it.emission[1], it.emission[2]) * Mathf.Max(1f, it.emissionIntensity * 0.6f);
                    m.SetColor("_EmissionColor", e);
                    m.EnableKeyword("_EMISSION");
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                }
            }
            if (it.cutout)
            {
                m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.SetFloat("_Cull", 0f); m.doubleSidedGI = true;
                m.renderQueue = (int)RenderQueue.AlphaTest;
            }
            EditorUtility.SetDirty(m);
            dict[it.name] = m;
        }
        AssetDatabase.SaveAssets();
        L("Materials: " + dict.Count);
        return dict;
    }

    static int Remap(GameObject go, Dictionary<string, Material> mats)
    {
        int miss = 0;
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var arr = r.sharedMaterials;
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == null) continue;
                var n = arr[i].name.Replace(" (Instance)", "");
                int dot = n.IndexOf('.'); if (dot > 0) n = n.Substring(0, dot);
                if (mats.TryGetValue(n, out var m)) arr[i] = m; else { miss++; L("WARNING: no material for " + n + " on " + r.name); }
            }
            r.sharedMaterials = arr;
        }
        return miss;
    }

    static void SetStatic(GameObject go)
    {
        var f = StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.ReflectionProbeStatic;
        foreach (var t in go.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(t.gameObject, f);
    }

    // ---------------------------------------------------------------- prefabs
    static void BuildPrefabs(Dictionary<string, Material> mats)
    {
        Directory.CreateDirectory(ROOT + "/Prefabs");
        int n = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ROOT + "/FBX/Props" }))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(p);
            var name = Path.GetFileNameWithoutExtension(p);
            var wrap = new GameObject(name);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.transform.SetParent(wrap.transform, false);
            inst.name = name + "_Mesh";
            Remap(wrap, mats);
            SetStatic(wrap);
            PrefabUtility.SaveAsPrefabAsset(wrap, ROOT + "/Prefabs/" + name + ".prefab");
            Object.DestroyImmediate(wrap); n++;
        }
        L("Prefabs: " + n);
    }

    static MeshRenderer FirstRenderer(GameObject go) { return go.GetComponentInChildren<MeshRenderer>(true); }

    static Transform Child(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t == null) { t = new GameObject(name).transform; t.SetParent(parent, false); }
        return t;
    }

    // ---------------------------------------------------------------- scene
    static void BuildScene(Dictionary<string, Material> mats)
    {
        Directory.CreateDirectory(ROOT + "/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("ControlRoom").transform;

        var map = new Dictionary<string, string>();
        foreach (var it in JsonUtility.FromJson<MapList>(File.ReadAllText(ROOT + "/Data/props_map.json")).items) map[it.obj] = it.asset;
        var scr = new Dictionary<string, ScrItem>();
        foreach (var it in JsonUtility.FromJson<ScrList>(File.ReadAllText(ROOT + "/Data/screens.json")).items) scr[it.name] = it;

        // Reference FBX gives exact world placement of every object
        var refModel = AssetDatabase.LoadAssetAtPath<GameObject>(ROOT + "/FBX/Indoor_Full_Reference.fbx");
        var refInst = (GameObject)Object.Instantiate(refModel);
        var refByName = new Dictionary<string, Transform>();
        foreach (var t in refInst.GetComponentsInChildren<Transform>(true)) refByName[t.name] = t;

        // room shell (static, merged per material later by static batching)
        var shellModel = AssetDatabase.LoadAssetAtPath<GameObject>(ROOT + "/FBX/Room_Shell.fbx");
        var shell = (GameObject)PrefabUtility.InstantiatePrefab(shellModel);
        PrefabUtility.UnpackPrefabInstance(shell, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        shell.name = "Room_Shell"; shell.transform.SetParent(root, true);
        Remap(shell, mats); SetStatic(shell);

        // props: prefab instance placed so its renderer matches the reference renderer exactly
        int placed = 0, missing = 0; float maxErr = 0;
        foreach (var kv in map)
        {
            if (!refByName.TryGetValue(kv.Key, out var rt)) { missing++; L("WARNING: ref missing " + kv.Key); continue; }
            var pf = AssetDatabase.LoadAssetAtPath<GameObject>(ROOT + "/Prefabs/" + kv.Value + ".prefab");
            if (pf == null) { missing++; L("WARNING: prefab missing " + kv.Value); continue; }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(pf);
            string grp = kv.Key.Substring(0, kv.Key.IndexOf('_', 3));
            var parent = Child(Child(root, grp.StartsWith("OP_") ? "OperatorDesks" : "Stations"), grp);
            go.transform.SetParent(parent, false);
            go.name = kv.Key;
            var pr = FirstRenderer(go).transform;
            // world(wrapper) = refWorld * inverse(meshLocalInWrapper)
            Matrix4x4 local = go.transform.worldToLocalMatrix * pr.localToWorldMatrix;
            Matrix4x4 w = rt.localToWorldMatrix * local.inverse;
            go.transform.position = w.GetColumn(3);
            go.transform.rotation = Quaternion.LookRotation(w.GetColumn(2), w.GetColumn(1));
            float err = Vector3.Distance(FirstRenderer(go).bounds.center, rt.GetComponent<Renderer>() ? rt.GetComponent<Renderer>().bounds.center : rt.position);
            maxErr = Mathf.Max(maxErr, err);
            if (scr.TryGetValue(kv.Key, out var s))
            {
                var slot = go.AddComponent<ScreenSlot>();
                slot.group = s.group; slot.tier = s.tier; slot.frames = s.frames; slot.widthMeters = s.width; slot.heightMeters = s.height;
            }
            placed++;
        }
        L($"Props placed: {placed}, missing: {missing}, max placement error: {maxErr * 1000f:F1} mm");
        Object.DestroyImmediate(refInst);

        // lights + cameras from layout markers
        var layModel = AssetDatabase.LoadAssetAtPath<GameObject>(ROOT + "/FBX/Layout.fbx");
        var lay = (GameObject)Object.Instantiate(layModel);
        var lights = Child(root, "Lights");
        var camPos = new Dictionary<string, Vector3>(); var camTgt = new Dictionary<string, Vector3>();
        foreach (var t in lay.GetComponentsInChildren<Transform>(true))
        {
            int k = t.name.IndexOf("__"); if (k < 0) continue;
            string a = t.name.Substring(0, k), b = t.name.Substring(k + 2);
            if (a == "LIGHT")
            {
                var lg = new GameObject(b); lg.transform.SetParent(lights, false);
                lg.transform.position = t.position; lg.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                var li = lg.AddComponent<Light>();
                li.type = LightType.Rectangle; li.lightmapBakeType = LightmapBakeType.Baked;
                li.areaSize = new Vector2(1.18f, 0.13f); li.intensity = LED_INTENSITY; li.range = 12f; li.shadows = LightShadows.Soft;
                li.color = new Color(1f, 0.97f, 0.92f);
            }
            else if (a == "CAM") camPos[b] = t.position;
            else if (a == "CAMTGT") camTgt[b] = t.position;
        }
        Object.DestroyImmediate(lay);

        var cams = Child(root, "Cameras");
        foreach (var kv in camPos)
        {
            var cg = new GameObject("CAM_" + kv.Key); cg.transform.SetParent(cams, false);
            cg.transform.position = kv.Value;
            if (camTgt.TryGetValue(kv.Key, out var tg)) cg.transform.LookAt(tg);
            var cam = cg.AddComponent<Camera>(); cam.fieldOfView = 70f; cam.nearClipPlane = 0.05f; cam.farClipPlane = 60f;
            if (kv.Key == "Door_View") { cg.tag = "MainCamera"; cg.AddComponent<AudioListener>(); }
            else cam.enabled = false;
        }

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.10f, 0.105f, 0.12f);
        RenderSettings.skybox = null;

        var ls = new LightingSettings();
        ls.bakedGI = true; ls.realtimeGI = false;
        ls.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
        ls.lightmapResolution = 20f; ls.lightmapPadding = 2; ls.lightmapMaxSize = 2048;
        ls.directSampleCount = 32; ls.indirectSampleCount = 256; ls.environmentSampleCount = 64; ls.maxBounces = 2;
        ls.ao = true; ls.aoMaxDistance = 0.5f;
        ls.mixedBakeMode = MixedLightingMode.IndirectOnly;
        ls.filteringMode = LightingSettings.FilterMode.Auto;
        AssetDatabase.CreateAsset(ls, ROOT + "/Scenes/ControlRoom_Lighting.lighting");
        Lightmapping.lightingSettings = ls;

        EditorSceneManager.SaveScene(scene, SCENE);
        var bs = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(SCENE, true) };
        EditorBuildSettings.scenes = bs.ToArray();
        AssetDatabase.SaveAssets();
        L("Scene saved: " + SCENE);
    }

    // ---------------------------------------------------------------- bake + screenshots
    [MenuItem("ControlRoom/2. Bake Lighting")]
    public static void BakeLighting()
    {
        var scene = EditorSceneManager.OpenScene(SCENE, OpenSceneMode.Single);
        var t0 = System.DateTime.Now;
        bool ok = Lightmapping.Bake();
        if (!ok)
        {
            L("GPU bake failed, retrying on CPU");
            Lightmapping.lightingSettings.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
            ok = Lightmapping.Bake();
        }
        L($"Bake {(ok ? "OK" : "FAILED")} in {(System.DateTime.Now - t0).TotalSeconds:F0}s");
        EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("ControlRoom/3. Save Screenshots")]
    public static void Screenshots()
    {
        EditorSceneManager.OpenScene(SCENE, OpenSceneMode.Single);
        var dir = Path.Combine(Application.dataPath, "../Screenshots"); Directory.CreateDirectory(dir);
        foreach (var cam in Object.FindObjectsOfType<Camera>(true))
        {
            var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32); rt.antiAliasing = 4;
            bool was = cam.enabled; cam.enabled = true;
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(dir, cam.name + ".png"), tex.EncodeToPNG());
            cam.targetTexture = null; RenderTexture.active = null; cam.enabled = was;
            Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
        }
        L("Screenshots saved to " + dir);
    }
}
