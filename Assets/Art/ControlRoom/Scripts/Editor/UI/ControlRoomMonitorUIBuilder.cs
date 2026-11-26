using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ControlRoom.UI.Editor
{
    /// <summary>
    /// ControlRoom.unity 중앙 책상(OP_CENTER)의 3대 모니터 앞에 16:9 World-Space UI Canvas를 구축하고
    /// Capture[00..49] 및 교육자료 기준 10종 검증 스크린샷을 생성하는 에디터 빌더입니다.
    /// </summary>
    public static class ControlRoomMonitorUIBuilder
    {
        private const string CONTROL_ROOM_SCENE_PATH = "Assets/Art/ControlRoom/Scene/ControlRoom.unity";
        private const string UI_ASSET_ROOT = "Assets/Art/ControlRoom/UI";
        private const string FONT_ASSET_PATH = "Assets/Art/ControlRoom/UI/Fonts/MalgunGothic SDF.asset";
        private const string FONT_TTF_PATH = "Assets/Art/ControlRoom/UI/Fonts/malgun.ttf";
        private const string PREFAB_OUTPUT_PATH = "Assets/Art/ControlRoom/Prefabs/UI/PF_OP_CENTER_MonitorStation.prefab";
        private const string PREFAB_SCENARIO2_OUTPUT_PATH = "Assets/Art/ControlRoom/Prefabs/UI/PF_OP_CENTER_MonitorStation_Scenario2.prefab";
        private const string PREFAB_WALL_TV_OUTPUT_PATH = "Assets/Art/ControlRoom/Prefabs/UI/PF_WallTV_DynamicOverlays.prefab";
        private const string XRI_INPUT_ACTIONS_PATH = "Assets/Samples/XR Interaction Toolkit/2.6.5/Starter Assets/XRI Default Input Actions.inputactions";
        private const string VERIFY_RENDER_DIR = @"C:\Users\render3\.gemini\antigravity\brain\31967b68-7f5f-4124-b60f-5e189e46a3d5\scratch\renders";
        private const string WALL_VERIFY_RENDER_DIR = @"C:\Users\render3\.gemini\antigravity\brain\633d62f3-e796-456e-bae3-a700b21d98f1\scratch\wall_verify";

        public struct ScreenTransformInfo
        {
            public Vector3 centerWorld;
            public Vector3 outwardNormal;
            public float widthMeters;
            public float heightMeters;
        }

        [MenuItem("Tools/ControlRoom UI/1. Build OP_CENTER 3-Monitor UI")]
        public static void buildControlRoomMonitorUI()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                configureSpriteImporters();
                TMP_FontAsset koreanFont = ensureKoreanFontAsset();

                Scene scene = EditorSceneManager.OpenScene(CONTROL_ROOM_SCENE_PATH, OpenSceneMode.Single);
                if (!scene.IsValid()) throw new Exception($"씬을 열 수 없습니다: {CONTROL_ROOM_SCENE_PATH}");

                // 모니터 스크린 등 기존 씬 오브젝트에 남아있던 Missing Script 컴포넌트 일괄 정리
                int removedMissing = removeMissingScriptsInScene(scene);

                GameObject opCenter = findSceneObject("OP_CENTER");
                GameObject chairObj = findSceneObject("OP_CENTER_Chair");
                GameObject leftScreenObj = findMonitorTarget("OP_CENTER_Monitor_L_Screen", "OP_CENTER_Monitor_L");
                GameObject centerScreenObj = findMonitorTarget("OP_CENTER_Monitor_C_Screen", "OP_CENTER_Monitor_C");
                GameObject rightScreenObj = findMonitorTarget("OP_CENTER_Monitor_R_Screen", "OP_CENTER_Monitor_R");

                if (opCenter == null || leftScreenObj == null || centerScreenObj == null || rightScreenObj == null)
                {
                    throw new Exception("ControlRoom 씬에서 OP_CENTER 또는 모니터 스크린 오브젝트를 찾을 수 없습니다.");
                }

                Vector3 viewerRefPos = chairObj != null
                    ? getObjectBoundsCenter(chairObj) + Vector3.up * 0.6f
                    : getObjectBoundsCenter(centerScreenObj) + Vector3.forward * 0.9f;

                destroyIfExists("UI_OP_CENTER_MonitorStation");
                destroyIfExists("Canvas_VRTrainingHelper");

                ScreenTransformInfo leftInfoBase = computeScreenPlacement(leftScreenObj, viewerRefPos);
                ScreenTransformInfo centerInfoBase = computeScreenPlacement(centerScreenObj, viewerRefPos);
                // 책상 중심 피벗(OP_1/2/3 공용 프리팹 로컬 기준)에 stationRoot를 배치해 4개 책상 모두 정확히 정렬되도록 유지
                Vector3 deskPivot = new Vector3(
                    centerInfoBase.centerWorld.x,
                    centerScreenObj.transform.position.y,
                    leftInfoBase.centerWorld.z - 0.135343f);

                var stationRoot = new GameObject("UI_OP_CENTER_MonitorStation");
                stationRoot.transform.SetParent(opCenter.transform, false);
                stationRoot.transform.position = deskPivot;
                stationRoot.transform.rotation = Quaternion.identity;
                stationRoot.transform.localScale = Vector3.one;

                // 3개 순수 16:9 모니터 Canvas만 생성 (교육자료와 다른 외부 오버레이 없음)
                Canvas leftCanvas = createMonitorCanvas(stationRoot.transform, "Canvas_LeftMonitor", leftScreenObj, viewerRefPos);
                Canvas centerCanvas = createMonitorCanvas(stationRoot.transform, "Canvas_CenterMonitor", centerScreenObj, viewerRefPos);
                Canvas rightCanvas = createMonitorCanvas(stationRoot.transform, "Canvas_RightMonitor", rightScreenObj, viewerRefPos);

                ControlRoomSpritePack spritePack = loadSpritePack();
                AudioClip bgClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{UI_ASSET_ROOT}/Audio/server_room_noise_bg.wav");
                AudioClip clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{UI_ASSET_ROOT}/Audio/01_mouse_click.wav");

                var coordinator = stationRoot.AddComponent<OperatorStationCoordinator>();
                coordinator.configureReferences(leftCanvas, centerCanvas, rightCanvas, koreanFont, spritePack, bgClip, clickClip);
                coordinator.initializeStation();
                persistGuideArrowAssets(coordinator.GuideArrow);

                ensureXrEventSystemInScene();

                string prefabDir = Path.GetDirectoryName(PREFAB_OUTPUT_PATH);
                if (!Directory.Exists(Path.GetFullPath(prefabDir)))
                {
                    Directory.CreateDirectory(Path.GetFullPath(prefabDir));
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }
                PrefabUtility.SaveAsPrefabAssetAndConnect(stationRoot, PREFAB_OUTPUT_PATH, InteractionMode.AutomatedAction);

                EditorSceneManager.MarkSceneDirty(scene);
                bool saved = EditorSceneManager.SaveScene(scene, CONTROL_ROOM_SCENE_PATH);
                AssetDatabase.SaveAssets();

                Debug.Log($"[ControlRoomMonitorUIBuilder] BUILD_MONITOR_UI_SUCCESS | RemovedMissingScripts={removedMissing} | Saved={saved} | Scene={CONTROL_ROOM_SCENE_PATH}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ControlRoomMonitorUIBuilder] buildControlRoomMonitorUI 실패: {ex}");
                throw;
            }
        }

        [MenuItem("Tools/ControlRoom UI/3. Build OP_CENTER Scenario 2 Prefab")]
        public static void buildScenario2MonitorUIPrefab()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                configureSpriteImporters();
                TMP_FontAsset koreanFont = ensureKoreanFontAsset();

                Scene scene = EditorSceneManager.OpenScene(CONTROL_ROOM_SCENE_PATH, OpenSceneMode.Single);
                if (!scene.IsValid()) throw new Exception($"씬을 열 수 없습니다: {CONTROL_ROOM_SCENE_PATH}");

                int removedMissing = removeMissingScriptsInScene(scene);

                GameObject opCenter = findSceneObject("OP_CENTER");
                GameObject chairObj = findSceneObject("OP_CENTER_Chair");
                GameObject leftScreenObj = findMonitorTarget("OP_CENTER_Monitor_L_Screen", "OP_CENTER_Monitor_L");
                GameObject centerScreenObj = findMonitorTarget("OP_CENTER_Monitor_C_Screen", "OP_CENTER_Monitor_C");
                GameObject rightScreenObj = findMonitorTarget("OP_CENTER_Monitor_R_Screen", "OP_CENTER_Monitor_R");

                if (opCenter == null || leftScreenObj == null || centerScreenObj == null || rightScreenObj == null)
                {
                    throw new Exception("ControlRoom 씬에서 OP_CENTER 또는 모니터 스크린 오브젝트를 찾을 수 없습니다.");
                }

                Vector3 viewerRefPos = chairObj != null
                    ? getObjectBoundsCenter(chairObj) + Vector3.up * 0.6f
                    : getObjectBoundsCenter(centerScreenObj) + Vector3.forward * 0.9f;

                destroyIfExists("UI_OP_CENTER_MonitorStation_Scenario2");

                ScreenTransformInfo leftInfoBase = computeScreenPlacement(leftScreenObj, viewerRefPos);
                ScreenTransformInfo centerInfoBase = computeScreenPlacement(centerScreenObj, viewerRefPos);
                Vector3 deskPivot = new Vector3(
                    centerInfoBase.centerWorld.x,
                    centerScreenObj.transform.position.y,
                    leftInfoBase.centerWorld.z - 0.135343f);

                var stationRoot = new GameObject("UI_OP_CENTER_MonitorStation_Scenario2");
                stationRoot.transform.SetParent(opCenter.transform, false);
                stationRoot.transform.position = deskPivot;
                stationRoot.transform.rotation = Quaternion.identity;
                stationRoot.transform.localScale = Vector3.one;

                Canvas leftCanvas = createMonitorCanvas(stationRoot.transform, "Canvas_LeftMonitor", leftScreenObj, viewerRefPos);
                Canvas centerCanvas = createMonitorCanvas(stationRoot.transform, "Canvas_CenterMonitor", centerScreenObj, viewerRefPos);
                Canvas rightCanvas = createMonitorCanvas(stationRoot.transform, "Canvas_RightMonitor", rightScreenObj, viewerRefPos);

                ControlRoomSpritePack spritePack = loadSpritePack();
                AudioClip bgClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{UI_ASSET_ROOT}/Audio/server_room_noise_bg.wav");
                AudioClip clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{UI_ASSET_ROOT}/Audio/01_mouse_click.wav");

                var coordinator = stationRoot.AddComponent<Scenario2StationCoordinator>();
                coordinator.configureReferences(leftCanvas, centerCanvas, rightCanvas, koreanFont, spritePack, bgClip, clickClip);
                coordinator.initializeStation();
                persistGuideArrowAssets(coordinator.GuideArrow);

                ensureXrEventSystemInScene();

                string prefabDir = Path.GetDirectoryName(PREFAB_SCENARIO2_OUTPUT_PATH);
                if (!Directory.Exists(Path.GetFullPath(prefabDir)))
                {
                    Directory.CreateDirectory(Path.GetFullPath(prefabDir));
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }
                PrefabUtility.SaveAsPrefabAssetAndConnect(stationRoot, PREFAB_SCENARIO2_OUTPUT_PATH, InteractionMode.AutomatedAction);

                // 시나리오 1 기본 스테이션과 겹치지 않도록 씬 내 시나리오 2 프리팹 인스턴스는 비활성으로 배치 저장
                stationRoot.SetActive(false);

                EditorSceneManager.MarkSceneDirty(scene);
                bool saved = EditorSceneManager.SaveScene(scene, CONTROL_ROOM_SCENE_PATH);
                AssetDatabase.SaveAssets();

                Debug.Log($"[ControlRoomMonitorUIBuilder] BUILD_SCENARIO2_PREFAB_SUCCESS | RemovedMissingScripts={removedMissing} | Prefab={PREFAB_SCENARIO2_OUTPUT_PATH} | Saved={saved}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ControlRoomMonitorUIBuilder] buildScenario2MonitorUIPrefab 실패: {ex}");
                throw;
            }
        }

        [MenuItem("Tools/ControlRoom UI/4. Build Wall TV Dynamic Overlays")]
        public static void buildWallDisplayOverlays()
        {
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                configureSpriteImporters();
                TMP_FontAsset koreanFont = ensureKoreanFontAsset();
                ControlRoomSpritePack spritePack = loadSpritePack();

                Scene scene = EditorSceneManager.OpenScene(CONTROL_ROOM_SCENE_PATH, OpenSceneMode.Single);
                if (!scene.IsValid()) throw new Exception($"씬을 열 수 없습니다: {CONTROL_ROOM_SCENE_PATH}");

                int removedMissing = removeMissingScriptsInScene(scene);

                GameObject stationsRootObj = findSceneObject("Stations");
                if (stationsRootObj == null)
                {
                    throw new Exception("ControlRoom 씬에서 Stations 오브젝트를 찾을 수 없습니다.");
                }
                GameObject chairObj = findSceneObject("OP_CENTER_Chair");
                GameObject ctrUpperObj = findSceneObject("ST_CTR_TV_Upper_Screen");
                GameObject u6UpperObj = findSceneObject("ST_06_TV_Upper_Screen");
                GameObject u6LowerObj = findSceneObject("ST_06_TV_Lower_Screen");

                if (ctrUpperObj == null || u6UpperObj == null || u6LowerObj == null)
                {
                    throw new Exception("ControlRoom 씬에서 ST_CTR_TV_Upper_Screen / ST_06_TV_Upper_Screen / ST_06_TV_Lower_Screen 오브젝트를 찾을 수 없습니다.");
                }

                Vector3 viewerRefPos = chairObj != null
                    ? getObjectBoundsCenter(chairObj) + Vector3.up * 0.8f
                    : getObjectBoundsCenter(ctrUpperObj) + Vector3.forward * 4.0f;

                destroyIfExists("UI_WallTV_DynamicOverlays");

                var wallOverlayRoot = new GameObject("UI_WallTV_DynamicOverlays");
                // 사용자 요청에 따라 ControlRoom/Stations 직속 자식으로 배치
                wallOverlayRoot.transform.SetParent(stationsRootObj.transform, false);
                wallOverlayRoot.transform.localPosition = Vector3.zero;
                wallOverlayRoot.transform.localRotation = Quaternion.identity;
                wallOverlayRoot.transform.localScale = Vector3.one;

                Canvas ctrCanvas = createDisplayOnlyWallCanvas(wallOverlayRoot.transform, "Canvas_Wall_CTR_Upper", ctrUpperObj, viewerRefPos);
                Canvas u6UpperCanvas = createDisplayOnlyWallCanvas(wallOverlayRoot.transform, "Canvas_Wall_Unit06_Upper", u6UpperObj, viewerRefPos);
                Canvas u6LowerCanvas = createDisplayOnlyWallCanvas(wallOverlayRoot.transform, "Canvas_Wall_Unit06_Lower", u6LowerObj, viewerRefPos);

                // 06호기 외 나머지 11개 호기(01~05, 07~12) 상단·하단 TV에도 하트비트 오버레이용 캔버스 생성
                var extraUnitCanvases = new List<Canvas>();
                var extraUnitLowerCanvases = new List<Canvas>();
                for (int unit = 1; unit <= 12; unit++)
                {
                    if (unit == 6) continue;
                    string screenName = $"ST_{unit:00}_TV_Upper_Screen";
                    GameObject unitUpperObj = findSceneObject(screenName);
                    if (unitUpperObj != null)
                    {
                        Canvas unitCanvas = createDisplayOnlyWallCanvas(
                            wallOverlayRoot.transform,
                            $"Canvas_Wall_Unit{unit:00}_Upper",
                            unitUpperObj,
                            viewerRefPos);
                        extraUnitCanvases.Add(unitCanvas);
                    }
                    else
                    {
                        Debug.LogWarning($"[ControlRoomMonitorUIBuilder] 상단 TV 스크린을 찾을 수 없어 하트비트 오버레이를 건너뜁니다: {screenName}");
                    }

                    string lowerScreenName = $"ST_{unit:00}_TV_Lower_Screen";
                    GameObject unitLowerObj = findSceneObject(lowerScreenName);
                    if (unitLowerObj != null)
                    {
                        Canvas unitLowerCanvas = createDisplayOnlyWallCanvas(
                            wallOverlayRoot.transform,
                            $"Canvas_Wall_Unit{unit:00}_Lower",
                            unitLowerObj,
                            viewerRefPos);
                        extraUnitLowerCanvases.Add(unitLowerCanvas);
                    }
                    else
                    {
                        Debug.LogWarning($"[ControlRoomMonitorUIBuilder] 하단 TV 스크린을 찾을 수 없어 하트비트 오버레이를 건너뜁니다: {lowerScreenName}");
                    }
                }

                var syncCtrl = wallOverlayRoot.AddComponent<WallDisplaySyncController>();
                syncCtrl.configureReferences(ctrCanvas, u6UpperCanvas, u6LowerCanvas, koreanFont, spritePack);
                syncCtrl.configureAdditionalUnitCanvases(extraUnitCanvases.ToArray());
                syncCtrl.configureAdditionalUnitLowerCanvases(extraUnitLowerCanvases.ToArray());
                syncCtrl.initializeWallDisplays();

                string prefabDir = Path.GetDirectoryName(PREFAB_WALL_TV_OUTPUT_PATH);
                if (!Directory.Exists(Path.GetFullPath(prefabDir)))
                {
                    Directory.CreateDirectory(Path.GetFullPath(prefabDir));
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }
                PrefabUtility.SaveAsPrefabAssetAndConnect(wallOverlayRoot, PREFAB_WALL_TV_OUTPUT_PATH, InteractionMode.AutomatedAction);

                // 기존 씬에 중앙 시나리오 매니저가 있다면 재생성된 벽면 TV 컨트롤러 참조를 자동 재연결
                var scenarioMgr = UnityEngine.Object.FindObjectOfType<ControlRoomScenarioManager>(true);
                if (scenarioMgr != null)
                {
                    scenarioMgr.configureReferences(scenarioMgr.Scenario1Coordinator, scenarioMgr.Scenario2Coordinator, syncCtrl);
                }

                // 오프스크린 시각 검증 캡처 (광각 3종 + 근접 클로즈업 3종)
                Directory.CreateDirectory(WALL_VERIFY_RENDER_DIR);
                ScreenTransformInfo ctrInfo = computeScreenPlacement(ctrUpperObj, viewerRefPos);
                ScreenTransformInfo u6UpInfo = computeScreenPlacement(u6UpperObj, viewerRefPos);
                ScreenTransformInfo u6LowInfo = computeScreenPlacement(u6LowerObj, viewerRefPos);
                Vector3 u6ColCenter = (u6UpInfo.centerWorld + u6LowInfo.centerWorld) * 0.5f;
                Vector3 pairCenter = (ctrInfo.centerWorld + u6ColCenter) * 0.5f;
                Vector3 wideCamPos = pairCenter + ctrInfo.outwardNormal * 3.85f;

                syncCtrl.updateSystemClock(new DateTime(2026, 11, 26, 10, 30, 45));
                syncCtrl.applyDisplayState(0, 0, 1, false, false, 0);
                captureWallVerifyShot("wall_verify_normal.png", wideCamPos, pairCenter, 52f);

                // 근접 클로즈업 3종 (ST_06 상단 녹색 박스, ST_CTR 상단 녹색 박스+메인 시계, ST_06 하단 하트비트+시계 제거 확인)
                captureWallVerifyShot("wall_verify_st06_closeup.png", u6UpInfo.centerWorld + u6UpInfo.outwardNormal * 1.45f, u6UpInfo.centerWorld, 46f);
                captureWallVerifyShot("wall_verify_ctr_closeup.png", ctrInfo.centerWorld + ctrInfo.outwardNormal * 1.85f, ctrInfo.centerWorld, 46f);
                captureWallVerifyShot("wall_verify_st06_lower_closeup.png", u6LowInfo.centerWorld + u6LowInfo.outwardNormal * 1.45f, u6LowInfo.centerWorld, 46f);

                syncCtrl.updateSystemClock(new DateTime(2026, 11, 26, 10, 31, 12));
                syncCtrl.applyDisplayState(2, 1, 1, true, true, 1);
                captureWallVerifyShot("wall_verify_loca.png", wideCamPos, pairCenter, 52f);

                Vector3 hbWideCamPos = ctrInfo.centerWorld + ctrInfo.outwardNormal * 5.2f;
                captureWallVerifyShot("wall_verify_wide_heartbeats.png", hbWideCamPos, ctrInfo.centerWorld, 60f);

                // 초기 정상 + 현재 시스템 시각으로 복원 후 저장
                syncCtrl.updateSystemClock(DateTime.Now);
                syncCtrl.applyDisplayState(0, 0, 1, false, false, 0);

                EditorSceneManager.MarkSceneDirty(scene);
                bool saved = EditorSceneManager.SaveScene(scene, CONTROL_ROOM_SCENE_PATH);
                AssetDatabase.SaveAssets();

                Debug.Log($"[ControlRoomMonitorUIBuilder] BUILD_WALL_TV_OVERLAYS_SUCCESS | RemovedMissingScripts={removedMissing} | ExtraUnitWallTVs={extraUnitCanvases.Count} | ExtraUnitLowerTVs={extraUnitLowerCanvases.Count} | Prefab={PREFAB_WALL_TV_OUTPUT_PATH} | Saved={saved}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ControlRoomMonitorUIBuilder] buildWallDisplayOverlays 실패: {ex}");
                throw;
            }
        }

        [MenuItem("Tools/ControlRoom UI/5. Setup Central Scenario Manager")]
        public static void setupCentralScenarioManager()
        {
            try
            {
                Scene scene = EditorSceneManager.OpenScene(CONTROL_ROOM_SCENE_PATH, OpenSceneMode.Single);
                if (!scene.IsValid()) throw new Exception($"씬을 열 수 없습니다: {CONTROL_ROOM_SCENE_PATH}");

                var s1 = UnityEngine.Object.FindObjectOfType<OperatorStationCoordinator>(true);
                var s2 = UnityEngine.Object.FindObjectOfType<Scenario2StationCoordinator>(true);
                var wall = UnityEngine.Object.FindObjectOfType<WallDisplaySyncController>(true);
                GameObject opCenter = findSceneObject("OP_CENTER");

                if (s1 == null || s2 == null)
                {
                    throw new Exception("OperatorStationCoordinator 또는 Scenario2StationCoordinator를 찾을 수 없습니다. 먼저 메뉴 1, 3을 실행해 주세요.");
                }

                destroyIfExists("ControlRoomScenarioManager");

                var mgrGo = new GameObject("ControlRoomScenarioManager");
                if (opCenter != null) mgrGo.transform.SetParent(opCenter.transform, false);
                mgrGo.transform.position = Vector3.zero;
                mgrGo.transform.rotation = Quaternion.identity;
                mgrGo.transform.localScale = Vector3.one;

                var mgr = mgrGo.AddComponent<ControlRoomScenarioManager>();
                mgr.configureReferences(s1, s2, wall);
                mgr.resetDefaultStepOrders();
                mgr.switchScenario(ControlRoomScenarioId.Scenario1, true);

                EditorSceneManager.MarkSceneDirty(scene);
                bool saved = EditorSceneManager.SaveScene(scene, CONTROL_ROOM_SCENE_PATH);
                AssetDatabase.SaveAssets();

                Debug.Log($"[ControlRoomMonitorUIBuilder] SETUP_SCENARIO_MANAGER_SUCCESS | Saved={saved} | WallLinked={wall != null}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ControlRoomMonitorUIBuilder] setupCentralScenarioManager 실패: {ex}");
                throw;
            }
        }

        [MenuItem("Tools/ControlRoom UI/2. Build + Capture Verification Renders")]
        public static void buildAndCaptureVerification()
        {
            try
            {
                buildControlRoomMonitorUI();
                Directory.CreateDirectory(VERIFY_RENDER_DIR);

                var coordinator = UnityEngine.Object.FindObjectOfType<OperatorStationCoordinator>();
                if (coordinator == null) throw new Exception("OperatorStationCoordinator를 찾을 수 없습니다.");
                if (coordinator.SimulationModel == null) coordinator.initializeStation();

                GameObject chairObj = findSceneObject("OP_CENTER_Chair");
                GameObject leftObj = findMonitorTarget("OP_CENTER_Monitor_L_Screen", "OP_CENTER_Monitor_L");
                GameObject centerObj = findMonitorTarget("OP_CENTER_Monitor_C_Screen", "OP_CENTER_Monitor_C");
                GameObject rightObj = findMonitorTarget("OP_CENTER_Monitor_R_Screen", "OP_CENTER_Monitor_R");

                Vector3 chairEye = chairObj != null ? getObjectBoundsCenter(chairObj) + new Vector3(0f, 0.72f, 0.16f) : new Vector3(-7.0f, 1.18f, -2.35f);
                Vector3 leftLook = getObjectBoundsCenter(leftObj);
                Vector3 centerLook = getObjectBoundsCenter(centerObj);
                Vector3 rightLook = getObjectBoundsCenter(rightObj);

                ScreenTransformInfo leftInfo = computeScreenPlacement(leftObj, chairEye);
                ScreenTransformInfo rightInfo = computeScreenPlacement(rightObj, chairEye);
                Vector3 centerFront = centerLook + new Vector3(0f, 0.0f, 0.54f);
                Vector3 leftFront = leftInfo.centerWorld + leftInfo.outwardNormal * 0.54f;
                Vector3 rightFront = rightInfo.centerWorld + rightInfo.outwardNormal * 0.54f;

                // 1. 초기 3모니터 전체 뷰 (Capture[00]: Paused Instructor + Unit Overview + SFM Unit 06) + 실사화 룸 전경 2종
                coordinator.SimulationModel.resetToStandby();
                captureOffscreenView("v3_01_3monitors_start.png", chairEye, centerLook, 58f);
                captureOffscreenView("v4_room_front.png", new Vector3(-7.0f, 1.55f, -0.35f), new Vector3(-7.0f, 1.25f, -5.5f), 60f);
                captureOffscreenView("v4_room_angle.png", new Vector3(-12.2f, 1.65f, -0.6f), new Vector3(-6.5f, 1.15f, -4.5f), 58f);

                // 2. 좌측 모니터 클로즈업 (Unit Overview)
                captureOffscreenView("v3_02_left_unit_overview.png", leftFront, leftLook, 38f);

                // 3. 우측 모니터 클로즈업 (12-Unit Overview 기본 화면 & 보존 SFM 검증)
                captureOffscreenView("v3_03_right_12unit_overview.png", rightFront, rightLook, 38f);

                // 4. 중앙 모니터: Play 클릭 -> Running (Capture[01])
                coordinator.MainMonitor.InstPlayButtonWidget.simulateClick();
                captureOffscreenView("v3_04_center_instructor_play.png", centerFront, centerLook, 38f);

                // 5. Process Library 열기 + IOP -> D. Power Operation 확장 (Capture[10])
                coordinator.MainMonitor.Chrome.ProcessLibraryNavWidget.simulateClick();
                coordinator.MainMonitor.TreeIopWidget.simulateClick();
                coordinator.MainMonitor.TreePowerOpWidget.simulateClick();
                captureOffscreenView("v3_05_center_proclib_tree.png", centerFront, centerLook, 38f);

                // 6. C. Power Reduction 미리보기 (Capture[12])
                coordinator.MainMonitor.TreePowerRedWidget.simulateClick();
                captureOffscreenView("v3_06_center_proc_preview.png", centerFront, centerLook, 38f);

                // 7. Start Procedure -> Step 1 체크 -> Step 3 컨텍스트 메뉴 (Capture[22])
                coordinator.MainMonitor.StartProcWidget.simulateClick();
                coordinator.MainMonitor.Step1CheckWidget.simulateClick();
                coordinator.MainMonitor.Step3CheckWidget.simulateClick();
                captureOffscreenView("v3_07_center_proc_step3_menu.png", centerFront, centerLook, 38f);

                // 8. N/A 선택 -> Link -> Power Maneuvering Setup -> Target 클릭으로 80/300 동시 입력 -> Validate (Capture[33])
                coordinator.MainMonitor.Step3NaMenuWidget.simulateClick();
                coordinator.MainMonitor.Step6LinkWidget.simulateClick();
                coordinator.MainMonitor.skipSetupPleaseWaitForVerification();
                coordinator.MainMonitor.PmTargetFieldWidget.simulateClick();
                coordinator.MainMonitor.PmValidateWidget.simulateClick();
                captureOffscreenView("v3_08_center_pm_setup_80_300.png", centerFront, centerLook, 38f);

                // 9. Execute -> Active C.Power Reduction 복귀 -> Step 6, 8 체크 (Capture[42])
                coordinator.MainMonitor.PmExecuteWidget.simulateClick();
                coordinator.MainMonitor.ProcActiveCardWidget.simulateClick();
                coordinator.MainMonitor.Step6CheckWidget.simulateClick();
                coordinator.MainMonitor.Step8CheckWidget.simulateClick();
                captureOffscreenView("v3_09_center_proc_57pct.png", centerFront, centerLook, 38f);

                // 10. Power Maneuvering Active 카드 클릭 -> 80% 램프 완료 대시보드 (Capture[49])
                coordinator.MainMonitor.PmActiveCardWidget.simulateClick();
                coordinator.SimulationModel.completePowerManeuverImmediately();
                captureOffscreenView("v3_10_center_pm_running_80pct.png", centerFront, centerLook, 38f);

                // 씬 저장 상태는 Capture[00] 시작 상태(Instructor Station Paused)로 복원
                coordinator.SimulationModel.resetToStandby();
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), CONTROL_ROOM_SCENE_PATH);

                Debug.Log($"[ControlRoomMonitorUIBuilder] VERIFY_RENDERS_COMPLETE | Dir={VERIFY_RENDER_DIR}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ControlRoomMonitorUIBuilder] buildAndCaptureVerification 실패: {ex}");
                throw;
            }
        }

        private static ControlRoomSpritePack loadSpritePack()
        {
            string s = $"{UI_ASSET_ROOT}/Sprites";
            string e = $"{s}/Extracted";
            string d = $"{s}/Diagrams";
            return new ControlRoomSpritePack
            {
                e2ChromeTop = AssetDatabase.LoadAssetAtPath<Sprite>($"{e}/e2_chrome_top.png"),
                e2ChromeBottom = AssetDatabase.LoadAssetAtPath<Sprite>($"{e}/e2_chrome_bottom.png"),
                e2LeftUnitOverview = AssetDatabase.LoadAssetAtPath<Sprite>($"{e}/e2_left_unit_overview.png"),
                e2RightSfm = AssetDatabase.LoadAssetAtPath<Sprite>($"{e}/e2_right_sfm.png"),
                e2LogoInstructor = AssetDatabase.LoadAssetAtPath<Sprite>($"{e}/e2_logo_instructor.png"),
                navDots = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_nav_dots.png"),
                menuList = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_menu_list.png"),
                chevronLeft = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_chevron_left.png"),
                chevronRight = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_chevron_right.png"),
                alarmOff = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_alm_oct_off.png"),
                alarmOn = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_alm_oct_on.png"),
                cautionOff = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_cau_tri_off.png"),
                cautionOn = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_cau_tri_on.png"),
                noticeOff = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_ntc_doc_off.png"),
                noticeOn = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_ntc_doc_on.png"),
                qbTree = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_qb_tree.png"),
                qbUnit = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_qb_unit.png"),
                qb12Unit = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_qb_12unit.png"),
                qbNotif = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_qb_notif.png"),
                qbEvent = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_qb_event.png"),
                qbProc = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_qb_proc.png"),
                qbInst = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_qb_inst.png"),
                infoIcon = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_info.png"),
                rodSymbol = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_rod_symbol.png"),
                docNote = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_doc_note.png"),
                bookBlue = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_book_blue.png"),
                icoCheckStep = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_check_step.png"),
                icoNoteDoc = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_note_doc.png"),
                icoSpeechBubble = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_speech_bubble.png"),
                icoBookBlue = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_book_blue.png"),
                icoGearListBlue = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_gear_list_blue.png"),
                icoGovCyan = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_gov_cyan.png"),
                icoPumpCyan = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_pump_cyan.png"),
                icoPumpDark = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_pump_dark.png"),
                circleDot = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_circle_dot.png"),
                bottomOneBadge = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/ico_bottom_one.png"),
                valveOpen = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/spr_valve_open.png"),
                valveClosed = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/spr_valve_closed.png"),
                pumpOn = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/spr_pump_on.png"),
                pumpOff = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/spr_pump_off.png"),
                lockClosed = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/spr_lock_closed.png"),
                lockOpen = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/spr_lock_open.png"),
                upBadge = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/spr_up_badge.png"),
                downBadge = AssetDatabase.LoadAssetAtPath<Sprite>($"{s}/spr_down_badge.png"),
                diagNpmCutaway = AssetDatabase.LoadAssetAtPath<Sprite>($"{d}/diag_npm_cutaway.png"),
                diagFwmsMimic = AssetDatabase.LoadAssetAtPath<Sprite>($"{d}/diag_fwms_mimic.png"),
                diagCePid = AssetDatabase.LoadAssetAtPath<Sprite>($"{d}/diag_ce_pid.png"),
                diagCvcHeatingSpray = AssetDatabase.LoadAssetAtPath<Sprite>($"{d}/diag_cvc_heating_spray.png"),
                diagCvcDemins = AssetDatabase.LoadAssetAtPath<Sprite>($"{d}/diag_cvc_demins.png"),
                diagCvcRecirc = AssetDatabase.LoadAssetAtPath<Sprite>($"{d}/diag_cvc_recirc.png"),
                diagRccw = AssetDatabase.LoadAssetAtPath<Sprite>($"{d}/diag_rccw.png"),
            };
        }

        public static ScreenTransformInfo computeScreenPlacement(GameObject screenObj, Vector3 viewerRefPos)
        {
            MeshFilter mf = screenObj.GetComponentInChildren<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) throw new Exception($"MeshFilter 없음: {screenObj.name}");

            Mesh mesh = mf.sharedMesh;
            Transform t = mf.transform;
            Vector3[] verts = mesh.vertices;
            int[] tris = mesh.triangles;

            Vector3 sum = Vector3.zero;
            Vector3[] worldVerts = new Vector3[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                worldVerts[i] = t.TransformPoint(verts[i]);
                sum += worldVerts[i];
            }
            Vector3 centroid = sum / worldVerts.Length;

            bool isHeadFallback = !screenObj.name.EndsWith("_Screen", StringComparison.OrdinalIgnoreCase);
            Vector3 normal = Vector3.zero;
            if (!isHeadFallback && tris != null && tris.Length >= 3)
            {
                for (int i = 0; i + 2 < tris.Length; i += 3)
                {
                    Vector3 n = Vector3.Cross(worldVerts[tris[i + 1]] - worldVerts[tris[i]], worldVerts[tris[i + 2]] - worldVerts[tris[i]]);
                    if (n.sqrMagnitude > 1e-8f) { normal = n.normalized; break; }
                }
            }
            if (normal.sqrMagnitude < 0.001f) normal = t.forward.sqrMagnitude > 0.001f ? t.forward.normalized : Vector3.forward;
            if (Vector3.Dot(normal, viewerRefPos - centroid) < 0f) normal = -normal;

            Vector3 horiz = Vector3.Cross(Vector3.up, -normal).normalized;
            if (horiz.sqrMagnitude < 0.001f) horiz = Vector3.right;
            Vector3 vert = Vector3.Cross(-normal, horiz).normalized;

            float minH = float.MaxValue, maxH = float.MinValue, minV = float.MaxValue, maxV = float.MinValue, maxN = float.MinValue;
            for (int i = 0; i < worldVerts.Length; i++)
            {
                Vector3 d = worldVerts[i] - centroid;
                float h = Vector3.Dot(d, horiz);
                float v = Vector3.Dot(d, vert);
                float n = Vector3.Dot(d, normal);
                if (h < minH) minH = h; if (h > maxH) maxH = h;
                if (v < minV) minV = v; if (v > maxV) maxV = v;
                if (n > maxN) maxN = n;
            }

            Vector3 frontCenter = centroid + horiz * ((minH + maxH) * 0.5f) + vert * ((minV + maxV) * 0.5f) + normal * maxN;
            float width = maxH - minH;
            float height = maxV - minV;
            if (isHeadFallback)
            {
                // Monitor27_Head 3D 하우징 베젤을 제외한 27인치 16:9 실제 LCD 영역으로 정확히 내접 보정
                width = Mathf.Min(Mathf.Max(0.45f, width - 0.014f), 0.598f);
                height = width * (9f / 16f);
                if ((maxV - minV) > height + 0.008f)
                {
                    frontCenter += vert * 0.004f;
                }
            }
            else if (height > 0.01f && Mathf.Abs((width / height) - (16f / 9f)) > 0.05f)
            {
                height = width * (9f / 16f);
            }

            return new ScreenTransformInfo { centerWorld = frontCenter, outwardNormal = normal.normalized, widthMeters = width, heightMeters = height };
        }

        private static Canvas createMonitorCanvas(Transform parent, string name, GameObject screenMeshObj, Vector3 viewerRefPos)
        {
            ScreenTransformInfo info = computeScreenPlacement(screenMeshObj, viewerRefPos);
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster));
            go.transform.SetParent(parent, false);
            go.layer = 5;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(ControlRoomTheme.CanvasWidth, ControlRoomTheme.CanvasHeight);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.position = info.centerWorld + info.outwardNormal * 0.0045f;
            rt.rotation = Quaternion.LookRotation(-info.outwardNormal, Vector3.up);
            float sx = info.widthMeters / ControlRoomTheme.CanvasWidth;
            float sy = info.heightMeters / ControlRoomTheme.CanvasHeight;
            rt.localScale = new Vector3(sx, sy, sx);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.dynamicPixelsPerUnit = 1f;
            return canvas;
        }

        /// <summary>벽면 TV 전면 메쉬에 정밀 밀착되는 읽기 전용(Raycaster 없음) 16:9 World-Space UI Canvas를 생성합니다.</summary>
        private static Canvas createDisplayOnlyWallCanvas(Transform parent, string name, GameObject screenMeshObj, Vector3 viewerRefPos)
        {
            ScreenTransformInfo info = computeScreenPlacement(screenMeshObj, viewerRefPos);
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(parent, false);
            go.layer = 5;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(ControlRoomTheme.CanvasWidth, ControlRoomTheme.CanvasHeight);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.position = info.centerWorld + info.outwardNormal * 0.006f;
            rt.rotation = Quaternion.LookRotation(-info.outwardNormal, Vector3.up);
            float sx = info.widthMeters / ControlRoomTheme.CanvasWidth;
            float sy = info.heightMeters / ControlRoomTheme.CanvasHeight;
            rt.localScale = new Vector3(sx, sy, sx);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 12;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.dynamicPixelsPerUnit = 1f;
            return canvas;
        }

        private static void captureWallVerifyShot(string fileName, Vector3 camPos, Vector3 lookAt, float fov)
        {
            List<Camera> disabled = new List<Camera>();
            foreach (Camera c in UnityEngine.Object.FindObjectsOfType<Camera>(true))
            {
                if (c.enabled) { disabled.Add(c); c.enabled = false; }
            }

            GameObject camGo = new GameObject($"TMP_WallVerify_{fileName}");
            try
            {
                camGo.transform.position = camPos;
                camGo.transform.LookAt(lookAt, Vector3.up);
                Camera cam = camGo.AddComponent<Camera>();
                cam.fieldOfView = fov; cam.nearClipPlane = 0.03f; cam.farClipPlane = 60f; cam.allowHDR = true;

                var urpDataType = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
                if (urpDataType != null)
                {
                    Component urp = camGo.AddComponent(urpDataType);
                    urpDataType.GetProperty("renderPostProcessing")?.SetValue(urp, true, null);
                }

                foreach (Canvas cv in UnityEngine.Object.FindObjectsOfType<Canvas>(true)) cv.worldCamera = cam;
                Canvas.ForceUpdateCanvases();

                const int width = 1920; const int height = 1080;
                RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
                cam.targetTexture = rt;
                RenderTexture prev = RenderTexture.active;
                cam.Render();
                RenderTexture.active = rt;

                Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(WALL_VERIFY_RENDER_DIR, fileName), tex.EncodeToPNG());

                RenderTexture.active = prev;
                cam.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(tex);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(camGo);
                foreach (Camera c in disabled) if (c != null) c.enabled = true;
            }
        }

        private static void configureSpriteImporters()
        {
            string[] dirs = { $"{UI_ASSET_ROOT}/Sprites", $"{UI_ASSET_ROOT}/Sprites/Extracted", $"{UI_ASSET_ROOT}/Sprites/Diagrams" };
            foreach (string folder in dirs)
            {
                if (!Directory.Exists(Path.GetFullPath(folder))) continue;
                foreach (string file in Directory.GetFiles(Path.GetFullPath(folder), "*.png"))
                {
                    string rel = $"{folder}/{Path.GetFileName(file)}";
                    TextureImporter imp = AssetImporter.GetAtPath(rel) as TextureImporter;
                    if (imp == null)
                    {
                        AssetDatabase.ImportAsset(rel, ImportAssetOptions.ForceSynchronousImport);
                        imp = AssetImporter.GetAtPath(rel) as TextureImporter;
                    }
                    if (imp != null)
                    {
                        bool needsReimport = imp.textureType != TextureImporterType.Sprite
                                             || imp.spriteImportMode != SpriteImportMode.Single
                                             || imp.maxTextureSize < 2048
                                             || imp.textureCompression != TextureImporterCompression.Uncompressed;
                        if (needsReimport)
                        {
                            imp.textureType = TextureImporterType.Sprite;
                            imp.spriteImportMode = SpriteImportMode.Single;
                            imp.sRGBTexture = true;
                            imp.mipmapEnabled = false;
                            imp.filterMode = FilterMode.Bilinear;
                            imp.textureCompression = TextureImporterCompression.Uncompressed;
                            imp.maxTextureSize = 2048;
                            imp.SaveAndReimport();
                        }
                    }
                }
            }
        }

        private static TMP_FontAsset ensureKoreanFontAsset()
        {
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_ASSET_PATH);
            Font ttf = AssetDatabase.LoadAssetAtPath<Font>(FONT_TTF_PATH);
            if (ttf == null) throw new Exception($"한글 TTF 폰트를 찾을 수 없습니다: {FONT_TTF_PATH}");

            if (existing == null || existing.atlasWidth < 2048)
            {
                if (existing != null) AssetDatabase.DeleteAsset(FONT_ASSET_PATH);
                existing = TMP_FontAsset.CreateFontAsset(ttf, 44, 4, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
                existing.name = "MalgunGothic SDF";
                AssetDatabase.CreateAsset(existing, FONT_ASSET_PATH);
                if (existing.atlasTexture != null && !AssetDatabase.Contains(existing.atlasTexture))
                {
                    existing.atlasTexture.name = "MalgunGothic SDF Atlas";
                    AssetDatabase.AddObjectToAsset(existing.atlasTexture, existing);
                }
                if (existing.material != null && !AssetDatabase.Contains(existing.material))
                {
                    existing.material.name = "MalgunGothic SDF Material";
                    AssetDatabase.AddObjectToAsset(existing.material, existing);
                }
            }

            existing.TryAddCharacters(collectAllUiCharacters(), out _);
            TMP_FontAsset lib = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (lib != null)
            {
                if (existing.fallbackFontAssetTable == null) existing.fallbackFontAssetTable = new List<TMP_FontAsset>();
                if (!existing.fallbackFontAssetTable.Contains(lib)) existing.fallbackFontAssetTable.Add(lib);
                if (lib.fallbackFontAssetTable == null) lib.fallbackFontAssetTable = new List<TMP_FontAsset>();
                if (!lib.fallbackFontAssetTable.Contains(existing)) lib.fallbackFontAssetTable.Add(existing);
                EditorUtility.SetDirty(lib);
            }
            EditorUtility.SetDirty(existing);
            AssetDatabase.SaveAssets();
            return existing;
        }

        private static string collectAllUiCharacters()
        {
            var sb = new StringBuilder();
            for (char c = (char)32; c <= (char)126; c++) sb.Append(c);
            sb.Append("°▲▼▶▸►›●○■• ");
            string uiScriptsDir = Path.GetFullPath("Assets/Art/ControlRoom/Scripts/UI");
            if (Directory.Exists(uiScriptsDir))
            {
                foreach (string cs in Directory.GetFiles(uiScriptsDir, "*.cs")) sb.Append(File.ReadAllText(cs, Encoding.UTF8));
            }
            var unique = new HashSet<char>();
            var filtered = new StringBuilder();
            foreach (char ch in sb.ToString()) if (ch >= 32 && unique.Add(ch)) filtered.Append(ch);
            return filtered.ToString();
        }

        private static void ensureXrEventSystemInScene()
        {
            EventSystem es = UnityEngine.Object.FindObjectOfType<EventSystem>(true);
            if (es == null)
            {
                var esGo = new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule));
                var xrui = esGo.GetComponent<XRUIInputModule>();
                xrui.enableXRInput = true; xrui.enableMouseInput = true; xrui.enableTouchInput = true; xrui.enableBuiltinActionsAsFallback = true;
                InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(XRI_INPUT_ACTIONS_PATH);
                if (actions != null)
                {
                    xrui.pointAction = InputActionReference.Create(actions.FindAction("XRI UI/Point"));
                    xrui.leftClickAction = InputActionReference.Create(actions.FindAction("XRI UI/Click"));
                    xrui.scrollWheelAction = InputActionReference.Create(actions.FindAction("XRI UI/ScrollWheel"));
                    xrui.navigateAction = InputActionReference.Create(actions.FindAction("XRI UI/Navigate"));
                    xrui.submitAction = InputActionReference.Create(actions.FindAction("XRI UI/Submit"));
                    xrui.cancelAction = InputActionReference.Create(actions.FindAction("XRI UI/Cancel"));
                }
            }
        }

        private static void captureOffscreenView(string fileName, Vector3 camPos, Vector3 lookAt, float fov)
        {
            List<Camera> disabled = new List<Camera>();
            foreach (Camera c in UnityEngine.Object.FindObjectsOfType<Camera>(true))
            {
                if (c.enabled) { disabled.Add(c); c.enabled = false; }
            }

            GameObject camGo = new GameObject($"TMP_Verify_{fileName}");
            try
            {
                camGo.transform.position = camPos;
                camGo.transform.LookAt(lookAt, Vector3.up);
                Camera cam = camGo.AddComponent<Camera>();
                cam.fieldOfView = fov; cam.nearClipPlane = 0.03f; cam.farClipPlane = 60f; cam.allowHDR = true;

                var urpDataType = Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
                if (urpDataType != null)
                {
                    Component urp = camGo.AddComponent(urpDataType);
                    urpDataType.GetProperty("renderPostProcessing")?.SetValue(urp, true, null);
                }

                foreach (Canvas cv in UnityEngine.Object.FindObjectsOfType<Canvas>(true)) cv.worldCamera = cam;
                Canvas.ForceUpdateCanvases();

                const int width = 1920; const int height = 1080;
                RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
                cam.targetTexture = rt;
                RenderTexture prev = RenderTexture.active;
                cam.Render();
                RenderTexture.active = rt;

                Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                File.WriteAllBytes(Path.Combine(VERIFY_RENDER_DIR, fileName), tex.EncodeToPNG());

                RenderTexture.active = prev;
                cam.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(rt);
                UnityEngine.Object.DestroyImmediate(tex);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(camGo);
                foreach (Camera c in disabled) if (c != null) c.enabled = true;
            }
        }

        private static GameObject findMonitorTarget(string screenName, string headFallbackName)
        {
            return findSceneObject(screenName) ?? findSceneObject(headFallbackName);
        }

        private static GameObject findSceneObject(string exactName)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Transform t = findRecursive(root.transform, exactName);
                if (t != null) return t.gameObject;
            }
            return null;
        }

        private static Transform findRecursive(Transform t, string name)
        {
            if (t.name.Equals(name, StringComparison.Ordinal)) return t;
            for (int i = 0; i < t.childCount; i++) { Transform res = findRecursive(t.GetChild(i), name); if (res != null) return res; }
            return null;
        }

        private static void destroyIfExists(string exactName)
        {
            GameObject go = findSceneObject(exactName);
            while (go != null) { UnityEngine.Object.DestroyImmediate(go); go = findSceneObject(exactName); }
        }

        /// <summary>씬 내 모든 GameObject(비활성 포함)를 순회하며 Missing Script 컴포넌트를 안전하게 제거합니다.</summary>
        private static int removeMissingScriptsInScene(Scene scene)
        {
            int removedTotal = 0;
            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    removedTotal += removeMissingRecursive(root.transform);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ControlRoomMonitorUIBuilder] removeMissingScriptsInScene 오류: {ex}");
            }
            return removedTotal;
        }

        private static int removeMissingRecursive(Transform t)
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
            int removed = count > 0 ? GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject) : 0;
            for (int i = 0; i < t.childCount; i++)
            {
                removed += removeMissingRecursive(t.GetChild(i));
            }
            return removed;
        }

        private static Vector3 getObjectBoundsCenter(GameObject go)
        {
            Renderer r = go.GetComponentInChildren<Renderer>();
            return r != null ? r.bounds.center : go.transform.position;
        }

        /// <summary>3D 가이드 화살표의 메쉬와 머티리얼을 프로젝트 에셋으로 저장해 프리팹 직렬화 시 누락을 방지합니다.</summary>
        private static void persistGuideArrowAssets(ScenarioGuideArrow3D guideArrow)
        {
            try
            {
                if (guideArrow == null) return;

                string arrowDir = $"{UI_ASSET_ROOT}/GuideArrow";
                if (!Directory.Exists(Path.GetFullPath(arrowDir)))
                {
                    Directory.CreateDirectory(Path.GetFullPath(arrowDir));
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                }

                string meshPath = $"{arrowDir}/MESH_ScenarioGuideArrow3D.asset";
                string matPath = $"{arrowDir}/MAT_ScenarioGuideArrow3D.mat";

                Mesh meshAsset = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (meshAsset == null)
                {
                    meshAsset = ScenarioGuideArrow3D.buildFacetedClassicArrowMesh();
                    AssetDatabase.CreateAsset(meshAsset, meshPath);
                }

                Material matAsset = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (matAsset == null)
                {
                    matAsset = ScenarioGuideArrow3D.createArrowMaterial(new Color(1f, 0.8f, 0.2f, 1f), 1.35f);
                    if (matAsset != null) AssetDatabase.CreateAsset(matAsset, matPath);
                }

                AssetDatabase.SaveAssets();
                guideArrow.assignSerializedAssets(meshAsset, matAsset);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ControlRoomMonitorUIBuilder] persistGuideArrowAssets 오류: {ex}");
            }
        }
    }
}
