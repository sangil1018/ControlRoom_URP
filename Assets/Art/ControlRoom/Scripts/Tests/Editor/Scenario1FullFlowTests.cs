using System;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ControlRoom.UI.Tests
{
    /// <summary>
    /// 시나리오 1 전체 흐름(100% → 80% 1분 감발 → B. Power Ascension → 100% 1분 상승 복귀)과
    /// 좌·중·우 3대 모니터 연동, 가이드 화살표 타겟, 경계/예외 처리를 검증하는 EditMode 단위 테스트입니다.
    /// </summary>
    public class Scenario1FullFlowTests
    {
        private GameObject mRootGo;
        private PlantSimulationModel mModel;
        private ScenarioGuideController mGuide;
        private LeftMonitorController mLeft;
        private MainMonitorController mMain;
        private RightMonitorController mRight;

        /// <summary>테스트마다 독립된 3모니터 UI 캔버스 및 시뮬레이션 모델을 생성합니다.</summary>
        [SetUp]
        public void setUp()
        {
            try
            {
                mRootGo = new GameObject("TestControlRoomRoot");
                var leftCanvas = createTestCanvas("LeftCanvas");
                var mainCanvas = createTestCanvas("MainCanvas");
                var rightCanvas = createTestCanvas("RightCanvas");

                TMP_FontAsset fallbackFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
                ControlRoomUIFactory.DefaultFont = fallbackFont;
                ControlRoomUIFactory.OnAnyClickSound = null;

                mModel = new PlantSimulationModel();
                mGuide = new ScenarioGuideController(mModel);
                mLeft = new LeftMonitorController(mModel);
                mMain = new MainMonitorController(mModel, mGuide);
                mRight = new RightMonitorController(mModel);

                var sprites = new ControlRoomSpritePack();
                mLeft.buildUI(leftCanvas.transform, sprites);
                mMain.buildUI(mainCanvas.transform, sprites);
                mRight.buildUI(rightCanvas.transform, sprites);

                mModel.OnStateChanged += () =>
                {
                    mLeft.refreshUI();
                    mMain.refreshUI();
                    mRight.refreshUI();
                };
            }
            catch (Exception ex)
            {
                Assert.Fail($"테스트 SetUp 중 예외 발생: {ex}");
            }
        }

        /// <summary>테스트 종료 후 임시 생성한 UI 오브젝트를 정리합니다.</summary>
        [TearDown]
        public void tearDown()
        {
            if (mRootGo != null)
            {
                UnityEngine.Object.DestroyImmediate(mRootGo);
                mRootGo = null;
            }
        }

        private Canvas createTestCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(mRootGo.transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            return canvas;
        }

        private void advanceModelBySeconds(float totalSeconds, float stepDt = 1f)
        {
            float elapsed = 0f;
            while (elapsed < totalSeconds)
            {
                float dt = Mathf.Min(stepDt, totalSeconds - elapsed);
                mModel.tickSimulation(dt);
                elapsed += dt;
            }
        }

        private void executePowerReductionUiClicksUpToRamp80()
        {
            mMain.InstPlayButtonWidget.simulateClick();
            mMain.Chrome.ProcessLibraryNavWidget.simulateClick();
            mMain.TreeIopWidget.simulateClick();
            mMain.TreePowerOpWidget.simulateClick();
            mMain.TreePowerRedWidget.simulateClick();
            mMain.StartProcWidget.simulateClick();
            mMain.Step1CheckWidget.simulateClick();
            mMain.Step3CheckWidget.simulateClick();
            mMain.Step3NaMenuWidget.simulateClick();
            mMain.Step6LinkWidget.simulateClick();
            mMain.skipSetupPleaseWaitForVerification();
            mMain.PmTargetFieldWidget.simulateClick();
            mMain.PmValidateWidget.simulateClick();
            mMain.PmExecuteWidget.simulateClick();
            mMain.ProcActiveCardWidget.simulateClick();
            mMain.Step6CheckWidget.simulateClick();
            mMain.Step8CheckWidget.simulateClick();
            mMain.PmActiveCardWidget.simulateClick();
        }

        [Test]
        public void testPowerReduction60SecondsUpdatesLeftMonitorAndReaches80Percent()
        {
            try
            {
                // 초기 100% 정상 상태 확인 (좌측 모니터)
                Assert.AreEqual("99.8 %", mLeft.NiPwrText);
                Assert.AreEqual("50.0 MW", mLeft.TurbinePowerText);
                Assert.AreEqual("111 °F", mLeft.Sg1Text);
                Assert.AreEqual("111 °F", mLeft.Sg2Text);
                Assert.AreEqual("1.02 psia", mLeft.CndPressText);
                Assert.AreEqual("0.01 lb/s", mLeft.MismatchText);
                Assert.AreEqual("151 lb/s", mLeft.FwTotalText);
                Assert.AreEqual("71.3 %", mLeft.FwPctAText);
                Assert.AreEqual("84 %", mLeft.GovText);
                Assert.AreEqual("480 psia", mLeft.MsPressText);

                executePowerReductionUiClicksUpToRamp80();
                Assert.AreEqual(CaptureSequenceStep.Step17_ReturnedToPmRamp80, mModel.CaptureStep);

                // 지정한 1분(60초) 출력 감소 진행
                advanceModelBySeconds(60f, 1f);

                Assert.IsTrue(mModel.HasReached80Percent);
                Assert.IsFalse(mModel.IsPowerManeuverRunning);
                Assert.AreEqual(CaptureSequenceStep.Step18_Ramp80Completed, mModel.CaptureStep);

                // 물리 모델 80% 정밀 수치 검증
                Assert.AreEqual(79.8f, mModel.CoreThermalPowerPct, 0.1f);
                Assert.AreEqual(60.30f, mModel.FwPumpASpeedPct, 0.1f);
                Assert.AreEqual(60.2f, mModel.FwPumpAFlowLbS, 0.1f);
                Assert.AreEqual(120.4f, mModel.FwFlowLbS, 0.2f);
                Assert.AreEqual(50.03f, mModel.GovernorPct, 0.1f);
                Assert.AreEqual(39.5f, mModel.TurbineElectricMw, 0.1f);
                Assert.AreEqual(78.7f, mModel.TurbinePowerPct, 0.1f);
                Assert.AreEqual(101f, mModel.Sg1SuperheatF, 0.2f);
                Assert.AreEqual(101f, mModel.Sg2SuperheatF, 0.2f);
                Assert.AreEqual(0.89f, mModel.CondenserPressurePsia, 0.01f);
                Assert.AreEqual(0.096f, mModel.FwMsMismatchLbS, 0.002f);
                Assert.AreEqual(520f, mModel.MsHeaderPressurePsia, 0.5f);
                Assert.AreEqual(178, mModel.ControlRodInnerSteps);

                // 좌측 모니터 12페이지 변경 항목 반영 확인
                Assert.AreEqual("79.8 %", mLeft.NiPwrText);
                Assert.AreEqual("39.5 MW", mLeft.TurbinePowerText);
                Assert.AreEqual("101 °F", mLeft.Sg1Text);
                Assert.AreEqual("101 °F", mLeft.Sg2Text);
                Assert.AreEqual("0.89 psia", mLeft.CndPressText);
                Assert.AreEqual("0.096 lb/s", mLeft.MismatchText);
                Assert.AreEqual("120 lb/s", mLeft.FwTotalText);
                Assert.AreEqual("60.3 %", mLeft.FwPctAText);
                Assert.AreEqual("50 %", mLeft.GovText);
                Assert.AreEqual("520 psia", mLeft.MsPressText);
            }
            catch (Exception ex)
            {
                Assert.Fail($"testPowerReduction60SecondsUpdatesLeftMonitorAndReaches80Percent 실패: {ex}");
            }
        }

        [Test]
        public void testFullScenario1PowerReductionAndAscension100PercentRestoresAllValues()
        {
            try
            {
                executePowerReductionUiClicksUpToRamp80();
                advanceModelBySeconds(60f, 1f);
                Assert.AreEqual(CaptureSequenceStep.Step18_Ramp80Completed, mModel.CaptureStep);

                // 13페이지: Procedures 탭 -> B. Power Ascension -> Start Procedure -> 1번 체크 -> 5번 링크
                Assert.AreSame(mMain.SubTabProcWidget.GetComponent<RectTransform>(), mMain.getCurrentScenarioGuideTargetRect());
                mMain.SubTabProcWidget.simulateClick();
                Assert.AreEqual(CaptureSequenceStep.Step19_ProceduresTabForAscension, mModel.CaptureStep);

                Assert.AreSame(mMain.TreePowerAscWidget.GetComponent<RectTransform>(), mMain.getCurrentScenarioGuideTargetRect());
                mMain.TreePowerAscWidget.simulateClick();
                Assert.AreEqual(CaptureSequenceStep.Step20_PowerAscensionPreview, mModel.CaptureStep);

                Assert.AreSame(mMain.AscStartProcWidget.GetComponent<RectTransform>(), mMain.getCurrentScenarioGuideTargetRect());
                mMain.AscStartProcWidget.simulateClick();
                Assert.AreEqual(CaptureSequenceStep.Step21_PowerAscensionActive, mModel.CaptureStep);

                mMain.AscStep1CheckWidget.simulateClick();
                Assert.IsTrue(mModel.isStepChecked("ASC_1"));

                Assert.AreSame(mMain.AscStep5LinkWidget.GetComponent<RectTransform>(), mMain.getCurrentScenarioGuideTargetRect());
                mMain.AscStep5LinkWidget.simulateClick();
                mMain.skipSetupPleaseWaitForVerification();
                Assert.AreEqual(CaptureSequenceStep.Step22_AscPmSetup, mModel.CaptureStep);

                // Target 100 / Rate 20 입력 -> Validate -> Execute
                Assert.AreSame(mMain.PmTargetFieldWidget.GetComponent<RectTransform>(), mMain.getCurrentScenarioGuideTargetRect());
                mMain.PmTargetFieldWidget.simulateClick();
                Assert.AreEqual(CaptureSequenceStep.Step23_AscPmParamsEntered, mModel.CaptureStep);
                Assert.AreEqual(100f, mModel.PendingTargetPowerPct, 0.01f);
                Assert.AreEqual(20f, mModel.PendingRampRatePctHr, 0.01f);

                Assert.AreSame(mMain.PmValidateWidget.GetComponent<RectTransform>(), mMain.getCurrentScenarioGuideTargetRect());
                mMain.PmValidateWidget.simulateClick();
                Assert.AreEqual(CaptureSequenceStep.Step24_AscPmValidated, mModel.CaptureStep);

                Assert.AreSame(mMain.PmExecuteWidget.GetComponent<RectTransform>(), mMain.getCurrentScenarioGuideTargetRect());
                mMain.PmExecuteWidget.simulateClick();
                Assert.AreEqual(CaptureSequenceStep.Step25_AscPmRamp100, mModel.CaptureStep);
                Assert.IsTrue(mModel.IsPowerManeuverRunning);

                // 100% 상승 중 가이드 상태 확인
                var midGuide = mGuide.evaluateGuideStatus();
                Assert.IsFalse(midGuide.isGoalCompleted);
                StringAssert.Contains("100%", midGuide.stepTitle);

                // 지정한 1분(60초) 출력 상승 진행
                advanceModelBySeconds(60f, 1f);

                Assert.IsFalse(mModel.IsPowerManeuverRunning);
                Assert.IsTrue(mModel.HasReached100PercentAfterReduction);
                Assert.AreEqual(CaptureSequenceStep.Step26_Scenario1Completed, mModel.CaptureStep);
                Assert.IsNull(mMain.getCurrentScenarioGuideTargetRect());

                // 100% 원복 물리 수치 검증
                Assert.AreEqual(99.8f, mModel.CoreThermalPowerPct, 0.1f);
                Assert.AreEqual(99.8f, mModel.CoreNiPowerPct, 0.1f);
                Assert.AreEqual(71.30f, mModel.FwPumpASpeedPct, 0.1f);
                Assert.AreEqual(75.6f, mModel.FwPumpAFlowLbS, 0.1f);
                Assert.AreEqual(151.2f, mModel.FwFlowLbS, 0.2f);
                Assert.AreEqual(83.85f, mModel.GovernorPct, 0.1f);
                Assert.AreEqual(50.0f, mModel.TurbineElectricMw, 0.1f);
                Assert.AreEqual(99.8f, mModel.TurbinePowerPct, 0.1f);
                Assert.AreEqual(111f, mModel.Sg1SuperheatF, 0.2f);
                Assert.AreEqual(111f, mModel.Sg2SuperheatF, 0.2f);
                Assert.AreEqual(1.02f, mModel.CondenserPressurePsia, 0.01f);
                Assert.AreEqual(0.01f, mModel.FwMsMismatchLbS, 0.002f);
                Assert.AreEqual(480f, mModel.MsHeaderPressurePsia, 0.5f);
                Assert.AreEqual(201, mModel.ControlRodInnerSteps);
                Assert.AreEqual(201, mModel.ControlRodOuterCrossSteps);

                // 좌측 모니터 100% 원복 검증
                Assert.AreEqual("99.8 %", mLeft.NiPwrText);
                Assert.AreEqual("50.0 MW", mLeft.TurbinePowerText);
                Assert.AreEqual("111 °F", mLeft.Sg1Text);
                Assert.AreEqual("111 °F", mLeft.Sg2Text);
                Assert.AreEqual("1.02 psia", mLeft.CndPressText);
                Assert.AreEqual("0.01 lb/s", mLeft.MismatchText);
                Assert.AreEqual("151 lb/s", mLeft.FwTotalText);
                Assert.AreEqual("71.3 %", mLeft.FwPctAText);
                Assert.AreEqual("84 %", mLeft.GovText);
                Assert.AreEqual("480 psia", mLeft.MsPressText);

                // 우측 12-Unit Overview 06호기 복귀 및 정상 경보 없음 검증
                Assert.AreEqual(99.8f, mModel.AllUnits[5].thermalPowerPct, 0.1f);
                Assert.IsFalse(mRight.hasAnyAlertIndicatorVisible());

                // 히스토리 버퍼가 80%에서 100%로 상승 곡선을 기록했는지 검증
                Assert.AreEqual(mModel.PowerHistory.Count, mModel.NiPowerHistory.Count);
                Assert.AreEqual(mModel.PowerHistory.Count, mModel.TurbineMwHistory.Count);
                Assert.AreEqual(mModel.PowerHistory.Count, mModel.MsPressHistory.Count);
                Assert.Less(mModel.PowerHistory[0], mModel.PowerHistory[mModel.PowerHistory.Count - 1]);
                Assert.Less(mModel.TurbineMwHistory[0], mModel.TurbineMwHistory[mModel.TurbineMwHistory.Count - 1]);

                var endGuide = mGuide.evaluateGuideStatus();
                Assert.IsTrue(endGuide.isGoalCompleted);
            }
            catch (Exception ex)
            {
                Assert.Fail($"testFullScenario1PowerReductionAndAscension100PercentRestoresAllValues 실패: {ex}");
            }
        }

        [Test]
        public void testEdgeCasesDurationClampingInvalidValidationAndImmediateComplete()
        {
            try
            {
                // 최소 1초 클램프 검증
                mModel.setPowerManeuverDurationSeconds(0f);
                mModel.setPowerAscensionDurationSeconds(-10f);
                Assert.AreEqual(1f, mModel.PowerManeuverDurationSeconds, 0.001f);
                Assert.AreEqual(1f, mModel.PowerAscensionDurationSeconds, 0.001f);

                // 잘못된 입력 범위 거부 검증
                Assert.IsFalse(mModel.validatePowerManeuver(-5f, 300f, false));
                Assert.IsFalse(mModel.validatePowerManeuver(80f, 0f, false));
                Assert.IsFalse(mModel.validatePowerManeuver(80f, 700f, false));
                Assert.IsFalse(mModel.executePowerManeuver());

                // 양방향 즉시 완료 검증
                executePowerReductionUiClicksUpToRamp80();
                mModel.completePowerManeuverImmediately();
                Assert.AreEqual(CaptureSequenceStep.Step18_Ramp80Completed, mModel.CaptureStep);
                Assert.AreEqual(79.8f, mModel.CoreThermalPowerPct, 0.1f);

                mMain.SubTabProcWidget.simulateClick();
                mMain.TreePowerAscWidget.simulateClick();
                mMain.AscStartProcWidget.simulateClick();
                mMain.AscStep5LinkWidget.simulateClick();
                mMain.PmTargetFieldWidget.simulateClick();
                mMain.PmValidateWidget.simulateClick();
                mMain.PmExecuteWidget.simulateClick();
                mModel.completePowerManeuverImmediately();
                Assert.AreEqual(CaptureSequenceStep.Step26_Scenario1Completed, mModel.CaptureStep);
                Assert.AreEqual(99.8f, mModel.CoreThermalPowerPct, 0.1f);
            }
            catch (Exception ex)
            {
                Assert.Fail($"testEdgeCasesDurationClampingInvalidValidationAndImmediateComplete 실패: {ex}");
            }
        }
    }
}
