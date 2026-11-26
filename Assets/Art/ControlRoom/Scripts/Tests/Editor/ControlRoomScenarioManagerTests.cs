using System;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ControlRoom.UI.Tests
{
    /// <summary>
    /// 시나리오 1·2 단일 메서드 전환, 벽면 TV 자동 동기화, 단계 스킵/순서 제어,
    /// 점프/초기화/예외 안전성을 검증하는 중앙 시나리오 매니저 EditMode 단위 테스트입니다.
    /// </summary>
    public class ControlRoomScenarioManagerTests
    {
        private GameObject mRootGo;
        private ControlRoomScenarioManager mManager;
        private OperatorStationCoordinator mS1Coord;
        private Scenario2StationCoordinator mS2Coord;
        private WallDisplaySyncController mWallCtrl;
        private TMP_FontAsset mFont;
        private ControlRoomSpritePack mSprites;

        [SetUp]
        public void setUp()
        {
            try
            {
                mRootGo = new GameObject("TestScenarioManagerRoot");
                mFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
                ControlRoomUIFactory.DefaultFont = mFont;
                ControlRoomUIFactory.OnAnyClickSound = null;

                mSprites = new ControlRoomSpritePack
                {
                    alarmOff = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f),
                    alarmOn = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f),
                    cautionOff = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f),
                    cautionOn = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f),
                    noticeOff = Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f),
                    noticeOn = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f),
                    circleDot = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 2, 2), Vector2.one * 0.5f)
                };

                var s1Go = new GameObject("S1Station");
                s1Go.transform.SetParent(mRootGo.transform, false);
                mS1Coord = s1Go.AddComponent<OperatorStationCoordinator>();
                mS1Coord.configureReferences(
                    createCanvas(s1Go.transform, "S1_L"),
                    createCanvas(s1Go.transform, "S1_C"),
                    createCanvas(s1Go.transform, "S1_R"),
                    mFont, mSprites, null, null);
                mS1Coord.initializeStation();

                var s2Go = new GameObject("S2Station");
                s2Go.transform.SetParent(mRootGo.transform, false);
                mS2Coord = s2Go.AddComponent<Scenario2StationCoordinator>();
                mS2Coord.configureReferences(
                    createCanvas(s2Go.transform, "S2_L"),
                    createCanvas(s2Go.transform, "S2_C"),
                    createCanvas(s2Go.transform, "S2_R"),
                    mFont, mSprites, null, null);
                mS2Coord.initializeStation();

                var wallGo = new GameObject("WallOverlays");
                wallGo.transform.SetParent(mRootGo.transform, false);
                mWallCtrl = wallGo.AddComponent<WallDisplaySyncController>();
                mWallCtrl.configureReferences(
                    createCanvas(wallGo.transform, "W_CTR"),
                    createCanvas(wallGo.transform, "W_U6Up"),
                    createCanvas(wallGo.transform, "W_U6Low"),
                    mFont, mSprites);
                mWallCtrl.initializeWallDisplays();

                var mgrGo = new GameObject("ScenarioManager");
                mgrGo.transform.SetParent(mRootGo.transform, false);
                mManager = mgrGo.AddComponent<ControlRoomScenarioManager>();
                mManager.configureReferences(mS1Coord, mS2Coord, mWallCtrl);
                mManager.resetDefaultStepOrders();
            }
            catch (Exception ex)
            {
                Assert.Fail($"ControlRoomScenarioManagerTests SetUp 오류: {ex}");
            }
        }

        [TearDown]
        public void tearDown()
        {
            if (mRootGo != null)
            {
                UnityEngine.Object.DestroyImmediate(mRootGo);
                mRootGo = null;
            }
        }

        private Canvas createCanvas(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            return canvas;
        }

        [Test]
        public void testSingleMethodCallSwitchesBetweenScenario1And2AndSyncsWallTV()
        {
            try
            {
                int eventCount = 0;
                ControlRoomScenarioId lastEventId = ControlRoomScenarioId.Scenario1;
                mManager.OnScenarioChanged += id => { eventCount++; lastEventId = id; };

                // 시나리오 2로 단일 메서드 전환 -> S2 활성, S1 비활성, 벽면 TV LOCA(Alarm 1, 상승 화살표 ON) 즉시 연동
                Assert.IsTrue(mManager.switchScenario(2));
                Assert.AreEqual(ControlRoomScenarioId.Scenario2, mManager.CurrentScenario);
                Assert.AreEqual(ControlRoomScenarioId.Scenario2, lastEventId);
                Assert.IsFalse(mS1Coord.gameObject.activeSelf);
                Assert.IsTrue(mS2Coord.gameObject.activeSelf);
                Assert.AreEqual("1", mWallCtrl.Unit06AlarmCountText);
                Assert.IsTrue(mWallCtrl.AreUnit06UpArrowsVisible);

                // 시나리오 1로 단일 메서드 복귀 전환 -> S1 활성, S2 비활성, 벽면 TV 정상(Alarm 0, 상승 화살표 OFF) 즉시 연동
                Assert.IsTrue(mManager.switchScenario(1));
                Assert.AreEqual(ControlRoomScenarioId.Scenario1, mManager.CurrentScenario);
                Assert.AreEqual(ControlRoomScenarioId.Scenario1, lastEventId);
                Assert.AreEqual(2, eventCount);
                Assert.IsTrue(mS1Coord.gameObject.activeSelf);
                Assert.IsFalse(mS2Coord.gameObject.activeSelf);
                Assert.AreEqual("0", mWallCtrl.Unit06AlarmCountText);
                Assert.IsFalse(mWallCtrl.AreUnit06UpArrowsVisible);
            }
            catch (Exception ex)
            {
                Assert.Fail($"testSingleMethodCallSwitchesBetweenScenario1And2AndSyncsWallTV 실패: {ex}");
            }
        }

        [Test]
        public void testDisablingStepsInCentralOrderConfigSkipsSmoothly()
        {
            try
            {
                // 시나리오 1: Step01 비활성화 후 Play(▶) 클릭 시 Step02로 자동 건너뛰기 검증
                mManager.switchScenario(1);
                mManager.setScenario1StepEnabled(CaptureSequenceStep.Step01_InstructorRunning, false);
                mS1Coord.MainMonitor.InstPlayButtonWidget.simulateClick();
                Assert.AreEqual(CaptureSequenceStep.Step02_ProcessLibraryEmpty, mS1Coord.SimulationModel.CaptureStep);

                // 시나리오 2: S2_01, S2_02 비활성화 후 S2_00에서 CategoryBox 클릭 시 S2_03_CeInitialAlarm까지 자동 스킵 검증
                mManager.switchScenario(2);
                mManager.setScenario2StepEnabled(Scenario2SequenceStep.S2_01_StartCatMenuOpen, false);
                mManager.setScenario2StepEnabled(Scenario2SequenceStep.S2_02_StartPrimaryMenuOpen, false);
                mS2Coord.MainMonitor.Chrome.CategoryBoxWidget.simulateClick();
                Assert.AreEqual(Scenario2SequenceStep.S2_03_CeInitialAlarm, mS2Coord.Scenario2Model.CurrentStep);
                Assert.AreEqual(MonitorScreenId.PrimaryCE, mS2Coord.Scenario2Model.CurrentMainScreen);
            }
            catch (Exception ex)
            {
                Assert.Fail($"testDisablingStepsInCentralOrderConfigSkipsSmoothly 실패: {ex}");
            }
        }

        [Test]
        public void testJumpToStepAdvanceAndResetWorkFromCentralManager()
        {
            try
            {
                // 시나리오 1 출력 변화 시간 일괄 변경 및 단계 전진/초기화 검증
                mManager.switchScenario(1);
                mManager.setScenario1Durations(45f, 30f);
                Assert.AreEqual(45f, mS1Coord.PowerManeuverDurationSeconds, 0.01f);
                Assert.AreEqual(30f, mS1Coord.PowerAscensionDurationSeconds, 0.01f);

                Assert.IsTrue(mManager.advanceCurrentScenarioStep());
                Assert.AreEqual(CaptureSequenceStep.Step01_InstructorRunning, mS1Coord.SimulationModel.CaptureStep);

                mManager.jumpToScenario1Step(CaptureSequenceStep.Step18_Ramp80Completed);
                Assert.AreEqual(CaptureSequenceStep.Step18_Ramp80Completed, mS1Coord.SimulationModel.CaptureStep);

                mManager.resetCurrentScenario();
                Assert.AreEqual(CaptureSequenceStep.Step00_InstructorPaused, mS1Coord.SimulationModel.CaptureStep);

                // 시나리오 2 점프 시 2차 경보(Alarm 2) 및 벽면 TV 동기화 -> 초기화 시 Alarm 1 복원 검증
                mManager.jumpToScenario2Step(Scenario2SequenceStep.S2_19_Mov0318Closed);
                Assert.AreEqual(ControlRoomScenarioId.Scenario2, mManager.CurrentScenario);
                Assert.AreEqual(Scenario2SequenceStep.S2_19_Mov0318Closed, mS2Coord.Scenario2Model.CurrentStep);
                Assert.AreEqual("2", mWallCtrl.Unit06AlarmCountText);

                mManager.resetCurrentScenario();
                Assert.AreEqual(Scenario2SequenceStep.S2_00_ProcLibInitial, mS2Coord.Scenario2Model.CurrentStep);
                Assert.AreEqual("1", mWallCtrl.Unit06AlarmCountText);
            }
            catch (Exception ex)
            {
                Assert.Fail($"testJumpToStepAdvanceAndResetWorkFromCentralManager 실패: {ex}");
            }
        }

        [Test]
        public void testInvalidScenarioNumberHandledSafelyWithoutCrashing()
        {
            try
            {
                mManager.switchScenario(1);
                LogAssert.Expect(LogType.Warning, "[ControlRoomScenarioManager] 잘못된 시나리오 번호(99)입니다. 1 또는 2만 허용됩니다.");
                bool ok = mManager.switchScenario(99);
                Assert.IsFalse(ok);
                Assert.AreEqual(ControlRoomScenarioId.Scenario1, mManager.CurrentScenario);
            }
            catch (Exception ex)
            {
                Assert.Fail($"testInvalidScenarioNumberHandledSafelyWithoutCrashing 실패: {ex}");
            }
        }
    }
}
