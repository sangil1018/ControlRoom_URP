using System;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace ControlRoom.UI.Tests
{
    /// <summary>
    /// 시나리오 2(원자로 냉각재 누설 LOCA: Small Charging Line Break) 0.25초 단위 영상 분석 기반
    /// 56단계 전체 클릭 시퀀스, 3대 모니터(좌/중/우) 실시간 연동, 3D 가이드 화살표 타겟,
    /// 밸브 좌표·Boric Acid Only·SHOW ALL·Tag Labels·경보 카운트 전이를 검증하는 EditMode 단위 테스트입니다.
    /// </summary>
    public class Scenario2FullFlowTests
    {
        private GameObject mRootGo;
        private PlantSimulationModel mPlantModel;
        private Scenario2ControllerModel mModel;
        private Scenario2LeftMonitorController mLeft;
        private Scenario2MainMonitorController mMain;
        private RightMonitorController mRight;

        [SetUp]
        public void setUp()
        {
            try
            {
                mRootGo = new GameObject("TestScenario2Root");
                var leftCanvas = createTestCanvas("LeftCanvas");
                var mainCanvas = createTestCanvas("MainCanvas");
                var rightCanvas = createTestCanvas("RightCanvas");

                TMP_FontAsset fallbackFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
                ControlRoomUIFactory.DefaultFont = fallbackFont;
                ControlRoomUIFactory.OnAnyClickSound = null;

                mPlantModel = new PlantSimulationModel();
                mModel = new Scenario2ControllerModel(mPlantModel);
                mModel.initializeScenario2();

                mLeft = new Scenario2LeftMonitorController(mModel);
                mMain = new Scenario2MainMonitorController(mModel);
                mRight = new RightMonitorController(mPlantModel);
                mRight.setHeaderUnitCodeOverride("00");

                var sprites = new ControlRoomSpritePack();
                mLeft.buildUI(leftCanvas.transform, sprites);
                mMain.buildUI(mainCanvas.transform, sprites);
                mRight.buildUI(rightCanvas.transform, sprites);

                mModel.OnScenario2StateChanged += () =>
                {
                    mLeft.refreshUI();
                    mMain.refreshUI();
                    mRight.refreshUI();
                };
            }
            catch (Exception ex)
            {
                Assert.Fail($"Scenario2FullFlowTests SetUp 오류: {ex}");
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

        private Canvas createTestCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(mRootGo.transform, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            return canvas;
        }

        /// <summary>가이드 화살표 타겟이 기대 위젯과 일치하는지 확인한 뒤 클릭을 실행하는 검증 헬퍼입니다.</summary>
        private void clickAndVerifyGuide(E2InteractiveWidget widget, Scenario2SequenceStep expectedAfterStep)
        {
            Assert.IsNotNull(widget, $"단계 전이({expectedAfterStep}) 대상 위젯이 null입니다.");
            Assert.AreSame(widget.GetComponent<RectTransform>(), mMain.getCurrentScenarioGuideTargetRect(),
                $"단계 {mModel.CurrentStep} -> {expectedAfterStep} 가이드 타겟 불일치");
            widget.simulateClick();
            Assert.AreEqual(expectedAfterStep, mModel.CurrentStep,
                $"위젯 {widget.name} 클릭 후 단계 전이 실패");
        }

        [Test]
        public void testScenario2InitialStateAndThreeMonitorSync()
        {
            try
            {
                Assert.AreEqual(Scenario2SequenceStep.S2_00_ProcLibInitial, mModel.CurrentStep);
                Assert.AreEqual(MonitorScreenId.ProcessLibrary, mModel.CurrentMainScreen);

                // 3대 모니터 호기 번호 연동 (좌 06, 중 06, 우 00)
                Assert.AreEqual("06", mLeft.Chrome.CurrentUnitCode);
                Assert.AreEqual("06", mMain.Chrome.CurrentUnitCode);
                Assert.AreEqual("00", mRight.Chrome.CurrentUnitCode);

                // 3대 모니터 상단 경보 배지 동기화 (초기 Alarm 1, Caution 1, Notice 1)
                Assert.AreEqual(1, mLeft.HeaderAlarmCount);
                Assert.AreEqual(1, mMain.Chrome.CurrentAlarmCount);
                Assert.AreEqual(1, mRight.Chrome.CurrentAlarmCount);
                Assert.AreEqual(1, mLeft.HeaderCautionCount);
                Assert.AreEqual(1, mRight.Chrome.CurrentCautionCount);
                Assert.AreEqual(1, mLeft.HeaderNoticeCount);
                Assert.AreEqual(1, mRight.Chrome.CurrentNoticeCount);

                // 좌측 모니터 Instructor Station Primary Breaks 선택 확인
                Assert.AreEqual("CVC Charging line", mLeft.SelectedLocationText);
                Assert.AreEqual("Small", mLeft.SelectedSizeText);

                // 우측 12-Unit Overview 06호기 경보 배지 활성화 확인
                Assert.IsTrue(mRight.hasAnyAlertIndicatorVisible());
            }
            catch (Exception ex)
            {
                Assert.Fail($"testScenario2InitialStateAndThreeMonitorSync 실패: {ex}");
            }
        }

        [Test]
        public void testScenario2FullEndToEndClickFlowMatchingVideo()
        {
            try
            {
                // 0. Process Library (00:14) -> Category(Primary) -> (CE) Containment Evacuation 진입
                clickAndVerifyGuide(mMain.Chrome.CategoryBoxWidget, Scenario2SequenceStep.S2_01_StartCatMenuOpen);
                clickAndVerifyGuide(mMain.CatPrimaryWidget, Scenario2SequenceStep.S2_02_StartPrimaryMenuOpen);
                clickAndVerifyGuide(mMain.PrimCeWidget, Scenario2SequenceStep.S2_03_CeInitialAlarm);
                Assert.AreEqual(MonitorScreenId.PrimaryCE, mModel.CurrentMainScreen);

                // 1. CE 배출 유로 전환 (CE-AOV-0130 OPEN, CE-AOV-0128 CLOSE)
                clickAndVerifyGuide(mMain.CeAov0130Widget, Scenario2SequenceStep.S2_04_Ce0130PopupOpen);
                clickAndVerifyGuide(mMain.Ce0130OpenWidget, Scenario2SequenceStep.S2_05_Ce0130Opened);
                Assert.IsTrue(mPlantModel.getValveOpen("CE-AOV-0130"));

                clickAndVerifyGuide(mMain.CeAov0128Widget, Scenario2SequenceStep.S2_06_Ce0128PopupOpen);
                clickAndVerifyGuide(mMain.Ce0128CloseWidget, Scenario2SequenceStep.S2_07_CeFlowRerouted);
                Assert.IsFalse(mPlantModel.getValveOpen("CE-AOV-0128"));

                // 2. Module Overview -> 뒤로가기(<)로 CE 복귀 -> (CVC) > Recirc & Makeup -> CE 재확인 -> (CVC) > Heating & Spray
                clickAndVerifyGuide(mMain.Chrome.UnitOverviewNavWidget, Scenario2SequenceStep.S2_08_ModOverviewChecked);
                Assert.AreEqual(MonitorScreenId.ModuleOverview, mModel.CurrentMainScreen);

                clickAndVerifyGuide(mMain.Chrome.BackNavWidget, Scenario2SequenceStep.S2_09_CeRecheckBeforeCvc);
                Assert.AreEqual(MonitorScreenId.PrimaryCE, mModel.CurrentMainScreen);

                clickAndVerifyGuide(mMain.Chrome.ScreenBoxWidget, Scenario2SequenceStep.S2_10_CeToCvcMenuOpen);
                clickAndVerifyGuide(mMain.PrimCvcWidget, Scenario2SequenceStep.S2_11_CvcSubMenuForRecirc);
                clickAndVerifyGuide(mMain.SubRecircWidget, Scenario2SequenceStep.S2_12_CvcRecircChecked);
                Assert.AreEqual(MonitorScreenId.PrimaryCvcRecirMakeup, mModel.CurrentMainScreen);

                clickAndVerifyGuide(mMain.Chrome.ScreenBoxWidget, Scenario2SequenceStep.S2_13_CvcToCeMenuOpen);
                clickAndVerifyGuide(mMain.PrimCeWidget, Scenario2SequenceStep.S2_14_CeCheckedForTrip);
                Assert.AreEqual(MonitorScreenId.PrimaryCE, mModel.CurrentMainScreen);
                Assert.GreaterOrEqual(mModel.CnvPressurePsia, 1.10f);
                Assert.GreaterOrEqual(mModel.RcsLeakRateGpm, 7.10f);

                clickAndVerifyGuide(mMain.Chrome.ScreenBoxWidget, Scenario2SequenceStep.S2_15_CeToCvcForHeatingMenu);
                clickAndVerifyGuide(mMain.PrimCvcWidget, Scenario2SequenceStep.S2_16_CvcSubMenuForHeating);
                clickAndVerifyGuide(mMain.SubHeatingWidget, Scenario2SequenceStep.S2_17_HeatingSprayOpened);
                Assert.AreEqual(MonitorScreenId.PrimaryCvcHeatingSpray, mModel.CurrentMainScreen);

                // 3. Heating & Spray 밸브 위치 검증 (AOV-0339 좌측 하단) 및 차단
                float aov339TopY = -mMain.Aov0339Widget.GetComponent<RectTransform>().anchoredPosition.y;
                Assert.Greater(aov339TopY, 700f, "CVC-AOV-0339는 좌측 하단 복귀 배관(y > 700)에 위치해야 합니다.");

                clickAndVerifyGuide(mMain.Mov0318Widget, Scenario2SequenceStep.S2_18_Mov0318PopupOpen);
                clickAndVerifyGuide(mMain.Mov0318CloseWidget, Scenario2SequenceStep.S2_19_Mov0318Closed);
                Assert.IsFalse(mPlantModel.getValveOpen("CVC-MOV-0318"));
                Assert.AreEqual(2, mLeft.HeaderAlarmCount);
                Assert.AreEqual(2, mMain.Chrome.CurrentAlarmCount);
                Assert.AreEqual(2, mRight.Chrome.CurrentAlarmCount);
                Assert.AreEqual(2, mRight.Chrome.CurrentCautionCount);

                clickAndVerifyGuide(mMain.Aov0339Widget, Scenario2SequenceStep.S2_20_Aov0339PopupOpen);
                clickAndVerifyGuide(mMain.Aov0339CloseWidget, Scenario2SequenceStep.S2_21_CvcsValvesClosed);
                Assert.IsFalse(mPlantModel.getValveOpen("CVC-AOV-0339"));
                Assert.AreEqual(-0.68f, mModel.CvcSprayFlowGpm, 0.01f);
                Assert.AreEqual(0.64f, mModel.CvcInjectionFlowGpm, 0.01f);
                Assert.AreEqual(0.61f, mModel.CvcDischargeFlowGpm, 0.01f);

                // 4. Demins & Letdowns -> Process Library > Automations > Search("cvc") > CVC Makeup (Boric Acid Only -> Validate -> Execute)
                clickAndVerifyGuide(mMain.Chrome.ScreenBoxWidget, Scenario2SequenceStep.S2_22_CvcSubMenuForDemins);
                clickAndVerifyGuide(mMain.SubDeminsWidget, Scenario2SequenceStep.S2_23_DeminsChecked);
                Assert.AreEqual(MonitorScreenId.PrimaryCvcDeminsLetdowns, mModel.CurrentMainScreen);

                clickAndVerifyGuide(mMain.Chrome.ProcessLibraryNavWidget, Scenario2SequenceStep.S2_24_ProcLibOpened);
                clickAndVerifyGuide(mMain.ProcLibAutoTabWidget, Scenario2SequenceStep.S2_25_AutomationsTabOpened);
                clickAndVerifyGuide(mMain.ProcLibSearchWidget, Scenario2SequenceStep.S2_26_CvcAutoSearched);
                Assert.IsTrue(mModel.IsCvcSearchApplied);

                clickAndVerifyGuide(mMain.TreeCvcMakeupWidget, Scenario2SequenceStep.S2_27_CvcMakeupSetup);
                Assert.IsFalse(mModel.IsCvcMakeupBlendSelected, "영상 실측: Boric Acid Only 기본 선택이 유지되어야 합니다.");
                clickAndVerifyGuide(mMain.MakeupValidateWidget, Scenario2SequenceStep.S2_28_CvcMakeupValidated);
                Assert.IsTrue(mModel.IsCvcMakeupValidated);
                clickAndVerifyGuide(mMain.MakeupExecuteWidget, Scenario2SequenceStep.S2_29_CvcMakeupRunning);
                Assert.IsTrue(mModel.IsCvcMakeupRunning);
                Assert.IsTrue(mModel.IsMakeupPumpBRunning);

                mModel.tick(6.0f);
                Assert.AreEqual(50.0f, mModel.CvcMakeupProgressPct, 0.1f);

                // 5. 상단 경보 아이콘 재클릭으로 열기/닫기 (Alarm 2건 유지 확인)
                clickAndVerifyGuide(mMain.Chrome.AlarmBadgeWidget, Scenario2SequenceStep.S2_30_AlarmDropdownOpen);
                Assert.AreEqual(2, mMain.Chrome.CurrentAlarmCount);
                clickAndVerifyGuide(mMain.Chrome.AlarmBadgeWidget, Scenario2SequenceStep.S2_31_AlarmChecked);
                Assert.AreEqual(2, mMain.Chrome.CurrentAlarmCount);

                // 6. Module Overview 경유 -> Special > Primary > (CVC) > Recirc & Makeup -> Lock -> Manual -> Pump A Stop (Alarm 2->1)
                clickAndVerifyGuide(mMain.Chrome.UnitOverviewNavWidget, Scenario2SequenceStep.S2_32_ModOverviewBeforeRecirc);
                clickAndVerifyGuide(mMain.Chrome.CategoryBoxWidget, Scenario2SequenceStep.S2_33_CatMenuForRecircStop);
                clickAndVerifyGuide(mMain.CatPrimaryWidget, Scenario2SequenceStep.S2_34_PrimaryMenuForRecircStop);
                clickAndVerifyGuide(mMain.PrimCvcWidget, Scenario2SequenceStep.S2_35_CvcSubMenuForRecircStop);
                clickAndVerifyGuide(mMain.SubRecircWidget, Scenario2SequenceStep.S2_36_RecircForPumpStop);
                Assert.AreEqual(MonitorScreenId.PrimaryCvcRecirMakeup, mModel.CurrentMainScreen);

                clickAndVerifyGuide(mMain.RecircLockWidget, Scenario2SequenceStep.S2_37_RecircLockSelected);
                clickAndVerifyGuide(mMain.RecircSwitchModesWidget, Scenario2SequenceStep.S2_38_RecircManualMode);
                Assert.IsTrue(mModel.IsRecircManualMode);
                clickAndVerifyGuide(mMain.RecircPumpAWidget, Scenario2SequenceStep.S2_39_PumpAPopupOpen);
                clickAndVerifyGuide(mMain.PumpAStopWidget, Scenario2SequenceStep.S2_40_RecircPumpsStopped);
                Assert.IsFalse(mModel.IsRecircPumpARunning);
                Assert.IsFalse(mModel.IsRecircPumpBRunning);
                Assert.AreEqual(1, mLeft.HeaderAlarmCount, "Pump A 정지 후 Alarm 2 -> 1 감소");
                Assert.AreEqual(1, mMain.Chrome.CurrentAlarmCount);
                Assert.AreEqual(1, mRight.Chrome.CurrentAlarmCount);

                // 7. Module Overview > Isolation -> SHOW ALL -> CVC S(ISV 0323) 확인 -> Primary > CE (Sample Vessel 0.11 ft)
                clickAndVerifyGuide(mMain.Chrome.UnitOverviewNavWidget, Scenario2SequenceStep.S2_41_ModOverviewForIso);
                clickAndVerifyGuide(mMain.ModIsoTabWidget, Scenario2SequenceStep.S2_42_IsoTabOpened);
                clickAndVerifyGuide(mMain.IsoShowAllWidget, Scenario2SequenceStep.S2_43_IsoShowAllActive);
                Assert.IsTrue(mModel.IsIsoShowAllActive);
                clickAndVerifyGuide(mMain.IsoCvcCheckWidget, Scenario2SequenceStep.S2_44_IsoValvesChecked);
                Assert.IsFalse(mModel.IsIsoShowAllActive);

                clickAndVerifyGuide(mMain.Chrome.CategoryBoxWidget, Scenario2SequenceStep.S2_45_CatMenuForCeRecheck);
                clickAndVerifyGuide(mMain.CatPrimaryWidget, Scenario2SequenceStep.S2_46_PrimaryMenuForCeRecheck);
                clickAndVerifyGuide(mMain.PrimCeWidget, Scenario2SequenceStep.S2_47_CeLeakRechecked);
                Assert.AreEqual(MonitorScreenId.PrimaryCE, mModel.CurrentMainScreen);
                Assert.GreaterOrEqual(mModel.CeSampleVesselLevelFt, 0.11f);

                clickAndVerifyGuide(mMain.CeSampleLevelWidget, Scenario2SequenceStep.S2_48_CeSampleChecked);

                // 8. Module Overview -> 0A -> Primary -> (RCCW) 3단계 전환 및 0A 탱크 확인 완료
                clickAndVerifyGuide(mMain.Chrome.UnitOverviewNavWidget, Scenario2SequenceStep.S2_49_ModOverviewRechecked);
                clickAndVerifyGuide(mMain.Chrome.UnitBoxWidget, Scenario2SequenceStep.S2_50_UnitDropdownFor0A);
                clickAndVerifyGuide(mMain.Unit0AWidget, Scenario2SequenceStep.S2_51_Unit0ASelected);
                Assert.AreEqual("0A", mModel.CenterHeaderUnitCode);
                clickAndVerifyGuide(mMain.Chrome.CategoryBoxWidget, Scenario2SequenceStep.S2_52_Unit0ACatMenuOpen);
                clickAndVerifyGuide(mMain.CatPrimaryWidget, Scenario2SequenceStep.S2_53_Unit0APrimaryMenuOpen);
                clickAndVerifyGuide(mMain.Prim0ARccwWidget, Scenario2SequenceStep.S2_54_RccwOpened);
                Assert.AreEqual(MonitorScreenId.PrimaryRccw, mModel.CurrentMainScreen);

                // 0A 화면에서는 Unit06 : CVC Makeup 카드가 Processes 패널에서 숨겨짐 확인
                var cardTransform = mMain.Chrome.ContentArea.Find("SharedRightPane/Card_CvcMakeup");
                Assert.IsNotNull(cardTransform);
                Assert.IsFalse(cardTransform.gameObject.activeSelf, "0A RCCW 화면에서는 Unit06 Makeup 카드가 숨겨져야 합니다.");

                clickAndVerifyGuide(mMain.RccwExpTankWidget, Scenario2SequenceStep.S2_55_Scenario2Completed);
                Assert.IsNull(mMain.getCurrentScenarioGuideTargetRect());
            }
            catch (Exception ex)
            {
                Assert.Fail($"testScenario2FullEndToEndClickFlowMatchingVideo 실패: {ex}");
            }
        }

        [Test]
        public void testScenario2OutOfOrderClicksRejectedAndResetRestoresInitialState()
        {
            try
            {
                // S2_00 상태에서 다른 단계 위젯 클릭 시 무시됨
                mMain.CeAov0130Widget.simulateClick();
                mMain.CeAov0128Widget.simulateClick();
                mMain.Mov0318Widget.simulateClick();
                mMain.Chrome.AlarmBadgeWidget.simulateClick();
                mMain.RccwExpTankWidget.simulateClick();
                Assert.AreEqual(Scenario2SequenceStep.S2_00_ProcLibInitial, mModel.CurrentStep);

                // Tag Labels 토글 자유 동작 확인
                Assert.IsFalse(mModel.IsTagLabelsVisible);
                mMain.Chrome.TagToggleNavWidget.simulateClick();
                Assert.IsTrue(mModel.IsTagLabelsVisible);

                // 초반 진입 후 초기화 시 초기 상태로 깨끗이 복원
                mMain.Chrome.CategoryBoxWidget.simulateClick();
                mMain.CatPrimaryWidget.simulateClick();
                mMain.PrimCeWidget.simulateClick();
                Assert.AreEqual(Scenario2SequenceStep.S2_03_CeInitialAlarm, mModel.CurrentStep);

                mModel.initializeScenario2();
                Assert.AreEqual(Scenario2SequenceStep.S2_00_ProcLibInitial, mModel.CurrentStep);
                Assert.AreEqual(MonitorScreenId.ProcessLibrary, mModel.CurrentMainScreen);
                Assert.IsFalse(mModel.IsTagLabelsVisible);
                Assert.IsFalse(mPlantModel.getValveOpen("CE-AOV-0130"));
                Assert.IsTrue(mPlantModel.getValveOpen("CE-AOV-0128"));
                Assert.AreEqual(1, mLeft.HeaderAlarmCount);
            }
            catch (Exception ex)
            {
                Assert.Fail($"testScenario2OutOfOrderClicksRejectedAndResetRestoresInitialState 실패: {ex}");
            }
        }
    }
}
