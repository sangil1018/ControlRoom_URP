using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// ControlRoom 씬의 비주얼 퀄리티 업그레이드 및 Meta Quest XR 최적화를 수행하는 에디터 유틸리티 클래스입니다.
/// 기존 XR/게임 로직 및 머티리얼 GUID를 보존하면서 PBR 텍스처, 라이트맵 AO, 라이트 프로브, 박스 프로젝션 리플렉션 프로브를 구성합니다.
/// </summary>
public static class ControlRoomQualityUpgrader
{
    private const string targetScenePath = "Assets/Art/ControlRoom/Scene/ControlRoom.unity";
    private const string materialsRootPath = "Assets/Art/ControlRoom/Materials";
    private const string texturesRootPath = "Assets/Art/ControlRoom/Textures";
    private const string pbrTexturesPath = "Assets/Art/ControlRoom/Textures/PBR";
    private const string lightingAssetPath = "Assets/Art/ControlRoom/Scene/ControlRoom_Lighting.lighting";
    private const string volumeProfilePath = "Assets/Art/ControlRoom/Settings/ControlRoom_VolumeProfile.asset";
    private const string probeBakeFolder = "Assets/Art/ControlRoom/Scene/ReflectionProbes";

    /// <summary>
    /// Step 1: ControlRoom.unity의 모든 MeshRenderer UV/폴리곤/라이팅 상태를 감사하고 Before 스크린샷 4장을 저장합니다.
    /// </summary>
    [MenuItem("ControlRoom/Quality/1. Audit Scene & Capture Before")]
    public static void auditSceneAndCaptureBefore()
    {
        try
        {
            Debug.Log("[ControlRoomQuality] Step 1 시작: ControlRoom.unity UV 감사 및 Before 스크린샷 캡처");

            var scene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new Exception($"씬을 열 수 없습니다: {targetScenePath}");
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string logsDir = Path.Combine(projectRoot, "Logs");
            Directory.CreateDirectory(logsDir);
            string auditPath = Path.Combine(logsDir, "ControlRoom_UVAudit.txt");

            StringBuilder report = buildUvAuditReport(scene.path);
            File.WriteAllText(auditPath, report.ToString(), Encoding.UTF8);
            Debug.Log($"[ControlRoomQuality] UV 감사 로그 저장 완료: {auditPath}");

            string beforeDir = Path.Combine(projectRoot, "Screenshots", "Before");
            captureCameraScreenshots(beforeDir, false);

            Debug.Log("[ControlRoomQuality] Step 1 완료");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] auditSceneAndCaptureBefore 실패: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Pass 1 일괄 실행: 텍스처 임포터, 30개 머티리얼, URP 에셋 업그레이드 후 ControlRoom.unity 라이팅/프로브 구성 및 베이크, After_Pass1 캡처까지 수행합니다.
    /// </summary>
    [MenuItem("ControlRoom/Quality/2. Upgrade All & Bake Pass1")]
    public static void upgradeAllAndBakePass1()
    {
        try
        {
            Debug.Log("[ControlRoomQuality] Pass1 일괄 업그레이드 시작 (대상: ControlRoom.unity 전용)");

            upgradeTexturesAndMaterials();
            upgradeUrpAssets();
            upgradeLightingSettingsAsset();
            ensureVolumeProfileAsset();

            // ControlRoom.unity 단일 씬만 업그레이드 및 베이크합니다. (타 씬 절대 미수정)
            configureAndBakeScene(targetScenePath, "ControlRoom");

            EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            captureCameraScreenshots(Path.Combine(projectRoot, "Screenshots", "After_Pass1"), true);

            verifyQualityAndPerformance();

            Debug.Log("[ControlRoomQuality] Pass1 일괄 업그레이드 완료");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] upgradeAllAndBakePass1 실패: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// 재조정(Pass 2)용 진입점: 머티리얼/라이팅 설정 변경 후 ControlRoom.unity만 재베이크하고 After_Final 스크린샷을 캡처합니다.
    /// </summary>
    [MenuItem("ControlRoom/Quality/3. Rebake & Capture Final")]
    public static void rebakeAndCaptureFinal()
    {
        try
        {
            Debug.Log("[ControlRoomQuality] Final 재베이크 시작 (대상: ControlRoom.unity 전용)");
            upgradeTexturesAndMaterials();
            upgradeUrpAssets();
            upgradeLightingSettingsAsset();
            ensureVolumeProfileAsset();

            configureAndBakeScene(targetScenePath, "ControlRoom");

            EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            captureCameraScreenshots(Path.Combine(projectRoot, "Screenshots", "After_Final"), true);

            verifyQualityAndPerformance();
            Debug.Log("[ControlRoomQuality] Final 재베이크 및 캡처 완료");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] rebakeAndCaptureFinal 실패: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// RefMatch Pass 1 일괄 실행: 현장 실사 기반 머티리얼·라이팅·프로브 적용, ControlRoom.unity 베이크, RefMatch_Pass1 8컷 캡처 및 검증.
    /// </summary>
    [MenuItem("ControlRoom/Quality/5. RefMatch Bake & Capture Pass1")]
    public static void runRefMatchPass1()
    {
        try
        {
            Debug.Log("[ControlRoomQuality] RefMatch Pass1 시작 (대상: ControlRoom.unity 전용)");
            upgradeTexturesAndMaterials();
            upgradeUrpAssets();
            upgradeLightingSettingsAsset();
            ensureVolumeProfileAsset();

            configureAndBakeScene(targetScenePath, "ControlRoom");

            EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            captureCameraScreenshots(Path.Combine(projectRoot, "Screenshots", "RefMatch_Pass1"), true);

            verifyQualityAndPerformance();
            Debug.Log("[ControlRoomQuality] RefMatch Pass1 완료");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] runRefMatchPass1 실패: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// RefMatch Final 일괄 실행: 폴리싱 반영 후 ControlRoom.unity 재베이크, RefMatch_Final 8컷 캡처 및 검증.
    /// </summary>
    [MenuItem("ControlRoom/Quality/6. RefMatch Rebake & Capture Final")]
    public static void runRefMatchFinal()
    {
        try
        {
            Debug.Log("[ControlRoomQuality] RefMatch Final 시작 (대상: ControlRoom.unity 전용)");
            upgradeTexturesAndMaterials();
            upgradeUrpAssets();
            upgradeLightingSettingsAsset();
            ensureVolumeProfileAsset();

            configureAndBakeScene(targetScenePath, "ControlRoom");

            EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            captureCameraScreenshots(Path.Combine(projectRoot, "Screenshots", "RefMatch_Final"), true);

            verifyQualityAndPerformance();
            Debug.Log("[ControlRoomQuality] RefMatch Final 완료");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] runRefMatchFinal 실패: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// 이미 베이크된 라이트맵·프로브를 유지한 채 URP HDR 컬러 그레이딩·볼륨·HDR 8컷 캡처 및 검증만 빠르게 재실행합니다.
    /// </summary>
    [MenuItem("ControlRoom/Quality/7. RefMatch Quick HDR Capture Only")]
    public static void runRefMatchQuickCapture()
    {
        try
        {
            Debug.Log("[ControlRoomQuality] RefMatch Quick HDR Capture 시작");
            upgradeTexturesAndMaterials();
            upgradeUrpAssets();
            ensureVolumeProfileAsset();

            EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            captureCameraScreenshots(Path.Combine(projectRoot, "Screenshots", "RefMatch_Final"), true);

            verifyQualityAndPerformance();
            Debug.Log("[ControlRoomQuality] RefMatch Quick HDR Capture 완료");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] runRefMatchQuickCapture 실패: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// 생성된 PBR 텍스처와 기존 텍스처의 임포터(Quest ASTC, sRGB, NormalMap, Aniso)를 최적화하고 30개 머티리얼을 현장 실사 기준으로 업그레이드합니다.
    /// </summary>
    [MenuItem("ControlRoom/Quality/Upgrade Textures & Materials Only")]
    public static void upgradeTexturesAndMaterials()
    {
        try
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            configureTextureImporters();

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            Shader complexLitShader = Shader.Find("Universal Render Pipeline/Complex Lit");
            if (litShader == null)
            {
                throw new Exception("URP Lit 셰이더를 찾을 수 없습니다.");
            }
            if (complexLitShader == null)
            {
                Debug.LogWarning("[ControlRoomQuality] URP Complex Lit 셰이더를 찾지 못해 Lit으로 폴백합니다.");
                complexLitShader = litShader;
            }

            // PBR 텍스처 로드
            Texture2D floorAlbedo = loadPbrTex("T_Floor_AccessTile_Albedo");
            Texture2D floorNormal = loadPbrTex("T_Floor_AccessTile_Normal");
            Texture2D floorMask = loadPbrTex("T_Floor_AccessTile_Mask");
            Texture2D floorAo = loadPbrTex("T_Floor_AccessTile_AO");

            Texture2D woodAlbedo = loadPbrTex("T_Wood_LightOak_Albedo");
            Texture2D woodNormal = loadPbrTex("T_Wood_LightOak_Normal");
            Texture2D woodMask = loadPbrTex("T_Wood_LightOak_Mask");
            Texture2D woodAo = loadPbrTex("T_Wood_LightOak_AO");

            Texture2D ceilingAlbedo = loadPbrTex("T_Ceiling_Mineral_Albedo");
            Texture2D ceilingNormal = loadPbrTex("T_Ceiling_Mineral_Normal");
            Texture2D ceilingAo = loadPbrTex("T_Ceiling_Mineral_AO");

            Texture2D steelNormal = loadPbrTex("T_Steel_Brushed_Normal");
            Texture2D steelMask = loadPbrTex("T_Steel_Brushed_Mask");

            Texture2D anodizedNormal = loadPbrTex("T_Metal_Anodized_Normal");
            Texture2D anodizedMask = loadPbrTex("T_Metal_Anodized_Mask");
            Texture2D aluFrameMask = loadPbrTex("T_AluFrame_Mask");

            Texture2D paintDetailAlbedo = loadPbrTex("T_Paint_Navy_Detail_Albedo");
            Texture2D paintNormal = loadPbrTex("T_Paint_Micro_Normal");
            Texture2D paintSurfaceMask = loadPbrTex("T_Paint_Surface_Mask");

            Texture2D chairMeshNormal = loadPbrTex("T_Chair_Mesh_Normal");
            Texture2D fabricNormal = loadPbrTex("T_Fabric_Weave_Normal");
            Texture2D fabricMask = loadPbrTex("T_Fabric_Weave_Mask");

            Texture2D purifierAlbedo = loadPbrTex("T_Purifier_Perforated_Albedo");
            Texture2D purifierNormal = loadPbrTex("T_Purifier_Perforated_Normal");
            Texture2D purifierAo = loadPbrTex("T_Purifier_Perforated_AO");

            Texture2D ledDiffuserNormal = loadPbrTex("T_LED_Diffuser_Normal");
            Texture2D glassMicroMask = loadPbrTex("T_Glass_Micro_Mask");

            Texture2D baseChairMeshTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturesRootPath + "/T_Chair_Mesh.png");

            int updated = 0;

            // 1. 액세스 플로어 타일 (실사 웜 트래버틴 베이지 0.84/0.805/0.745 + 초평탄 유광 반사 대비)
            updated += applyLitPbr(
                "MAT_Floor_AccessTile", litShader, new Color(0.84f, 0.805f, 0.745f, 1f), 0f, 0.98f,
                floorAlbedo, floorNormal, 0.025f, floorMask, floorAo, 0.40f,
                new Vector2(0.8333333f, 0.8333333f),
                tilingOffset: new Vector2(0.0f, -0.04f)
            );

            // 2. 노르딕 라이트 오크 원목 데스크 (91점 달성 스윗스팟 유지 + 노출 0.12EV 보정 틴트)
            updated += applyLitPbr(
                "MAT_Wood_LightOak", litShader, new Color(0.95f, 0.945f, 0.93f, 1f), 0f, 1.0f,
                woodAlbedo, woodNormal, 0.09f, woodMask, woodAo, 0.24f,
                new Vector2(0.90f, 0.90f)
            );

            // 3. 천장 흡음 텍스 (밝은 백색 유지 + 11.5 HDR LED 디퓨저와 선명히 구분되는 0.90 알베도)
            updated += applyLitPbr(
                "MAT_Ceiling_Mineral", litShader, new Color(0.90f, 0.90f, 0.895f, 1f), 0.0f, 0.06f,
                ceilingAlbedo, ceilingNormal, 0.20f, null, ceilingAo, 0.25f,
                new Vector2(1.5f, 1.5f)
            );

            // 4. 금속 5종 (브러시드 스틸, 크롬, 알루미늄, 랙 샴페인 프레임, 샴페인-니켈 브라스)
            updated += applyLitPbr(
                "MAT_Steel_Brushed", litShader, Color.white, 0.48f, 0.90f,
                null, steelNormal, 0.12f, steelMask, null, 1.0f,
                new Vector2(2.5f, 2.5f)
            );

            updated += applyLitPbr(
                "MAT_Chrome", litShader, new Color(0.94f, 0.95f, 0.96f, 1f), 1.0f, 0.94f,
                null, anodizedNormal, 0.04f, null, null, 1.0f,
                new Vector2(3.0f, 3.0f)
            );

            updated += applyLitPbr(
                "MAT_Aluminium", litShader, new Color(0.86f, 0.87f, 0.89f, 1f), 0.92f, 0.94f,
                null, anodizedNormal, 0.10f, anodizedMask, null, 1.0f,
                new Vector2(2.5f, 2.5f)
            );

            updated += applyLitPbr(
                "MAT_Alu_Frame", litShader, new Color(0.89f, 0.86f, 0.80f, 1f), 0.35f, 0.88f,
                null, steelNormal, 0.10f, aluFrameMask, null, 1.0f,
                new Vector2(2.0f, 2.0f)
            );

            updated += applyLitPbr(
                "MAT_Brass_Satin", litShader, new Color(0.90f, 0.84f, 0.70f, 1f), 0.92f, 0.94f,
                null, steelNormal, 0.12f, anodizedMask, null, 1.0f,
                new Vector2(3.0f, 3.0f)
            );

            // 5. 건축 도장 / 가전 / 랙 캐비닛 / 플라스틱 / 패브릭
            updated += applyLitPbr(
                "MAT_Metal_Black_Paint", litShader, new Color(0.12f, 0.125f, 0.135f, 1f), 0.32f, 0.56f,
                null, paintNormal, 0.14f, null, null, 1.0f,
                new Vector2(3.5f, 3.5f)
            );

            updated += applyLitPbr(
                "MAT_Paint_Navy_Wall", litShader, new Color(0.255f, 0.315f, 0.395f, 1f), 0.0f, 0.22f,
                paintDetailAlbedo, paintNormal, 0.07f, null, null, 1.0f,
                new Vector2(2.0f, 2.0f)
            );

            updated += applyLitPbr(
                "MAT_Paint_Navy_Door", litShader, new Color(0.32f, 0.38f, 0.47f, 1f), 0.12f, 0.46f,
                null, paintNormal, 0.04f, null, null, 1.0f,
                new Vector2(2.0f, 2.0f)
            );

            updated += applyLitPbr(
                "MAT_PowderCoat_White", litShader, new Color(0.88f, 0.88f, 0.87f, 1f), 0.05f, 0.48f,
                null, paintNormal, 0.08f, null, null, 1.0f,
                new Vector2(3.5f, 3.5f)
            );

            updated += applyLitPbr(
                "MAT_Paint_Ivory", litShader, new Color(0.84f, 0.81f, 0.74f, 1f), 0.02f, 0.42f,
                null, paintNormal, 0.05f, null, null, 1.0f,
                new Vector2(2.5f, 2.5f)
            );

            // 에어컨/제습기 바디·카드리더: 밝은 펄-화이트 에나멜(하단 스커트 프레넬 암화 방지를 위해 clearCoatMask 0.55)
            updated += applyLitPbr(
                "MAT_Appliance_White", complexLitShader, new Color(0.945f, 0.95f, 0.955f, 1f), 0.0f, 0.82f,
                null, null, 0f, null, null, 1.0f,
                new Vector2(3.0f, 3.0f),
                isCutout: false, enableClearCoat: true, clearCoatMask: 0.55f, clearCoatSmoothness: 0.90f
            );

            // 천장 카세트 AC 라이트 그레이-아이보리 루버 및 스탠드 AC 전면 연회색 슬릿 벤트 전용 경량 Lit 머티리얼
            updated += ensureAndApplyLitMaterial(
                "MAT_AC_Louver_Gray", litShader, new Color(0.74f, 0.74f, 0.72f, 1f), 0.0f, 0.48f,
                null, paintNormal, 0.04f, null, null, 1.0f,
                new Vector2(3.0f, 3.0f)
            );
            updated += ensureAndApplyLitMaterial(
                "MAT_AC_Stand_Vent", litShader, new Color(0.66f, 0.68f, 0.70f, 1f), 0.0f, 0.52f,
                null, paintNormal, 0.04f, null, null, 1.0f,
                new Vector2(3.0f, 3.0f)
            );

            // 제습기/공기청정기 화이트 미세 타공판 (4.2x 타일링 + 밝은 화이트 바디)
            updated += applyLitPbr(
                "MAT_Purifier_Perforated", litShader, Color.white, 0.02f, 0.78f,
                purifierAlbedo, purifierNormal, 0.12f, null, purifierAo, 0.16f,
                new Vector2(4.2f, 4.2f)
            );

            updated += applyLitPbr(
                "MAT_Chair_MeshNet_Dense", litShader, new Color(0.14f, 0.145f, 0.155f, 1f), 0.0f, 0.36f,
                baseChairMeshTex, chairMeshNormal, 0.25f, null, null, 1.0f,
                new Vector2(5.8f, 5.8f),
                isCutout: true
            );

            updated += applyLitPbr(
                "MAT_Fabric_Black", litShader, new Color(0.11f, 0.115f, 0.125f, 1f), 0.0f, 0.85f,
                null, fabricNormal, 0.24f, fabricMask, null, 1.0f,
                new Vector2(8.0f, 8.0f)
            );

            updated += applyLitPbr(
                "MAT_Plastic_Black_Matte", litShader, new Color(0.125f, 0.128f, 0.138f, 1f), 0.0f, 0.36f,
                null, paintNormal, 0.12f, null, null, 1.0f,
                new Vector2(5.0f, 5.0f)
            );

            updated += applyLitPbr(
                "MAT_Plastic_Black_Satin", litShader, new Color(0.10f, 0.105f, 0.115f, 1f), 0.0f, 0.68f,
                null, paintNormal, 0.05f, null, null, 1.0f,
                new Vector2(5.0f, 5.0f)
            );

            updated += applyLitPbr(
                "MAT_Rubber_Black", litShader, new Color(0.135f, 0.135f, 0.14f, 1f), 0.0f, 0.18f,
                null, paintNormal, 0.16f, null, null, 1.0f,
                new Vector2(6.0f, 6.0f)
            );

            // 6. 발광 LED 디퓨저 및 인디케이터 (GI 이중 누적 분리: 시각적 HDR 11.5f 발광 + 프로브/바닥/유리 반사 극대화, 바닥 과노출 차단)
            updated += applyEmissiveLit(
                "MAT_LED_Diffuser", litShader, new Color(0.99f, 0.99f, 1.0f, 1f), 0.0f, 0.85f,
                null, 0f, Vector2.one,
                new Color(0.99f, 0.995f, 1.0f) * 11.5f,
                contributeEmissiveToGi: false
            );

            updated += applyEmissiveLit(
                "MAT_LED_Indicator_Blue", litShader, new Color(0.20f, 0.45f, 0.95f, 1f), 0.0f, 0.90f,
                null, 1.0f, Vector2.one,
                new Color(0.25f, 0.55f, 0.95f) * 1.15f
            );

            updated += applyEmissiveLit(
                "MAT_LED_Indicator_White", litShader, new Color(0.92f, 0.96f, 1.0f, 1f), 0.0f, 0.90f,
                null, 1.0f, Vector2.one,
                new Color(0.95f, 0.97f, 1.0f) * 1.35f
            );

            // 7. 근접 LCD, 오퍼레이터 모니터 및 대형 월 TV (Quest 경량 URP Lit Specular 워크플로우 -> F0 0.21~0.22 강화유리 정반사)
            updated += applyScreenGlass(
                "MAT_LCD_Small", litShader, false, new Color(0.22f, 0.30f, 0.24f, 1f),
                0.0f, 0.90f, 0f, 0f, null,
                new Color(0.85f, 0.95f, 0.76f) * 1.15f, false,
                specularColor: new Color(0.14f, 0.14f, 0.15f, 1f)
            );

            string[] operatorScreenMatNames =
            {
                "MAT_Screen_Op_Alarm",
                "MAT_Screen_Op_Over",
                "MAT_Screen_Op_Proc"
            };
            foreach (string scrName in operatorScreenMatNames)
            {
                updated += applyScreenGlass(
                    scrName, litShader, false, new Color(0.01f, 0.012f, 0.016f, 1f),
                    0.0f, 0.96f, 0f, 0f, null,
                    Color.white * 0.92f, true,
                    specularColor: new Color(0.22f, 0.22f, 0.23f, 1f)
                );
            }

            string[] wallTvMatNames =
            {
                "MAT_Screen_Clock",
                "MAT_Screen_HMI_Lower",
                "MAT_Screen_HMI_Upper"
            };
            foreach (string scrName in wallTvMatNames)
            {
                updated += applyScreenGlass(
                    scrName, litShader, false, new Color(0.01f, 0.014f, 0.018f, 1f),
                    0.0f, 0.97f, 0f, 0f, null,
                    Color.white * 0.92f, true,
                    specularColor: new Color(0.21f, 0.21f, 0.22f, 1f)
                );
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[ControlRoomQuality] 머티리얼 PBR 업그레이드 완료: {updated}개 처리됨");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] upgradeTexturesAndMaterials 실패: {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// PBR 및 기존 텍스처 임포트 설정을 Quest XR ASTC 압축과 올바른 선형/노말 규격으로 구성합니다.
    /// </summary>
    private static void configureTextureImporters()
    {
        try
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { texturesRootPath }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;

                bool isScreen = path.Contains("/Screens/");
                bool isNormal = path.EndsWith("_Normal.png", StringComparison.OrdinalIgnoreCase);
                bool isMaskOrAo = path.EndsWith("_Mask.png", StringComparison.OrdinalIgnoreCase)
                                  || path.EndsWith("_AO.png", StringComparison.OrdinalIgnoreCase);

                bool changed = false;
                TextureImporterType targetType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                if (ti.textureType != targetType) { ti.textureType = targetType; changed = true; }

                bool targetSrgb = !isNormal && !isMaskOrAo;
                if (ti.sRGBTexture != targetSrgb) { ti.sRGBTexture = targetSrgb; changed = true; }

                TextureWrapMode targetWrap = isScreen ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
                if (ti.wrapMode != targetWrap) { ti.wrapMode = targetWrap; changed = true; }

                bool targetAlphaTrans = path.Contains("Chair_Mesh.png");
                if (ti.alphaIsTransparency != targetAlphaTrans) { ti.alphaIsTransparency = targetAlphaTrans; changed = true; }

                if (!ti.mipmapEnabled) { ti.mipmapEnabled = true; changed = true; }

                int targetAniso = (isScreen || path.Contains("Floor_AccessTile") || path.Contains("Wood_LightOak") || path.Contains("Purifier")) ? 4 : 2;
                if (ti.anisoLevel != targetAniso) { ti.anisoLevel = targetAniso; changed = true; }

                int targetSize = (isScreen || path.Contains("Floor_AccessTile") || path.Contains("Wood_LightOak") || path.Contains("Ceiling_Mineral_Albedo")) ? 1024 : 512;
                if (ti.maxTextureSize != targetSize) { ti.maxTextureSize = targetSize; changed = true; }

                // Android(Meta Quest) ASTC 압축 오버라이드 설정 (스크린 및 Normal Map은 정밀도 유지를 위해 ASTC 4x4 사용)
                TextureImporterPlatformSettings androidSettings = ti.GetPlatformTextureSettings("Android");
                TextureImporterFormat targetFormat = (isScreen || isNormal) ? TextureImporterFormat.ASTC_4x4 : TextureImporterFormat.ASTC_6x6;
                if (!androidSettings.overridden || androidSettings.format != targetFormat || androidSettings.maxTextureSize != targetSize)
                {
                    androidSettings.overridden = true;
                    androidSettings.name = "Android";
                    androidSettings.maxTextureSize = targetSize;
                    androidSettings.format = targetFormat;
                    androidSettings.compressionQuality = 60;
                    ti.SetPlatformTextureSettings(androidSettings);
                    changed = true;
                }

                if (changed)
                {
                    ti.SaveAndReimport();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] configureTextureImporters 실패: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// PBR 폴더에서 텍스처를 로드합니다.
    /// </summary>
    private static Texture2D loadPbrTex(string fileNameWithoutExt)
    {
        try
        {
            string fullPath = $"{pbrTexturesPath}/{fileNameWithoutExt}.png";
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
            if (tex == null)
            {
                Debug.LogWarning($"[ControlRoomQuality] PBR 텍스처를 찾을 수 없습니다: {fullPath}");
            }
            return tex;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] loadPbrTex 실패 ({fileNameWithoutExt}): {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// URP Lit 머티리얼에 Base/Normal/MetallicGloss/AO 및 타일링/컷아웃 속성을 적용합니다.
    /// </summary>
    private static int applyLitPbr(
        string matName,
        Shader shader,
        Color baseColor,
        float metallic,
        float smoothness,
        Texture2D baseMap,
        Texture2D normalMap,
        float bumpScale,
        Texture2D metallicGlossMap,
        Texture2D occlusionMap,
        float occlusionStrength,
        Vector2 tiling,
        bool isCutout = false,
        bool enableClearCoat = false,
        float clearCoatMask = 0f,
        float clearCoatSmoothness = 0f,
        Vector2? tilingOffset = null
    )
    {
        try
        {
            string path = $"{materialsRootPath}/{matName}.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                Debug.LogWarning($"[ControlRoomQuality] 머티리얼 없음: {path}");
                return 0;
            }

            m.shader = shader;
            m.enableInstancing = true;
            m.SetColor("_BaseColor", baseColor);
            m.SetColor("_Color", baseColor);
            m.SetFloat("_WorkflowMode", 1f);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_SmoothnessTextureChannel", 0f);
            m.SetFloat("_EnvironmentReflections", 1f);
            m.SetFloat("_SpecularHighlights", 1f);
            m.SetFloat("_ReceiveShadows", 1f);

            // Clear Coat 설정 (가전 펄-화이트 등 고광택 코팅 재질 전용)
            if (enableClearCoat)
            {
                if (m.HasProperty("_ClearCoat")) m.SetFloat("_ClearCoat", 1f);
                if (m.HasProperty("_ClearCoatMask")) m.SetFloat("_ClearCoatMask", clearCoatMask);
                if (m.HasProperty("_ClearCoatSmoothness")) m.SetFloat("_ClearCoatSmoothness", clearCoatSmoothness);
                m.EnableKeyword("_CLEARCOAT");
                m.DisableKeyword("_CLEARCOATMAP");
            }
            else
            {
                if (m.HasProperty("_ClearCoat")) m.SetFloat("_ClearCoat", 0f);
                if (m.HasProperty("_ClearCoatMask")) m.SetFloat("_ClearCoatMask", 0f);
                m.DisableKeyword("_CLEARCOAT");
                m.DisableKeyword("_CLEARCOATMAP");
            }

            Vector2 uvOffset = tilingOffset ?? Vector2.zero;

            // BaseMap
            m.SetTexture("_BaseMap", baseMap);
            m.SetTexture("_MainTex", baseMap);
            m.SetTextureScale("_BaseMap", tiling);
            m.SetTextureScale("_MainTex", tiling);
            m.SetTextureOffset("_BaseMap", uvOffset);
            m.SetTextureOffset("_MainTex", uvOffset);

            // NormalMap
            if (normalMap != null)
            {
                m.SetTexture("_BumpMap", normalMap);
                m.SetTextureScale("_BumpMap", tiling);
                m.SetTextureOffset("_BumpMap", uvOffset);
                m.SetFloat("_BumpScale", bumpScale);
                m.EnableKeyword("_NORMALMAP");
            }
            else
            {
                m.SetTexture("_BumpMap", null);
                m.DisableKeyword("_NORMALMAP");
            }

            // MetallicGlossMap (R=Metallic, A=Smoothness)
            if (metallicGlossMap != null)
            {
                m.SetTexture("_MetallicGlossMap", metallicGlossMap);
                m.SetTextureScale("_MetallicGlossMap", tiling);
                m.SetTextureOffset("_MetallicGlossMap", uvOffset);
                m.SetFloat("_GlossMapScale", smoothness);
                m.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            else
            {
                m.SetTexture("_MetallicGlossMap", null);
                m.SetFloat("_GlossMapScale", 0f);
                m.DisableKeyword("_METALLICSPECGLOSSMAP");
            }

            // OcclusionMap
            if (occlusionMap != null)
            {
                m.SetTexture("_OcclusionMap", occlusionMap);
                m.SetTextureScale("_OcclusionMap", tiling);
                m.SetTextureOffset("_OcclusionMap", uvOffset);
                m.SetFloat("_OcclusionStrength", occlusionStrength);
                m.EnableKeyword("_OCCLUSIONMAP");
            }
            else
            {
                m.SetTexture("_OcclusionMap", null);
                m.SetFloat("_OcclusionStrength", 1f);
                m.DisableKeyword("_OCCLUSIONMAP");
            }

            // Alpha Clip 양면 처리 (의자 메쉬망 등)
            if (isCutout)
            {
                m.SetFloat("_AlphaClip", 1f);
                m.SetFloat("_Cutoff", 0.42f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.SetFloat("_Cull", 0f);
                m.doubleSidedGI = true;
                m.renderQueue = (int)RenderQueue.AlphaTest;
                m.SetOverrideTag("RenderType", "TransparentCutout");
            }
            else
            {
                m.SetFloat("_AlphaClip", 0f);
                m.DisableKeyword("_ALPHATEST_ON");
                m.SetFloat("_Cull", 2f);
                m.doubleSidedGI = false;
                m.renderQueue = -1;
                m.SetOverrideTag("RenderType", "Opaque");
            }

            // 비발광 정리 (EmissiveIsBlack 지정으로 불필요한 _EMISSION 배리언트 누수 방지)
            m.DisableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;

            EditorUtility.SetDirty(m);
            return 1;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] applyLitPbr 실패 ({matName}): {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 전용 추가 머티리얼이 존재하지 않으면 새로 생성한 뒤 applyLitPbr로 PBR 속성을 적용합니다.
    /// </summary>
    private static int ensureAndApplyLitMaterial(
        string matName,
        Shader shader,
        Color baseColor,
        float metallic,
        float smoothness,
        Texture2D baseMap,
        Texture2D normalMap,
        float bumpScale,
        Texture2D metallicGlossMap,
        Texture2D occlusionMap,
        float occlusionStrength,
        Vector2 tiling
    )
    {
        try
        {
            string path = $"{materialsRootPath}/{matName}.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
            {
                Material created = new Material(shader) { name = matName };
                AssetDatabase.CreateAsset(created, path);
            }
            return applyLitPbr(
                matName, shader, baseColor, metallic, smoothness,
                baseMap, normalMap, bumpScale, metallicGlossMap, occlusionMap, occlusionStrength, tiling
            );
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] ensureAndApplyLitMaterial 실패 ({matName}): {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 발광(LED 디퓨저/인디케이터) 머티리얼을 설정합니다.
    /// </summary>
    private static int applyEmissiveLit(
        string matName,
        Shader shader,
        Color baseColor,
        float metallic,
        float smoothness,
        Texture2D normalMap,
        float bumpScale,
        Vector2 tiling,
        Color emissionHdr,
        bool contributeEmissiveToGi = true
    )
    {
        try
        {
            int count = applyLitPbr(
                matName, shader, baseColor, metallic, smoothness,
                null, normalMap, bumpScale, null, null, 1f, tiling
            );
            if (count == 0) return 0;

            Material m = AssetDatabase.LoadAssetAtPath<Material>($"{materialsRootPath}/{matName}.mat");
            m.SetColor("_EmissionColor", emissionHdr);
            m.EnableKeyword("_EMISSION");
            // EmissiveIsBlack(4)는 Unity 직렬화 시 _EMISSION 키워드를 제거하므로, GI 비참여 발광은 None(0) 사용
            m.globalIlluminationFlags = contributeEmissiveToGi
                ? MaterialGlobalIlluminationFlags.BakedEmissive
                : MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(m);
            return 1;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] applyEmissiveLit 실패 ({matName}): {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 모니터/TV/LCD 머티리얼에 유리 반사(Complex Lit Clear Coat 또는 고광택 Lit)와 미세 마스크를 적용하며 기존 _EmissionMap을 유지합니다.
    /// </summary>
    private static int applyScreenGlass(
        string matName,
        Shader targetShader,
        bool enableClearCoat,
        Color baseColor,
        float metallic,
        float smoothness,
        float clearCoatMask,
        float clearCoatSmoothness,
        Texture2D glassMask,
        Color emissionHdr,
        bool preserveExistingEmissionMap,
        Color? specularColor = null
    )
    {
        try
        {
            string path = $"{materialsRootPath}/{matName}.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                Debug.LogWarning($"[ControlRoomQuality] 스크린 머티리얼 없음: {path}");
                return 0;
            }

            Texture existingEmissionTex = preserveExistingEmissionMap ? m.GetTexture("_EmissionMap") : null;

            m.shader = targetShader;
            m.enableInstancing = true;
            m.SetColor("_BaseColor", baseColor);
            m.SetColor("_Color", baseColor);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_SmoothnessTextureChannel", 0f);
            m.SetFloat("_EnvironmentReflections", 1f);
            m.SetFloat("_SpecularHighlights", 1f);

            if (specularColor.HasValue)
            {
                // Specular 워크플로우: 검은 베이스에서도 높은 정면 유리 F0 반사 구현 (Complex Lit 불필요)
                m.SetFloat("_WorkflowMode", 0f);
                m.EnableKeyword("_SPECULAR_SETUP");
                m.SetColor("_SpecColor", specularColor.Value);
                m.SetTexture("_SpecGlossMap", null);
                m.SetTexture("_MetallicGlossMap", null);
                m.SetFloat("_GlossMapScale", 0f);
                m.DisableKeyword("_SPECGLOSSMAP");
                m.DisableKeyword("_METALLICSPECGLOSSMAP");
            }
            else
            {
                m.SetFloat("_WorkflowMode", 1f);
                m.DisableKeyword("_SPECULAR_SETUP");
                m.SetFloat("_Metallic", metallic);

                // 유리 표면의 초미세 거칠기 변화 마스크 (미지정 시 균일 고광택 강화유리)
                if (glassMask != null)
                {
                    m.SetTexture("_MetallicGlossMap", glassMask);
                    m.SetTextureScale("_MetallicGlossMap", new Vector2(2f, 2f));
                    m.SetFloat("_GlossMapScale", smoothness);
                    m.EnableKeyword("_METALLICSPECGLOSSMAP");
                }
                else
                {
                    m.SetTexture("_MetallicGlossMap", null);
                    m.SetFloat("_GlossMapScale", 0f);
                    m.DisableKeyword("_METALLICSPECGLOSSMAP");
                }
            }

            // 근접 디스플레이는 Clear Coat 활성화, 대형 TV는 셰이더 부하 절감을 위해 Clear Coat 비활성화
            if (enableClearCoat)
            {
                if (m.HasProperty("_ClearCoat")) m.SetFloat("_ClearCoat", 1f);
                if (m.HasProperty("_ClearCoatMask")) m.SetFloat("_ClearCoatMask", clearCoatMask);
                if (m.HasProperty("_ClearCoatSmoothness")) m.SetFloat("_ClearCoatSmoothness", clearCoatSmoothness);
                m.EnableKeyword("_CLEARCOAT");
                m.DisableKeyword("_CLEARCOATMAP");
            }
            else
            {
                if (m.HasProperty("_ClearCoat")) m.SetFloat("_ClearCoat", 0f);
                if (m.HasProperty("_ClearCoatMask")) m.SetFloat("_ClearCoatMask", 0f);
                m.DisableKeyword("_CLEARCOAT");
                m.DisableKeyword("_CLEARCOATMAP");
            }

            // 화면 발광 유지
            if (existingEmissionTex != null)
            {
                m.SetTexture("_EmissionMap", existingEmissionTex);
                m.SetTextureScale("_EmissionMap", Vector2.one);
            }
            m.SetColor("_EmissionColor", emissionHdr);
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;

            m.renderQueue = -1;
            m.SetOverrideTag("RenderType", "Opaque");
            EditorUtility.SetDirty(m);
            return 1;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] applyScreenGlass 실패 ({matName}): {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// URP-Balanced(Quest 기본), URP-HighFidelity, ControlRoom_URP 에셋을 Quest XR 성능 및 실내 반사 품질 기준으로 최적화합니다.
    /// </summary>
    [MenuItem("ControlRoom/Quality/Upgrade URP Assets")]
    public static void upgradeUrpAssets()
    {
        try
        {
            string[] urpPaths =
            {
                "Assets/Settings/URP-Balanced.asset",
                "Assets/Settings/URP-HighFidelity.asset",
                "Assets/Art/ControlRoom/Settings/ControlRoom_URP.asset"
            };

            foreach (string path in urpPaths)
            {
                UniversalRenderPipelineAsset asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (asset == null)
                {
                    Debug.LogWarning($"[ControlRoomQuality] URP 에셋 없음: {path}");
                    continue;
                }

                bool isQuestProfile = path.Contains("URP-Balanced") || path.Contains("ControlRoom_URP");
                SerializedObject so = new SerializedObject(asset);

                setIntProp(so, "m_MSAA", 4);
                setIntProp(so, "m_ReflectionProbeBoxProjection", 1);
                setIntProp(so, "m_ReflectionProbeBlending", 1);
                setIntProp(so, "m_SupportsHDR", 1);
                setIntProp(so, "m_HDRColorBufferPrecision", 0); // 32-bit R11G11B10F
                setIntProp(so, "m_ColorGradingMode", 1); // 1 = HighDynamicRange (32^3 HDR LogC LUT + 톤맵 풀레인지)
                setIntProp(so, "m_RequireDepthTexture", 0);
                setIntProp(so, "m_RequireOpaqueTexture", 0);
                setIntProp(so, "m_UseSRPBatcher", 1);
                setIntProp(so, "m_MixedLightingSupported", 1);
                setIntProp(so, "m_SupportsLightCookies", 0);

                if (isQuestProfile)
                {
                    // Quest XR 목표(72~90fps): PerPixel 추가 라이트 2개 허용, 실시간 섀도우 패스 0개
                    setIntProp(so, "m_AdditionalLightsRenderingMode", 1);
                    setIntProp(so, "m_AdditionalLightsPerObjectLimit", 2);
                    setIntProp(so, "m_MainLightShadowsSupported", 0);
                    setIntProp(so, "m_AdditionalLightShadowsSupported", 0);
                    setIntProp(so, "m_SoftShadowsSupported", 0);
                }
                else
                {
                    // 에디터/PC 프리뷰에서도 일관된 라이트 및 그림자 없는 하이라이트 유지
                    setIntProp(so, "m_AdditionalLightsRenderingMode", 1);
                    setIntProp(so, "m_AdditionalLightsPerObjectLimit", 4);
                }

                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                Debug.Log($"[ControlRoomQuality] URP 에셋 최적화 완료: {path}");
            }

            AssetDatabase.SaveAssets();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] upgradeUrpAssets 실패: {ex.Message}");
            throw;
        }
    }

    private static void setIntProp(SerializedObject so, string propName, int value)
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p != null)
        {
            p.intValue = value;
        }
        else
        {
            Debug.LogWarning($"[ControlRoomQuality] URP 프로퍼티를 찾지 못함: {propName}");
        }
    }

    private static void setFloatProp(SerializedObject so, string propName, float value)
    {
        SerializedProperty p = so.FindProperty(propName);
        if (p != null)
        {
            p.floatValue = value;
        }
        else
        {
            Debug.LogWarning($"[ControlRoomQuality] Serialized float 프로퍼티를 찾지 못함: {propName}");
        }
    }

    /// <summary>
    /// ControlRoom_Lighting.lighting을 현장 전등 ALL ON 기준 다중 바운스 + Baked AO 사양으로 업그레이드합니다.
    /// </summary>
    private static void upgradeLightingSettingsAsset()
    {
        try
        {
            LightingSettings ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(lightingAssetPath);
            if (ls == null)
            {
                ls = new LightingSettings();
                AssetDatabase.CreateAsset(ls, lightingAssetPath);
            }

            ls.bakedGI = true;
            ls.realtimeGI = false;
            ls.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            ls.directionalityMode = LightmapsMode.CombinedDirectional;
            ls.mixedBakeMode = MixedLightingMode.IndirectOnly;

            // 라이트맵 Baked A.O. 설정 (코너 과암화 억제 + 책상 다리/캐비닛 바닥 1~5cm 컨택트 섀도우 강화)
            ls.ao = true;
            ls.aoMaxDistance = 0.42f;
            ls.aoExponentIndirect = 1.38f;
            ls.aoExponentDirect = 1.20f;

            ls.lightmapResolution = 36f;
            ls.lightmapMaxSize = 2048;
            ls.lightmapPadding = 4;
            ls.directSampleCount = 64;
            ls.indirectSampleCount = 512;
            ls.environmentSampleCount = 128;
            ls.maxBounces = 4;
            ls.filteringMode = LightingSettings.FilterMode.Auto;
            ls.lightmapCompression = LightmapCompression.NormalQuality;

            EditorUtility.SetDirty(ls);

            // AlbedoBoost와 IndirectOutputScale 균형 조정 (하이라이트 클리핑 방지 + 바닥/데스크 반사 대비 확보)
            SerializedObject lsSo = new SerializedObject(ls);
            setFloatProp(lsSo, "m_AlbedoBoost", 1.22f);
            setFloatProp(lsSo, "m_IndirectOutputScale", 1.28f);
            lsSo.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            Debug.Log("[ControlRoomQuality] LightingSettings(Baked AO + 4 Bounces + AlbedoBoost 1.35) 업그레이드 완료");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] upgradeLightingSettingsAsset 실패: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 현장 영상 화이트밸런스에 맞춘 ACES 톤매핑 + WhiteBalance + 경량 Quarter Bloom 기반 VolumeProfile 에셋을 생성/업데이트합니다.
    /// 서브에셋이 파일 내부에 정상 직렬화되도록 AddObjectToAsset을 반드시 호출합니다.
    /// </summary>
    private static VolumeProfile ensureVolumeProfileAsset()
    {
        try
        {
            VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(volumeProfilePath);
            bool hasCorruptNull = false;
            if (profile != null)
            {
                foreach (VolumeComponent c in profile.components)
                {
                    if (c == null || !AssetDatabase.Contains(c))
                    {
                        hasCorruptNull = true;
                        break;
                    }
                }
            }
            if (hasCorruptNull)
            {
                AssetDatabase.DeleteAsset(volumeProfilePath);
                profile = null;
            }

            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, volumeProfilePath);
            }

            if (!profile.TryGet(out Tonemapping tone))
            {
                tone = profile.Add<Tonemapping>(true);
            }
            ensureVolumeComponentSubAsset(tone, profile);
            tone.active = true;
            tone.mode.Override(TonemappingMode.ACES);

            if (!profile.TryGet(out ColorAdjustments colorAdj))
            {
                colorAdj = profile.Add<ColorAdjustments>(true);
            }
            ensureVolumeComponentSubAsset(colorAdj, profile);
            colorAdj.active = true;
            colorAdj.postExposure.Override(0.12f);
            colorAdj.contrast.Override(5f);
            colorAdj.saturation.Override(2f);

            if (!profile.TryGet(out WhiteBalance wb))
            {
                wb = profile.Add<WhiteBalance>(true);
            }
            ensureVolumeComponentSubAsset(wb, profile);
            wb.active = true;
            wb.temperature.Override(0.5f);
            wb.tint.Override(0f);

            if (!profile.TryGet(out Bloom bloom))
            {
                bloom = profile.Add<Bloom>(true);
            }
            ensureVolumeComponentSubAsset(bloom, profile);
            bloom.active = true;
            bloom.threshold.Override(1.65f);
            bloom.intensity.Override(0.20f);
            bloom.scatter.Override(0.50f);
            bloom.highQualityFiltering.Override(false);
            bloom.downscale.Override(BloomDownscaleMode.Quarter);
            bloom.maxIterations.Override(3);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(volumeProfilePath, ImportAssetOptions.ForceSynchronousImport);
            return profile;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] ensureVolumeProfileAsset 실패: {ex.Message}");
            throw;
        }
    }

    private static void ensureVolumeComponentSubAsset(VolumeComponent comp, VolumeProfile profile)
    {
        if (comp == null || profile == null) return;
        comp.hideFlags = HideFlags.HideInHierarchy;
        if (!AssetDatabase.Contains(comp))
        {
            AssetDatabase.AddObjectToAsset(comp, profile);
        }
        EditorUtility.SetDirty(comp);
    }

    /// <summary>
    /// 지정된 씬을 열어 라이팅, 실시간 하이라이트 라이트, 4계층 라이트 프로브 그룹, 5개 리플렉션 프로브, 볼륨을 구성하고 베이크합니다.
    /// </summary>
    private static void configureAndBakeScene(string scenePath, string probeSubFolder)
    {
        try
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                throw new Exception($"씬을 열 수 없습니다: {scenePath}");
            }

            LightingSettings ls = AssetDatabase.LoadAssetAtPath<LightingSettings>(lightingAssetPath);
            if (ls != null)
            {
                Lightmapping.lightingSettings = ls;
            }

            // 1. 소형 반복 프롭은 LightProbe 수신으로 전환해 라이트맵 UV 겹침 경고를 제거하고 바닥/벽/데스크 해상도를 극대화합니다.
            optimizeSmallPropsReceiveGi();

            // 1-B. 천장 카세트 AC 및 스탠드 AC 루버/벤트에 현장 색감 전용 머티리얼을 분리 적용합니다.
            assignPropAccentMaterials();

            // 2. 기존 천장 LED Area Light 25개 색온도/강도 튜닝 + 입구 니치 보조 LED + 상향 천장 바운스 필 라이트 구성
            GameObject crRoot = GameObject.Find("ControlRoom");
            if (crRoot == null)
            {
                throw new Exception($"ControlRoom 루트 오브젝트를 찾을 수 없습니다: {scenePath}");
            }

            Transform lightsParent = ensureChild(crRoot.transform, "Lights");
            Transform rigParent = ensureChild(crRoot.transform, "LightingRig");

            tuneCeilingAreaLights(lightsParent);

            // 4. 천장/TV 핫스팟을 유발하지 않는 그림자 없는 실시간 하이라이트 라이트 배치 (Spot + Directional)
            setupRealtimeHighlightLights(lightsParent);

            // 5. 방 전체 4계층 3D Light Probe Group 구축 (내부 갇힘 회피 및 중복 간격 제거 포함)
            setupLightProbeGroup(rigParent);

            // 6. Box Projection + Blending 6개 Reflection Probe 배치
            List<ReflectionProbe> probes = setupReflectionProbes(rigParent);

            // 7. Ambient 및 Post-Processing Global Volume + 카메라 postProcessing 활성화
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.165f, 0.172f, 0.185f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.145f, 0.150f, 0.162f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.105f, 0.098f, 0.088f, 1f);
            RenderSettings.skybox = null;

            setupGlobalVolumeAndCameras(rigParent);

            EditorSceneManager.SaveScene(scene);

            // 8. 라이트맵 + Baked AO + Light Probe Group 베이크
            DateTime t0 = DateTime.Now;
            bool ok = Lightmapping.Bake();
            if (!ok)
            {
                Debug.LogWarning("[ControlRoomQuality] GPU 라이트맵 베이크 실패 -> ProgressiveCPU로 재시도");
                Lightmapping.lightingSettings.lightmapper = LightingSettings.Lightmapper.ProgressiveCPU;
                ok = Lightmapping.Bake();
            }
            if (!ok)
            {
                throw new Exception($"라이트맵 베이크 실패: {scenePath}");
            }
            Debug.Log($"[ControlRoomQuality] {probeSubFolder} 라이트맵 베이크 완료 ({(DateTime.Now - t0).TotalSeconds:F1}s)");

            // 9. 베이크된 라이트맵이 반영된 상태에서 5개 Reflection Probe 큐브맵을 베이킹 및 할당
            bakeAndAssignReflectionProbes(probes, probeSubFolder);

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] configureAndBakeScene 실패 ({scenePath}): {ex.Message}\n{ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// 소형·복잡 UV 프롭과 4-vertex 스크린 쿼드를 LightProbes 수신으로 전환해 라이트맵 UV 겹침 경고를 제거하고 바닥 콘택트 AO 해상도를 집중합니다.
    /// </summary>
    private static void optimizeSmallPropsReceiveGi()
    {
        try
        {
            string[] lightProbeKeywords =
            {
                "Keyboard", "Mouse", "MiniPC", "Wall_Outlet",
                "OfficeChair", "MonitorArm", "TVFeet", "Floor_Grille",
                "Door_Hardware", "Door_Levers", "Wall_Controller", "Door_CardReader",
                "Air_Purifier", "Door2_Hardware"
            };
            int switched = 0;

            foreach (MeshRenderer mr in UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true))
            {
                if (mr == null) continue;
                string n = mr.gameObject.name;
                string parentName = mr.transform.parent != null ? mr.transform.parent.name : "";
                bool isLightProbeTarget = false;
                foreach (string kw in lightProbeKeywords)
                {
                    if (n.Contains(kw) || parentName.Contains(kw))
                    {
                        isLightProbeTarget = true;
                        break;
                    }
                }
                bool isScreen = n.Contains("Screen");

                if (isLightProbeTarget || isScreen)
                {
                    mr.receiveGI = ReceiveGI.LightProbes;
                    mr.scaleInLightmap = 0f;
                    EditorUtility.SetDirty(mr);
                    switched++;
                }
                else if (n.Contains("Floor_Access") || n.Contains("Floor_Niche") || n.Contains("Wall_") || n.Contains("Ceiling_Board"))
                {
                    if (!n.Contains("Floor_collider"))
                    {
                        mr.receiveGI = ReceiveGI.Lightmaps;
                        mr.scaleInLightmap = n.Contains("Floor_Access") ? 2.0f : 1.0f;
                        EditorUtility.SetDirty(mr);
                    }
                }
            }

            Debug.Log($"[ControlRoomQuality] 소형/복잡 프롭·스크린 LightProbes 수신 전환: {switched}개");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] optimizeSmallPropsReceiveGi 실패: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 천장 카세트 AC 및 스탠드 AC의 루버/벤트 슬롯에만 현장 색감 전용 머티리얼을 적용해 다른 프롭의 MAT_Plastic_Black_Matte는 보존합니다.
    /// </summary>
    private static void assignPropAccentMaterials()
    {
        try
        {
            Material louverGray = AssetDatabase.LoadAssetAtPath<Material>($"{materialsRootPath}/MAT_AC_Louver_Gray.mat");
            Material standVent = AssetDatabase.LoadAssetAtPath<Material>($"{materialsRootPath}/MAT_AC_Stand_Vent.mat");

            int updated = 0;
            foreach (MeshRenderer mr in UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true))
            {
                if (mr == null) continue;
                string n = mr.gameObject.name;
                bool isCassette = n.StartsWith("Ceiling_Cassette_AC");
                bool isStandAc = n == "Stand_AC";
                if (!isCassette && !isStandAc) continue;

                Material[] mats = mr.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] != null && mats[i].name == "MAT_Plastic_Black_Matte")
                    {
                        Material replacement = isCassette ? louverGray : standVent;
                        if (replacement != null)
                        {
                            mats[i] = replacement;
                            changed = true;
                        }
                    }
                }
                if (changed)
                {
                    mr.sharedMaterials = mats;
                    EditorUtility.SetDirty(mr);
                    updated++;
                }
            }
            Debug.Log($"[ControlRoomQuality] 에어컨 루버/벤트 전용 머티리얼 할당 완료: {updated}개");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] assignPropAccentMaterials 실패: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 25개 천장 LED Area Light를 ~4850K 크리습 데이라이트 조명으로 조정하고 입구 니치 보조광 및 상향 천장 바운스 필 라이트 4개를 배치합니다.
    /// </summary>
    private static void tuneCeilingAreaLights(Transform lightsParent)
    {
        try
        {
            int count = 0;
            Color ledNeutralDaylight = new Color(0.985f, 0.985f, 0.995f, 1f); // 약 4850K 현장 전등 ALL ON 기준
            foreach (Light li in UnityEngine.Object.FindObjectsOfType<Light>(true))
            {
                if (li == null || !li.gameObject.name.StartsWith("LED_")
                    || li.gameObject.name == "LED_Entry_Niche"
                    || li.gameObject.name.StartsWith("LED_CeilingBounce_")) continue;
                li.type = LightType.Rectangle;
                li.lightmapBakeType = LightmapBakeType.Baked;
                li.areaSize = new Vector2(1.18f, 0.13f);
                li.intensity = 34f;
                li.range = 12f;
                li.shadows = LightShadows.Soft;
                li.color = ledNeutralDaylight;
                EditorUtility.SetDirty(li);
                count++;
            }

            // 입구 니치 천장 보조 면광원 (Wall_SS_Panel, Stand_AC, Air_Purifier, 출입문 상단 조도 보강)
            Transform existingNiche = lightsParent.Find("LED_Entry_Niche");
            if (existingNiche != null)
            {
                UnityEngine.Object.DestroyImmediate(existingNiche.gameObject);
            }
            GameObject nicheGo = new GameObject("LED_Entry_Niche");
            nicheGo.transform.SetParent(lightsParent, false);
            nicheGo.transform.position = new Vector3(-11.50f, 2.88f, 0.40f);
            nicheGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Light nicheLight = nicheGo.AddComponent<Light>();
            nicheLight.type = LightType.Rectangle;
            nicheLight.lightmapBakeType = LightmapBakeType.Baked;
            nicheLight.areaSize = new Vector2(0.95f, 0.30f);
            nicheLight.intensity = 26f;
            nicheLight.range = 7.0f;
            nicheLight.shadows = LightShadows.Soft;
            nicheLight.color = ledNeutralDaylight;

            // 상향 천장 바운스 필 라이트 4개 (Baked 전용 -> 천장 과클리핑 없이 자연스러운 간접광 톤 유지)
            for (int b = 1; b <= 4; b++)
            {
                Transform oldBounce = lightsParent.Find($"LED_CeilingBounce_{b}");
                if (oldBounce != null)
                {
                    UnityEngine.Object.DestroyImmediate(oldBounce.gameObject);
                }
            }
            Vector3[] bouncePositions =
            {
                new Vector3(-3.5f, 1.85f, -3.5f),
                new Vector3(-10.5f, 1.85f, -3.5f),
                new Vector3(-3.5f, 1.85f, -7.0f),
                new Vector3(-10.5f, 1.85f, -7.0f)
            };
            Color bounceColor = new Color(0.98f, 0.985f, 1.0f, 1f);
            for (int i = 0; i < bouncePositions.Length; i++)
            {
                GameObject bGo = new GameObject($"LED_CeilingBounce_{i + 1}");
                bGo.transform.SetParent(lightsParent, false);
                bGo.transform.position = bouncePositions[i];
                bGo.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
                Light bLight = bGo.AddComponent<Light>();
                bLight.type = LightType.Rectangle;
                bLight.lightmapBakeType = LightmapBakeType.Baked;
                bLight.areaSize = new Vector2(4.2f, 3.2f);
                bLight.intensity = 1.6f;
                bLight.range = 5.0f;
                bLight.shadows = LightShadows.Soft;
                bLight.color = bounceColor;
            }

            Debug.Log($"[ControlRoomQuality] 천장 LED Area Light {count}개 + 니치 보조광 + 상향 바운스 4개 튜닝 완료");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] tuneCeilingAreaLights 실패: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 천장/TV 원형 글레어를 만들지 않도록 하향 Spot Light 1개와 미세 상부 Directional Light 1개만 사용해 금속/원목/가전에 하이라이트를 부여합니다.
    /// </summary>
    private static void setupRealtimeHighlightLights(Transform lightsParent)
    {
        try
        {
            Transform hlRoot = lightsParent.Find("RealtimeHighlights");
            if (hlRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(hlRoot.gameObject);
            }
            hlRoot = new GameObject("RealtimeHighlights").transform;
            hlRoot.SetParent(lightsParent, false);

            // 1. 상부 방향성 스페큘러 키라이트 (Mixed + Shadows None)
            GameObject dirGo = new GameObject("Key_Overhead_Specular");
            dirGo.transform.SetParent(hlRoot, false);
            dirGo.transform.rotation = Quaternion.Euler(74f, -15f, 0f);
            Light dirLight = dirGo.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.lightmapBakeType = LightmapBakeType.Mixed;
            dirLight.shadows = LightShadows.None;
            dirLight.intensity = 0.22f;
            dirLight.color = new Color(1.0f, 0.975f, 0.94f, 1f);

            // 2. 중앙 오퍼레이터 콘솔 영역 하향 스팟 라이트 (Mixed + Shadows None)
            GameObject centerGo = new GameObject("Highlight_Center_Console");
            centerGo.transform.SetParent(hlRoot, false);
            centerGo.transform.position = new Vector3(-7.0f, 2.55f, -4.4f);
            centerGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Light centerLight = centerGo.AddComponent<Light>();
            centerLight.type = LightType.Spot;
            centerLight.lightmapBakeType = LightmapBakeType.Mixed;
            centerLight.shadows = LightShadows.None;
            centerLight.spotAngle = 115f;
            centerLight.innerSpotAngle = 55f;
            centerLight.range = 5.5f;
            centerLight.intensity = 0.55f;
            centerLight.color = new Color(1.0f, 0.975f, 0.935f, 1f);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] setupRealtimeHighlightLights 실패: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 방 전체 4개 높이 계층 + 출입구 니치 영역에 3D Light Probe Group을 배치하며, 지오메트리 내부 갇힘을 자동 회피합니다.
    /// </summary>
    private static void setupLightProbeGroup(Transform rigParent)
    {
        try
        {
            Transform existing = rigParent.Find("ControlRoom_LightProbeGroup");
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }

            GameObject lpgGo = new GameObject("ControlRoom_LightProbeGroup");
            lpgGo.transform.SetParent(rigParent, false);
            LightProbeGroup lpg = lpgGo.AddComponent<LightProbeGroup>();

            // 방 내부 주요 지오메트리 바운드(데스크/캐비닛 등)를 수집하여 프로브 갇힘을 회피합니다.
            List<Bounds> solidBounds = new List<Bounds>();
            foreach (MeshRenderer mr in UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true))
            {
                if (mr == null) continue;
                string n = mr.gameObject.name;
                if (n.Contains("Desk") || n.Contains("Rack_Cabinet") || n.Contains("Stand_AC") || n.Contains("Air_Purifier"))
                {
                    // 가구 내부 관통을 피하기 위해 살짝 여유(-2cm) 둔 바운딩 체크
                    Bounds b = mr.bounds;
                    b.Expand(0.06f);
                    solidBounds.Add(b);
                }
            }

            List<Vector3> positions = new List<Vector3>();
            int xCount = 8; // X: -13.6 ~ -0.4
            int zCount = 6; // Z: -9.6 ~ -0.4
            float[] yLayers = { 0.25f, 0.85f, 1.55f, 2.55f };

            const float minProbeSpacingSq = 0.20f * 0.20f;
            for (int yi = 0; yi < yLayers.Length; yi++)
            {
                float y = yLayers[yi];
                for (int xi = 0; xi < xCount; xi++)
                {
                    float x = Mathf.Lerp(-13.6f, -0.4f, xi / (float)(xCount - 1));
                    for (int zi = 0; zi < zCount; zi++)
                    {
                        float z = Mathf.Lerp(-9.6f, -0.4f, zi / (float)(zCount - 1));
                        Vector3 candidate = adjustProbeOutOfSolids(new Vector3(x, y, z), solidBounds);
                        if (!isProbeTooClose(candidate, positions, minProbeSpacingSq))
                        {
                            positions.Add(candidate);
                        }
                    }
                }
            }

            // 출입구 니치 영역 보강 (X in [-12.3, -10.7], Z = 0.45, 3개 높이)
            float[] nicheX = { -12.25f, -11.50f, -10.75f };
            float[] nicheY = { 0.35f, 1.35f, 2.45f };
            foreach (float y in nicheY)
            {
                foreach (float x in nicheX)
                {
                    Vector3 candidate = new Vector3(x, y, 0.45f);
                    if (!isProbeTooClose(candidate, positions, minProbeSpacingSq))
                    {
                        positions.Add(candidate);
                    }
                }
            }

            lpg.probePositions = positions.ToArray();
            EditorUtility.SetDirty(lpg);
            Debug.Log($"[ControlRoomQuality] LightProbeGroup 구성 완료: 총 {positions.Count}개 프로브 (중복 제거 적용)");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] setupLightProbeGroup 실패: {ex.Message}");
            throw;
        }
    }

    private static bool isProbeTooClose(Vector3 candidate, List<Vector3> existing, float minSqDist)
    {
        for (int i = 0; i < existing.Count; i++)
        {
            if ((existing[i] - candidate).sqrMagnitude < minSqDist)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 프로브 위치가 데스크/캐비닛 내부에 겹치는 경우 위쪽(데스크 상판 위)으로 살짝 밀어내어 검은 프로브를 방지합니다.
    /// </summary>
    private static Vector3 adjustProbeOutOfSolids(Vector3 pos, List<Bounds> solids)
    {
        try
        {
            Vector3 adjusted = pos;
            for (int i = 0; i < solids.Count; i++)
            {
                Bounds b = solids[i];
                if (b.Contains(adjusted))
                {
                    float topY = b.max.y + 0.08f;
                    if (topY < 2.80f)
                    {
                        adjusted.y = topY;
                    }
                }
            }
            return adjusted;
        }
        catch
        {
            return pos;
        }
    }

    /// <summary>
    /// 실내 공간 전체와 주요 구역(중앙, 전면 HMI, 좌측, 우측 입구)을 커버하는 Box Projection Reflection Probe 5개를 배치합니다.
    /// </summary>
    private static List<ReflectionProbe> setupReflectionProbes(Transform rigParent)
    {
        List<ReflectionProbe> result = new List<ReflectionProbe>();
        try
        {
            Transform rpRoot = rigParent.Find("ReflectionProbes");
            if (rpRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(rpRoot.gameObject);
            }
            rpRoot = new GameObject("ReflectionProbes").transform;
            rpRoot.SetParent(rigParent, false);

            result.Add(createReflectionProbe(
                rpRoot, "RP_Room_Master",
                new Vector3(-7.0f, 1.50f, -5.0f), new Vector3(14.4f, 3.2f, 11.4f),
                importance: 1, resolution: 512, blendDistance: 1.5f, intensity: 1.60f
            ));
            result.Add(createReflectionProbe(
                rpRoot, "RP_Center_Operator",
                new Vector3(-7.0f, 1.50f, -4.6f), new Vector3(6.8f, 3.3f, 5.2f),
                importance: 2, resolution: 512, blendDistance: 1.2f, intensity: 1.65f
            ));
            result.Add(createReflectionProbe(
                rpRoot, "RP_Front_HMI_Wall",
                new Vector3(-7.0f, 1.50f, -7.8f), new Vector3(12.8f, 3.3f, 4.4f),
                importance: 2, resolution: 512, blendDistance: 1.2f, intensity: 1.65f
            ));
            result.Add(createReflectionProbe(
                rpRoot, "RP_Left_Side",
                new Vector3(-2.4f, 1.45f, -3.0f), new Vector3(5.6f, 3.0f, 7.6f),
                importance: 2, resolution: 128, blendDistance: 1.2f, intensity: 1.45f
            ));
            result.Add(createReflectionProbe(
                rpRoot, "RP_Right_Entry",
                new Vector3(-11.55f, 1.45f, -1.75f), new Vector3(5.2f, 3.0f, 6.2f),
                importance: 2, resolution: 128, blendDistance: 1.2f, intensity: 1.45f
            ));
            // 입구 니치 스테인리스 패널(Wall_SS_Panel)이 밝은 실내 천장·바닥 반사 시야를 확보하도록 배치
            result.Add(createReflectionProbe(
                rpRoot, "RP_Entry_Niche",
                new Vector3(-11.05f, 1.60f, -1.15f), new Vector3(4.6f, 3.2f, 4.4f),
                importance: 3, resolution: 256, blendDistance: 1.0f, intensity: 2.40f
            ));
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] setupReflectionProbes 실패: {ex.Message}");
            throw;
        }
        return result;
    }

    private static ReflectionProbe createReflectionProbe(
        Transform parent,
        string probeName,
        Vector3 worldCenter,
        Vector3 boxSize,
        int importance,
        int resolution,
        float blendDistance,
        float intensity
    )
    {
        GameObject go = new GameObject(probeName);
        go.transform.SetParent(parent, false);
        go.transform.position = worldCenter;

        ReflectionProbe rp = go.AddComponent<ReflectionProbe>();
        rp.mode = ReflectionProbeMode.Baked;
        rp.refreshMode = ReflectionProbeRefreshMode.OnAwake;
        rp.center = Vector3.zero;
        rp.size = boxSize;
        rp.boxProjection = true;
        rp.importance = importance;
        rp.resolution = resolution;
        rp.hdr = true;
        rp.blendDistance = blendDistance;
        rp.intensity = intensity;
        rp.nearClipPlane = 0.1f;
        rp.farClipPlane = 30f;
        rp.clearFlags = ReflectionProbeClearFlags.SolidColor;
        rp.backgroundColor = new Color(0.18f, 0.21f, 0.26f, 1f);
        rp.cullingMask = ~0;
        return rp;
    }

    /// <summary>
    /// 중복 고아 ReflectionProbe EXR을 정리하고 Lightmapping.Bake() 결과 큐브맵 중 마스터 큐브맵을 RenderSettings.customReflectionTexture로 연결합니다.
    /// </summary>
    private static void bakeAndAssignReflectionProbes(List<ReflectionProbe> probes, string sceneSubFolder)
    {
        try
        {
            // 이전 수동 베이크 폴더(ReflectionProbes/ControlRoom/*.exr)의 고아 중복 큐브맵 제거
            string legacyProbeFolder = $"{probeBakeFolder}/{sceneSubFolder}";
            if (AssetDatabase.IsValidFolder(legacyProbeFolder))
            {
                AssetDatabase.DeleteAsset(legacyProbeFolder);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            Cubemap masterCubemap = null;
            string sceneBakedDir = $"Assets/Art/ControlRoom/Scene/{sceneSubFolder}";

            for (int i = 0; i < probes.Count; i++)
            {
                ReflectionProbe rp = probes[i];
                if (rp == null) continue;

                Texture bakedTex = rp.bakedTexture;
                if (bakedTex == null)
                {
                    string builtInPath = $"{sceneBakedDir}/ReflectionProbe-{i}.exr";
                    bakedTex = AssetDatabase.LoadAssetAtPath<Cubemap>(builtInPath);
                    if (bakedTex == null)
                    {
                        Directory.CreateDirectory(Path.GetFullPath(Path.Combine(Application.dataPath, "..", sceneBakedDir)));
                        if (Lightmapping.BakeReflectionProbe(rp, builtInPath))
                        {
                            AssetDatabase.ImportAsset(builtInPath, ImportAssetOptions.ForceSynchronousImport);
                            bakedTex = AssetDatabase.LoadAssetAtPath<Cubemap>(builtInPath);
                        }
                    }
                    if (bakedTex != null)
                    {
                        rp.bakedTexture = bakedTex;
                        EditorUtility.SetDirty(rp);
                    }
                }

                if (rp.gameObject.name == "RP_Room_Master" && bakedTex is Cubemap masterCube)
                {
                    masterCubemap = masterCube;
                }
            }

            if (masterCubemap != null)
            {
                RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
                RenderSettings.customReflectionTexture = masterCubemap;
                RenderSettings.reflectionIntensity = 1.40f;
                Debug.Log($"[ControlRoomQuality] Master Reflection Cubemap 연결 완료(중복 EXR 없음): {masterCubemap.name}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] bakeAndAssignReflectionProbes 실패: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Global Volume 배치 및 씬 내 카메라들의 renderPostProcessing 활성화·배경색 어두운 실내 보정을 적용합니다.
    /// 카메라의 enabled/tag/fov 등 기존 XR 동작 설정은 변경하지 않습니다.
    /// </summary>
    private static void setupGlobalVolumeAndCameras(Transform rigParent)
    {
        try
        {
            Transform existingVol = rigParent.Find("ControlRoom_GlobalVolume");
            if (existingVol != null)
            {
                UnityEngine.Object.DestroyImmediate(existingVol.gameObject);
            }

            GameObject volGo = new GameObject("ControlRoom_GlobalVolume");
            volGo.transform.SetParent(rigParent, false);
            Volume vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 1f;
            vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(volumeProfilePath);

            // 카메라 PostProcessing 켜기 + Skybox==null일 때 문 하단 틈새 파란 배경색 노출 방지
            Color darkInteriorBg = new Color(0.022f, 0.026f, 0.034f, 1f);
            foreach (Camera cam in UnityEngine.Object.FindObjectsOfType<Camera>(true))
            {
                if (cam == null) continue;
                cam.backgroundColor = darkInteriorBg;
                var uac = cam.GetComponent<UniversalAdditionalCameraData>();
                if (uac == null) uac = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                uac.renderPostProcessing = true;
                EditorUtility.SetDirty(cam);
                EditorUtility.SetDirty(uac);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] setupGlobalVolumeAndCameras 실패: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 부모 Transform 아래에 지정 이름의 자식 Transform을 찾거나 생성합니다.
    /// </summary>
    private static Transform ensureChild(Transform parent, string childName)
    {
        Transform t = parent.Find(childName);
        if (t == null)
        {
            t = new GameObject(childName).transform;
            t.SetParent(parent, false);
        }
        return t;
    }

    /// <summary>
    /// Step 6: 씬 무결성, AO, 라이트 프로브, 리플렉션 프로브, 스크린 머티리얼, Quest URP 성능을 자동 검증합니다.
    /// </summary>
    [MenuItem("ControlRoom/Quality/4. Verify Quality & Performance")]
    public static void verifyQualityAndPerformance()
    {
        try
        {
            var scene = EditorSceneManager.OpenScene(targetScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) throw new Exception("ControlRoom 씬 로드 실패");

            List<string> failures = new List<string>();

            // 1. ControlRoom.unity 기본 계층(비활성 오브젝트 포함) 및 ScreenSlot 39개 무결성 확인
            GameObject crRoot = GameObject.Find("ControlRoom");
            if (crRoot == null)
            {
                failures.Add("필수 루트 오브젝트 누락: ControlRoom");
            }
            else
            {
                string[] requiredChildren = { "Room_Shell", "Lights", "Cameras", "OperatorDesks", "Stations" };
                foreach (string req in requiredChildren)
                {
                    if (crRoot.transform.Find(req) == null)
                    {
                        failures.Add($"필수 하위 오브젝트 누락: ControlRoom/{req}");
                    }
                }
            }
            ScreenSlot[] slots = UnityEngine.Object.FindObjectsOfType<ScreenSlot>(true);
            if (slots.Length != 39)
            {
                failures.Add($"ScreenSlot 수 불일치: 기대 39, 실제 {slots.Length}");
            }

            // 2. Missing Script / Missing Material 검사
            foreach (GameObject go in UnityEngine.Object.FindObjectsOfType<GameObject>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go) > 0)
                {
                    failures.Add($"Missing Script 발견: {go.name}");
                }
            }
            foreach (MeshRenderer mr in UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true))
            {
                foreach (Material m in mr.sharedMaterials)
                {
                    if (m == null) failures.Add($"Missing Material 발견: {mr.name}");
                }
            }

            // 3. LightingSettings 검증 (AO + CombinedDirectional + IndirectOnly)
            LightingSettings ls = Lightmapping.lightingSettings;
            if (ls == null || !ls.ao || ls.directionalityMode != LightmapsMode.CombinedDirectional || ls.mixedBakeMode != MixedLightingMode.IndirectOnly)
            {
                failures.Add("LightingSettings 검증 실패 (AO / CombinedDirectional / IndirectOnly)");
            }

            // 4. LightProbeGroup 검증
            LightProbeGroup lpg = UnityEngine.Object.FindObjectOfType<LightProbeGroup>(true);
            if (lpg == null || lpg.probePositions.Length < 150)
            {
                failures.Add($"LightProbeGroup 검증 실패: count={(lpg != null ? lpg.probePositions.Length : 0)}");
            }

            // 5. ReflectionProbe 5개 이상 검증
            ReflectionProbe[] probes = UnityEngine.Object.FindObjectsOfType<ReflectionProbe>(true);
            if (probes.Length < 5)
            {
                failures.Add($"ReflectionProbe 수 부족: {probes.Length}");
            }
            foreach (ReflectionProbe rp in probes)
            {
                if (rp.mode != ReflectionProbeMode.Baked || !rp.boxProjection || rp.bakedTexture == null)
                {
                    failures.Add($"ReflectionProbe 검증 실패: {rp.name} (mode={rp.mode}, box={rp.boxProjection}, tex={rp.bakedTexture})");
                }
            }

            // 6. 실시간 그림자 라이트 0개 검증
            foreach (Light li in UnityEngine.Object.FindObjectsOfType<Light>(true))
            {
                if (li.lightmapBakeType != LightmapBakeType.Baked && li.shadows != LightShadows.None)
                {
                    failures.Add($"실시간/Mixed 라이트에 그림자가 켜져 있음: {li.name}");
                }
            }

            // 7. URP-Balanced.asset Quest 성능 설정 검증
            UniversalRenderPipelineAsset balanced = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/URP-Balanced.asset");
            if (balanced == null || balanced.msaaSampleCount != 4 || balanced.supportsCameraDepthTexture || balanced.supportsCameraOpaqueTexture)
            {
                failures.Add("URP-Balanced Quest 성능 설정 검증 실패");
            }
            if (balanced != null)
            {
                SerializedObject so = new SerializedObject(balanced);
                var boxProp = so.FindProperty("m_ReflectionProbeBoxProjection");
                var blendProp = so.FindProperty("m_ReflectionProbeBlending");
                if (boxProp == null || boxProp.intValue != 1 || blendProp == null || blendProp.intValue != 1)
                {
                    failures.Add("URP-Balanced ReflectionProbe BoxProjection/Blending 비활성화됨");
                }
            }

            // 8. VolumeProfile 서브에셋 직렬화 무결성 검증
            VolumeProfile vp = AssetDatabase.LoadAssetAtPath<VolumeProfile>(volumeProfilePath);
            if (vp == null || vp.components.Count < 4)
            {
                failures.Add("ControlRoom_VolumeProfile 에셋 또는 컴포넌트 누락");
            }
            else
            {
                foreach (VolumeComponent vc in vp.components)
                {
                    if (vc == null || !AssetDatabase.Contains(vc))
                    {
                        failures.Add($"ControlRoom_VolumeProfile 서브에셋 직렬화 누락: {(vc != null ? vc.name : "NULL")}");
                    }
                }
            }

            // 9. 스크린 머티리얼 _EmissionMap 유지 및 비발광 머티리얼 _EMISSION 키워드 누수 차단 검증
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { materialsRootPath }))
            {
                Material mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (mat == null) continue;
                if (mat.name.StartsWith("MAT_Screen_"))
                {
                    if (mat.GetTexture("_EmissionMap") == null || !mat.IsKeywordEnabled("_EMISSION"))
                    {
                        failures.Add($"스크린 머티리얼 검증 실패: {mat.name}");
                    }
                }
                else if (!mat.name.StartsWith("MAT_LED_") && !mat.name.StartsWith("MAT_LCD_"))
                {
                    if (mat.IsKeywordEnabled("_EMISSION"))
                    {
                        failures.Add($"비발광 머티리얼에 _EMISSION 키워드 누수: {mat.name}");
                    }
                }
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string verifyPath = Path.Combine(projectRoot, "Logs", "ControlRoom_VerifyResult.txt");
            if (failures.Count > 0)
            {
                string msg = "VERIFY FAILED:\n" + string.Join("\n", failures);
                File.WriteAllText(verifyPath, msg, Encoding.UTF8);
                throw new Exception(msg);
            }

            string passMsg = $"VERIFY PASSED: ScreenSlots={slots.Length}, LightProbes={lpg.probePositions.Length}, ReflectionProbes={probes.Length}";
            File.WriteAllText(verifyPath, passMsg, Encoding.UTF8);
            Debug.Log("[ControlRoomQuality] " + passMsg);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] verifyQualityAndPerformance 실패: {ex.Message}");
            throw;
        }
    }

    private static StringBuilder buildUvAuditReport(string scenePath)
    {
        StringBuilder sb = new StringBuilder();
        try
        {
            sb.AppendLine($"=== ControlRoom UV & Scene Audit ({scenePath}) ===");
            sb.AppendLine($"ColorSpace: {PlayerSettings.colorSpace}");
            sb.AppendLine($"Active RenderPipeline: {(GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.name : "None")}");
            sb.AppendLine($"QualityLevel: {QualitySettings.names[QualitySettings.GetQualityLevel()]} (index={QualitySettings.GetQualityLevel()})");
            sb.AppendLine($"RenderSettings.ambientMode: {RenderSettings.ambientMode}, sky={RenderSettings.ambientSkyColor}, defaultReflection={RenderSettings.defaultReflectionMode}");
            sb.AppendLine($"Lightmapping.lightingSettings: {(Lightmapping.lightingSettings != null ? Lightmapping.lightingSettings.name : "None")}");

            MeshRenderer[] renderers = UnityEngine.Object.FindObjectsOfType<MeshRenderer>(true);
            sb.AppendLine($"Total MeshRenderers: {renderers.Length}");

            int totalTris = 0;
            int missingUv0Count = 0;
            int missingUv1Count = 0;
            int missingMatCount = 0;
            HashSet<string> auditedMeshes = new HashSet<string>();

            sb.AppendLine();
            sb.AppendLine("--- MeshRenderer Detail ---");
            foreach (MeshRenderer mr in renderers)
            {
                if (mr == null) continue;
                MeshFilter mf = mr.GetComponent<MeshFilter>();
                Mesh mesh = mf != null ? mf.sharedMesh : null;
                string goPath = getHierarchyPath(mr.transform);

                if (mesh == null)
                {
                    sb.AppendLine($"[NO_MESH] {goPath}");
                    continue;
                }

                int tris = mesh.triangles.Length / 3;
                totalTris += tris;

                List<Vector2> uv0 = new List<Vector2>();
                List<Vector2> uv1 = new List<Vector2>();
                mesh.GetUVs(0, uv0);
                mesh.GetUVs(1, uv1);

                bool hasUv0 = uv0.Count > 0;
                bool hasUv1 = uv1.Count > 0;
                if (!hasUv0) missingUv0Count++;
                if (!hasUv1) missingUv1Count++;

                string uv0Range = computeUvRange(uv0);
                string uv1Range = computeUvRange(uv1);

                List<string> matNames = new List<string>();
                foreach (Material m in mr.sharedMaterials)
                {
                    if (m == null) { missingMatCount++; matNames.Add("NULL"); }
                    else matNames.Add(m.name);
                }

                StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(mr.gameObject);
                Bounds b = mr.bounds;
                string meshKey = $"{mesh.name}_{mesh.vertexCount}_{tris}";
                bool firstSeen = auditedMeshes.Add(meshKey);

                sb.AppendLine(
                    $"{(firstSeen ? "[MESH]" : "[INST]")} {goPath} | mesh={mesh.name} | verts={mesh.vertexCount} tris={tris} | " +
                    $"uv0={(hasUv0 ? uv0Range : "MISSING")} | uv1={(hasUv1 ? uv1Range : "MISSING")} | " +
                    $"boundsCenter=({b.center.x:F2},{b.center.y:F2},{b.center.z:F2}) size=({b.size.x:F2},{b.size.y:F2},{b.size.z:F2}) | " +
                    $"staticGI={(flags & StaticEditorFlags.ContributeGI) != 0} lmIndex={mr.lightmapIndex} scaleLM={mr.scaleInLightmap:F2} receiveGI={mr.receiveGI} | " +
                    $"mats=[{string.Join(", ", matNames)}]"
                );
            }

            sb.AppendLine();
            sb.AppendLine("=== Summary ===");
            sb.AppendLine($"Total Triangles (all renderers): {totalTris}");
            sb.AppendLine($"Renderers Missing UV0: {missingUv0Count}");
            sb.AppendLine($"Renderers Missing UV1: {missingUv1Count}");
            sb.AppendLine($"Missing Material Slots: {missingMatCount}");

            Light[] lights = UnityEngine.Object.FindObjectsOfType<Light>(true);
            ReflectionProbe[] rps = UnityEngine.Object.FindObjectsOfType<ReflectionProbe>(true);
            LightProbeGroup[] lpgs = UnityEngine.Object.FindObjectsOfType<LightProbeGroup>(true);
            Camera[] cams = UnityEngine.Object.FindObjectsOfType<Camera>(true);
            sb.AppendLine($"Lights: {lights.Length}, ReflectionProbes: {rps.Length}, LightProbeGroups: {lpgs.Length}, Cameras: {cams.Length}");
            foreach (Camera cam in cams)
            {
                var uac = cam.GetComponent<UniversalAdditionalCameraData>();
                sb.AppendLine($"  Camera: {getHierarchyPath(cam.transform)} enabled={cam.enabled} tag={cam.tag} fov={cam.fieldOfView} postProc={(uac != null && uac.renderPostProcessing)}");
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"ERROR during buildUvAuditReport: {ex.Message}");
        }
        return sb;
    }

    private static string computeUvRange(List<Vector2> uvs)
    {
        try
        {
            if (uvs == null || uvs.Count == 0) return "EMPTY";
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < uvs.Count; i++)
            {
                Vector2 v = uvs[i];
                if (v.x < minX) minX = v.x;
                if (v.y < minY) minY = v.y;
                if (v.x > maxX) maxX = v.x;
                if (v.y > maxY) maxY = v.y;
            }
            string warn = ((maxX - minX) < 0.001f && (maxY - minY) < 0.001f) ? " [DEGENERATE]" : "";
            return $"[{minX:F2}..{maxX:F2}, {minY:F2}..{maxY:F2}]{warn}";
        }
        catch (Exception ex)
        {
            return $"ERR({ex.Message})";
        }
    }

    private static string getHierarchyPath(Transform t)
    {
        try
        {
            List<string> parts = new List<string>();
            Transform cur = t;
            while (cur != null) { parts.Add(cur.name); cur = cur.parent; }
            parts.Reverse();
            return string.Join("/", parts);
        }
        catch
        {
            return t != null ? t.name : "null";
        }
    }

    private static void captureCameraScreenshots(string outputDir, bool includeCloseupCameras)
    {
        List<GameObject> tempCams = new List<GameObject>();
        try
        {
            Directory.CreateDirectory(outputDir);
            string[] targetCamNames = { "CAM_Door_View", "CAM_Operator_POV", "CAM_Station_Close", "CAM_Back_Wall" };
            List<Camera> camsToCapture = new List<Camera>();

            foreach (Camera cam in UnityEngine.Object.FindObjectsOfType<Camera>(true))
            {
                if (Array.IndexOf(targetCamNames, cam.name) >= 0) camsToCapture.Add(cam);
            }

            if (includeCloseupCameras)
            {
                // 실사 1:1 대조용 검증 카메라 4컷 (캡처 후 즉시 삭제)
                camsToCapture.Add(createTempCloseupCamera(
                    "CAM_Ref_DeskAndFloor",
                    new Vector3(-6.15f, 1.38f, -2.15f),
                    new Vector3(-7.00f, 0.82f, -3.35f),
                    56f,
                    tempCams
                ));
                camsToCapture.Add(createTempCloseupCamera(
                    "CAM_Ref_EntryProps",
                    new Vector3(-10.45f, 1.30f, -1.55f),
                    new Vector3(-12.35f, 0.95f, 0.20f),
                    58f,
                    tempCams
                ));
                camsToCapture.Add(createTempCloseupCamera(
                    "CAM_Ref_RackCabinet",
                    new Vector3(-3.25f, 1.25f, -2.05f),
                    new Vector3(-1.15f, 1.05f, -0.75f),
                    56f,
                    tempCams
                ));
                camsToCapture.Add(createTempCloseupCamera(
                    "CAM_Ref_CeilingAndTV",
                    new Vector3(-7.00f, 1.65f, -4.60f),
                    new Vector3(-7.00f, 2.25f, -9.50f),
                    64f,
                    tempCams
                ));
            }

            foreach (Camera cam in camsToCapture)
            {
                if (cam == null) continue;
                RenderTexture hdrRt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.DefaultHDR, RenderTextureReadWrite.Linear);
                hdrRt.antiAliasing = 4;
                hdrRt.Create();

                RenderTexture srgbRt = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                srgbRt.Create();

                bool wasEnabled = cam.enabled;
                bool prevAllowHdr = cam.allowHDR;
                RenderTexture prevTarget = cam.targetTexture;
                RenderTexture prevActive = RenderTexture.active;

                try
                {
                    cam.enabled = true;
                    cam.allowHDR = true;
                    cam.targetTexture = hdrRt;
                    cam.Render();

                    Graphics.Blit(hdrRt, srgbRt);
                    RenderTexture.active = srgbRt;
                    Texture2D tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                    tex.Apply();

                    string filePath = Path.Combine(outputDir, cam.name + ".png");
                    File.WriteAllBytes(filePath, tex.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(tex);
                    Debug.Log($"[ControlRoomQuality] HDR 스크린샷 저장: {filePath}");
                }
                finally
                {
                    cam.targetTexture = prevTarget;
                    cam.allowHDR = prevAllowHdr;
                    cam.enabled = wasEnabled;
                    RenderTexture.active = prevActive;
                    hdrRt.Release();
                    srgbRt.Release();
                    UnityEngine.Object.DestroyImmediate(hdrRt);
                    UnityEngine.Object.DestroyImmediate(srgbRt);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] captureCameraScreenshots 실패: {ex.Message}");
            throw;
        }
        finally
        {
            foreach (GameObject go in tempCams)
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }

    private static Camera createTempCloseupCamera(string camName, Vector3 position, Vector3 lookTarget, float fov, List<GameObject> tracker)
    {
        try
        {
            GameObject go = new GameObject(camName);
            go.transform.position = position;
            go.transform.LookAt(lookTarget);
            Camera cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 40f;
            cam.backgroundColor = new Color(0.022f, 0.026f, 0.034f, 1f);
            var uac = go.GetComponent<UniversalAdditionalCameraData>();
            if (uac == null) uac = go.AddComponent<UniversalAdditionalCameraData>();
            uac.renderPostProcessing = true;
            tracker.Add(go);
            return cam;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ControlRoomQuality] createTempCloseupCamera 실패: {ex.Message}");
            return null;
        }
    }
}
