using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ControlRoom.UI
{
    /// <summary>
    /// 중앙 메인 모니터 컨트롤러입니다.
    /// Capture[00..49] 및 PowerOperation_C_Power_Reduction.mp4(27분 이후)의 모든 화면, 색상, 호버(어두워짐),
    /// 클릭(#00A5FF 점등 + 의도된 단계만 동작)을 1:1로 구현합니다.
    /// </summary>
    public class MainMonitorController
    {
        private readonly PlantSimulationModel mModel;
        private readonly ScenarioGuideController mGuide;
        private readonly MonitorCommonChrome mChrome = new MonitorCommonChrome();
        private ControlRoomSpritePack mSprites;

        public MonitorScreenId CurrentScreenId { get; private set; } = MonitorScreenId.InstructorStation;

        // 화면 컨테이너
        private RectTransform mInstructorRoot;
        private RectTransform mProcessLibraryRoot;

        // Instructor Station 위젯
        private TextMeshProUGUI mInstStatusTxt, mInstRunTimeTxt;
        public E2InteractiveWidget InstPlayButtonWidget { get; private set; }

        // Process Library 상단 서브탭 및 본문 영역
        private TextMeshProUGUI mSubTabProcTxt, mSubTabAutoTxt;
        private RectTransform mProcTreeContainer, mAutoTreeContainer;
        private RectTransform mCenterEmptyPane, mProcDocRoot, mAscDocRoot, mPmSetupRoot, mPmRunRoot;

        // Process Library 트리 주요 인터랙티브 위젯 (테스트 및 클릭 제어용)
        public E2InteractiveWidget SubTabProcWidget { get; private set; }
        public E2InteractiveWidget TreeIopWidget { get; private set; }
        public E2InteractiveWidget TreePowerOpWidget { get; private set; }
        public E2InteractiveWidget TreePowerAscWidget { get; private set; }
        public E2InteractiveWidget TreePowerRedWidget { get; private set; }
        private RectTransform mTreeSubIop, mTreeSubPowerOp;
        private ScrollRect mProcTreeScroll;

        // Procedure Document(C. Power Reduction) 위젯
        private ScrollRect mProcDocScroll;
        private TextMeshProUGUI mProcDocHeaderTxt;
        private RectTransform mPreviewFooter, mActiveFooter;
        public E2InteractiveWidget StartProcWidget { get; private set; }
        public E2InteractiveWidget Step1CheckWidget { get; private set; }
        public E2InteractiveWidget Step3CheckWidget { get; private set; }
        public E2InteractiveWidget Step3NaMenuWidget { get; private set; }
        public E2InteractiveWidget Step6LinkWidget { get; private set; }
        public E2InteractiveWidget Step6CheckWidget { get; private set; }
        public E2InteractiveWidget Step8CheckWidget { get; private set; }

        private RectTransform mStep1MinusBox, mStep1Bubble;
        private Image mStep1CheckImg, mStep1BorderImg;
        private RectTransform mStep1Badge1;
        private RectTransform mStep3FullRow, mStep3NaRow, mStep3ContextMenu;
        private RectTransform mStep6MinusBox, mStep6Bubble, mStep6Badge1;
        private Image mStep6CheckImg, mStep6BorderImg;
        private RectTransform mStep8MinusBox, mStep8Bubble, mStep8Badge1;
        private Image mStep8CheckImg, mStep8BorderImg;
        private readonly List<RectTransform> mActiveOnlyDecorations = new List<RectTransform>();

        public RectTransform Step3NaBadgeRect { get; private set; }
        public Image Step1CheckMarkImage => mStep1CheckImg;
        public Image Step1CheckBorderImage => mStep1BorderImg;

        // Procedure Document(B. Power Ascension) 위젯 (80% -> 100% 상승 시나리오)
        private RectTransform mAscPreviewFooter, mAscActiveFooter;
        private RectTransform mAscStep1Badge1;
        private Image mAscStep1CheckImg, mAscStep1BorderImg;
        private readonly List<RectTransform> mAscActiveOnlyDecorations = new List<RectTransform>();
        public E2InteractiveWidget AscStartProcWidget { get; private set; }
        public E2InteractiveWidget AscStep1CheckWidget { get; private set; }
        public E2InteractiveWidget AscStep5LinkWidget { get; private set; }

        // Power Maneuvering Setup 위젯 (Capture[28..34])
        private readonly List<(Image bg, TextMeshProUGUI lbl, bool isInner)> mPmSetupCrossRods = new List<(Image, TextMeshProUGUI, bool)>();
        private TrendChartWidget mPmSetupChart;
        private RectTransform mPmChartPleaseWait;
        private TextMeshProUGUI mPmWaitTimerTxt;
        private TextMeshProUGUI mPmHdrTHot, mPmHdrTCold, mPmHdrTAve, mPmHdrPzrP, mPmHdrCorePwr, mPmHdrNiPwr, mPmHdrMsFlow, mPmHdrFwFlow, mPmHdrMsP;
        private TextMeshProUGUI mPmLegTHotV, mPmLegTColdV, mPmLegTAveV;
        private TextMeshProUGUI mPmTargetTxt, mPmRateTxt, mPmSummaryTxt, mPmReadyTxt;
        private TMP_InputField mPmTargetInput, mPmRateInput;
        public E2InteractiveWidget PmTargetFieldWidget { get; private set; }
        public E2InteractiveWidget PmRateFieldWidget { get; private set; }
        public E2InteractiveWidget PmValidateWidget { get; private set; }
        public E2InteractiveWidget PmExecuteWidget { get; private set; }

        // Power Maneuvering Running 위젯 (Capture[35..49])
        private TextMeshProUGUI mRunReqPwr, mRunCurPwr, mRunCoreP, mRunTAvg, mRunFwFlow, mRunMsFlow, mRunTurbPwr;
        private TextMeshProUGUI mRunGovPct, mRunPumpAPct, mRunPumpBPct, mRunPumpCPct;
        private RectTransform mRunGovFill, mRunPumpAFill, mRunPumpBFill, mRunPumpCFill;
        private readonly List<(Image bg, TextMeshProUGUI lbl, bool isCrossRegRod, bool isInnerCross)> mRunCoreCells = new List<(Image, TextMeshProUGUI, bool, bool)>();
        private TrendChartWidget mRunChart;
        private RectTransform mRunChartAxesGroup;
        private readonly List<TextMeshProUGUI> mRunChartTimeLabels = new List<TextMeshProUGUI>();
        private readonly List<GameObject> mRunLegSwatches = new List<GameObject>();
        private readonly List<float> mRunTAveScaled = new List<float>(24);
        private readonly List<float> mRunPzrScaled = new List<float>(24);
        private TextMeshProUGUI mRunLegPwrV, mRunLegTAvgV, mRunLegPzrPV;

        // 우측 Active / Completed 패널 카드 (Capture[16..49])
        private RectTransform mProcActiveCard, mAscProcActiveCard, mPmActiveCard;
        private RectTransform mProcCardProgFill, mAscProcCardProgFill, mPmCardProgFill;
        private TextMeshProUGUI mProcCardProgTxt, mAscProcCardProgTxt, mPmCardStatusTxt, mPmCardProgTxt;
        public E2InteractiveWidget ProcActiveCardWidget { get; private set; }
        public E2InteractiveWidget AscProcActiveCardWidget { get; private set; }
        public E2InteractiveWidget PmActiveCardWidget { get; private set; }

        // 내부 상태
        private float mPleaseWaitTimer;
        private bool mInAutomationMode;

        public MainMonitorController(PlantSimulationModel model, ScenarioGuideController guide)
        {
            mModel = model ?? throw new ArgumentNullException(nameof(model));
            mGuide = guide ?? throw new ArgumentNullException(nameof(guide));
        }

        public MonitorCommonChrome Chrome => mChrome;

        public void buildUI(Transform canvasRoot, ControlRoomSpritePack sprites, Transform unusedHelperRoot = null)
        {
            try
            {
                mSprites = sprites ?? new ControlRoomSpritePack();
                mChrome.buildChrome(
                    canvasRoot,
                    true,
                    mSprites,
                    () => mModel.CaptureStep == CaptureSequenceStep.Step01_InstructorRunning,
                    onClickBottomProcessLibrary,
                    "net.tcp://SIMLABHS01:9080/service/tcp");

                var host = mChrome.ContentArea;
                buildInstructorStationScreen(host);
                buildProcessLibraryScreen(host);

                CurrentScreenId = MonitorScreenId.InstructorStation;
                refreshUI();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MainMonitorController] buildUI 오류: {ex}");
            }
        }

        public void tickVisualTimers(float deltaTime)
        {
            if (mPleaseWaitTimer > 0f)
            {
                mPleaseWaitTimer -= deltaTime;
                if (mPleaseWaitTimer <= 0f && mPmChartPleaseWait != null)
                {
                    mPmChartPleaseWait.gameObject.SetActive(false);
                }
            }
        }

        public void tickChrome(int heartbeatIndex)
        {
            mChrome.updateHeartbeat(heartbeatIndex);
            mChrome.refreshDeviceClock();
        }

        public void skipSetupPleaseWaitForVerification()
        {
            mPleaseWaitTimer = 0f;
            if (mPmChartPleaseWait != null) mPmChartPleaseWait.gameObject.SetActive(false);
        }

        /// <summary>화면 ID로 직접 전환 (기존 테스트/내부 탐색 호환)</summary>
        public void navigateToScreen(MonitorScreenId screenId, bool recordHistory = true)
        {
            CurrentScreenId = screenId;
            if (screenId == MonitorScreenId.ProcessLibrary)
            {
                mInstructorRoot.gameObject.SetActive(false);
                mProcessLibraryRoot.gameObject.SetActive(true);
            }
            else
            {
                mInstructorRoot.gameObject.SetActive(true);
                mProcessLibraryRoot.gameObject.SetActive(false);
            }
            mGuide.notifyScreenVisited(screenId);
            refreshUI();
        }

        // =====================================================================
        // Capture[00..03]: Instructor Station 화면
        // =====================================================================
        private void buildInstructorStationScreen(RectTransform host)
        {
            mInstructorRoot = ControlRoomUIFactory.createPanel(host, "InstructorStationScreen", ControlRoomTheme.BgWorkspaceEmpty);

            // 좌측 사이드바 (x=0..342, #2F2F2F)
            var left = ControlRoomUIFactory.createPanel(mInstructorRoot, "InstLeft", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(left, 0f, 0f, 342f, ControlRoomTheme.ContentAreaHeight);
            var divL = ControlRoomUIFactory.createPanel(mInstructorRoot, "LineL", ControlRoomTheme.BorderSubtle);
            ControlRoomUIFactory.setTopLeftRect(divL, 342f, 0f, 1.5f, ControlRoomTheme.ContentAreaHeight);

            var searchBox = ControlRoomUIFactory.createBorderedBox(left, "SearchBox", ControlRoomTheme.BgInput, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(searchBox, 10f, 12f, 294f, 34f);
            ControlRoomUIFactory.createLabel(searchBox.Find("Inner"), "PH", "  Search Transients...", 16f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.MidlineLeft, FontStyles.Italic);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(left, "Q", "Q", 16f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.Center).rectTransform, 308f, 12f, 26f, 34f);
            ControlRoomUIFactory.createHoverOnlyZone(left, "HitSearch", 10f, 12f, 322f, 34f);

            var tabM = ControlRoomUIFactory.createLabel(left, "TabM", "Malfunctions", 26f, ControlRoomTheme.TabActiveGreen, TextAlignmentOptions.Center);
            ControlRoomUIFactory.setTopLeftRect(tabM.rectTransform, 10f, 52f, 162f, 44f);
            var tabS = ControlRoomUIFactory.createLabel(left, "TabS", "Systems", 26f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center);
            ControlRoomUIFactory.setTopLeftRect(tabS.rectTransform, 174f, 52f, 158f, 44f);
            ControlRoomUIFactory.createHoverOnlyZone(left, "HitTabM", 10f, 52f, 162f, 44f);
            ControlRoomUIFactory.createHoverOnlyZone(left, "HitTabS", 174f, 52f, 158f, 44f);

            string[] malItems = { "Display", "Primary Breaks", "Secondary Breaks" };
            for (int i = 0; i < malItems.Length; i++)
            {
                float ry = 104f + i * 54f;
                var row = ControlRoomUIFactory.createBorderedBox(left, $"Mal_{i}", ControlRoomTheme.BgDarkPane, ControlRoomTheme.BorderSubtle, 1f);
                ControlRoomUIFactory.setTopLeftRect(row, 10f, ry, 322f, 54f);
                ControlRoomUIFactory.createLabel(row.Find("Inner"), "L", "   " + malItems[i], 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
                ControlRoomUIFactory.createHoverOnlyZone(row.Find("Inner"), "Hit", 0f, 0f, 320f, 52f);
            }

            // 좌하단 E2 로고 (Capture[00] 동일)
            var logo = ControlRoomUIFactory.createPanel(left, "E2Logo", Color.white, mSprites.e2LogoInstructor);
            ControlRoomUIFactory.setTopLeftRect(logo, 16f, 706f, 306f, 178f);

            // 우측 Sim Controls 패널 (x=1592..1920, #2F2F2F)
            var right = ControlRoomUIFactory.createPanel(mInstructorRoot, "InstRight", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(right, 1592f, 0f, 328f, ControlRoomTheme.ContentAreaHeight);
            var divR = ControlRoomUIFactory.createPanel(mInstructorRoot, "LineR", ControlRoomTheme.BorderSubtle);
            ControlRoomUIFactory.setTopLeftRect(divR, 1590.5f, 0f, 1.5f, ControlRoomTheme.ContentAreaHeight);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(right, "Hdr", "Sim Controls", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform, 14f, 10f, 200f, 28f);
            addHairline(right, 44f);

            mInstStatusTxt = ControlRoomUIFactory.createLabel(right, "Stat", "Simulator Status:  Paused", 16.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            ControlRoomUIFactory.setTopLeftRect(mInstStatusTxt.rectTransform, 14f, 64f, 210f, 34f);

            // 의도된 Play 버튼 [▶] (Capture[00 -> 01]) 및 호버 전용 Pause [❚❚]
            InstPlayButtonWidget = ControlRoomUIFactory.createE2Control(
                right, "BtnPlaySim",
                226f, 58f, 42f, 42f,
                ControlRoomTheme.BgButton, ControlRoomTheme.BgButton,
                null,
                () => mModel.CaptureStep == CaptureSequenceStep.Step00_InstructorPaused,
                onClickPlaySimulator,
                "▶", 18f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);

            ControlRoomUIFactory.createE2Control(right, "BtnPauseSim", 274f, 58f, 42f, 42f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "||", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);
            addHairline(right, 114f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(right, "CliK", "Clients Connected:", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform, 14f, 130f, 160f, 28f);
            var cli17 = ControlRoomUIFactory.createBorderedBox(right, "CliBox", ControlRoomTheme.BgInput, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(cli17, 152f, 128f, 36f, 30f);
            ControlRoomUIFactory.createLabel(cli17.Find("Inner"), "V", "17", 17f, ControlRoomTheme.StatusAlarm, TextAlignmentOptions.Center, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(right, "CliMax", "/ 18", 16.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform, 194f, 130f, 60f, 28f);
            addHairline(right, 168f);

            mInstRunTimeTxt = ControlRoomUIFactory.createLabel(right, "RunT", "Run Time:   00:00:00", 16.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            ControlRoomUIFactory.setTopLeftRect(mInstRunTimeTxt.rectTransform, 14f, 182f, 280f, 28f);
            addHairline(right, 224f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(right, "IcLbl", "IC:  Full Power Steady State", 15.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform, 14f, 240f, 200f, 28f);
            ControlRoomUIFactory.createE2Control(right, "BtnReset", 220f, 236f, 70f, 36f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "Reset", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true);
            ControlRoomUIFactory.createE2Control(right, "BtnResetDrop", 292f, 236f, 24f, 36f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "▼", 10f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.Center, null, true);
            addHairline(right, 282f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(right, "AudK", "Audio:", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform, 14f, 300f, 72f, 28f);
            var audDot = ControlRoomUIFactory.createPanel(right, "AudDot", ControlRoomTheme.StatusGreen);
            ControlRoomUIFactory.setTopLeftRect(audDot, 84f, 304f, 18f, 18f);
            addHairline(right, 340f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(right, "RstK", "Restart Simulator:", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform, 14f, 358f, 180f, 30f);
            ControlRoomUIFactory.createE2Control(right, "BtnRestartSim", 274f, 352f, 42f, 42f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "①", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true);
            addHairline(right, 456f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(right, "SubSC", "Sim Controls", 15.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center).rectTransform, 14f, 466f, 108f, 26f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(right, "SubLine", ControlRoomTheme.HighlightBlue), 14f, 494f, 108f, 2.5f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(right, "SubLog", "Log", 15.5f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.Center).rectTransform, 128f, 466f, 54f, 26f);
            ControlRoomUIFactory.createHoverOnlyZone(right, "HitLog", 128f, 466f, 54f, 28f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(right, "DetHdr", "Details", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform, 14f, 508f, 120f, 28f);
        }

        private void addHairline(Transform parent, float y)
        {
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(parent, $"HL_{y}", new Color(0.42f, 0.44f, 0.47f, 0.6f)), 10f, y, 308f, 1.2f);
        }

        // =====================================================================
        // Capture[04..49]: Process Library 화면
        // =====================================================================
        private void buildProcessLibraryScreen(RectTransform host)
        {
            mProcessLibraryRoot = ControlRoomUIFactory.createPanel(host, "ProcessLibraryScreen", ControlRoomTheme.BgDarkPane);

            // 상단 서브 네비게이션 바 (y=0, h=46f, #5A5D61? Capture[04]은 다크 위에 Procedures / Automations / Active / Completed)
            var sub = ControlRoomUIFactory.createPanel(mProcessLibraryRoot, "SubBar", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(sub, 0f, 0f, 1920f, 46f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(sub, "SubLine", ControlRoomTheme.BorderSubtle), 0f, 44.5f, 1920f, 1.5f);

            mSubTabProcTxt = ControlRoomUIFactory.createLabel(sub, "TabProc", "Procedures", 24f, ControlRoomTheme.TabActiveGreen, TextAlignmentOptions.MidlineLeft);
            ControlRoomUIFactory.setTopLeftRect(mSubTabProcTxt.rectTransform, 18f, 4f, 150f, 38f);
            SubTabProcWidget = ControlRoomUIFactory.createE2Control(
                sub, "HitProc", 12f, 2f, 158f, 42f,
                Color.clear, Color.clear,
                null,
                () => mModel.CaptureStep == CaptureSequenceStep.Step18_Ramp80Completed,
                onClickProceduresSubTab);

            mSubTabAutoTxt = ControlRoomUIFactory.createLabel(sub, "TabAuto", "Automations", 24f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            ControlRoomUIFactory.setTopLeftRect(mSubTabAutoTxt.rectTransform, 184f, 4f, 168f, 38f);
            ControlRoomUIFactory.createHoverOnlyZone(sub, "HitAuto", 178f, 2f, 174f, 42f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(sub, "TabAct", "Active", 22f, ControlRoomTheme.TabActiveGreen, TextAlignmentOptions.Center).rectTransform, 1610f, 6f, 120f, 34f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(sub, "TabComp", "Completed", 22f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center).rectTransform, 1756f, 6f, 136f, 34f);
            ControlRoomUIFactory.createHoverOnlyZone(sub, "HitAct", 1605f, 2f, 130f, 42f);
            ControlRoomUIFactory.createHoverOnlyZone(sub, "HitComp", 1745f, 2f, 155f, 42f);

            // 좌측 트리 패널 (x=0..388, y=46..974)
            var leftPane = ControlRoomUIFactory.createPanel(mProcessLibraryRoot, "LeftPane", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(leftPane, 0f, 46f, 388f, ControlRoomTheme.ContentAreaHeight - 46f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(mProcessLibraryRoot, "DivL", ControlRoomTheme.BorderSubtle), 388f, 46f, 1.5f, ControlRoomTheme.ContentAreaHeight - 46f);

            var sb = ControlRoomUIFactory.createBorderedBox(leftPane, "SearchBox", ControlRoomTheme.BgInput, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(sb, 10f, 10f, 250f, 32f);
            ControlRoomUIFactory.createHoverOnlyZone(sb.Find("Inner"), "Hit", 0f, 0f, 248f, 30f);
            ControlRoomUIFactory.createE2Control(leftPane, "BtnSearch", 268f, 10f, 66f, 32f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "Search", 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true);
            ControlRoomUIFactory.createE2Control(leftPane, "BtnClearX", 340f, 10f, 34f, 32f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "X", 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true);

            buildProceduresLeftTree(leftPane);
            buildAutomationsLeftTree(leftPane);

            // 중앙 본문 영역 (x=389..1590, y=46..974)
            var centerHost = ControlRoomUIFactory.createPanel(mProcessLibraryRoot, "CenterHost", ControlRoomTheme.BgWorkspaceEmpty);
            ControlRoomUIFactory.setTopLeftRect(centerHost, 389.5f, 46f, 1200.5f, ControlRoomTheme.ContentAreaHeight - 46f);

            mCenterEmptyPane = ControlRoomUIFactory.createPanel(centerHost, "EmptyWorkspace", ControlRoomTheme.BgWorkspaceEmpty);
            buildProcedureDocView(centerHost);
            buildPowerAscensionDocView(centerHost);
            buildPowerManeuveringSetupView(centerHost);
            buildPowerManeuveringRunView(centerHost);

            // 우측 Active / Completed 패널 (x=1591..1920, y=46..974)
            buildRightActivePane(mProcessLibraryRoot);

            mProcessLibraryRoot.gameObject.SetActive(false);
        }

        private void buildProceduresLeftTree(RectTransform leftPane)
        {
            mProcTreeContainer = ControlRoomUIFactory.createPanel(leftPane, "ProcTreeContainer", Color.clear);
            ControlRoomUIFactory.setTopLeftRect(mProcTreeContainer, 0f, 48f, 388f, 880f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mProcTreeContainer, "ColTitle", "  Procedure Title", 14f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.MidlineLeft).rectTransform, 8f, 0f, 240f, 24f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mProcTreeContainer, "ColSteps", "# Steps", 14f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.MidlineRight).rectTransform, 250f, 0f, 104f, 24f);

            var vp = ControlRoomUIFactory.createPanel(mProcTreeContainer, "Viewport", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(vp, 8f, 26f, 370f, 844f);
            vp.gameObject.AddComponent<RectMask2D>();
            var content = ControlRoomUIFactory.createPanel(vp, "Content", Color.clear);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0f, 1f);
            content.sizeDelta = new Vector2(0f, 1180f);

            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 2f; vlg.padding = new RectOffset(2, 16, 2, 12);

            mProcTreeScroll = vp.gameObject.AddComponent<ScrollRect>();
            mProcTreeScroll.viewport = vp; mProcTreeScroll.content = content;
            mProcTreeScroll.horizontal = false; mProcTreeScroll.vertical = true; mProcTreeScroll.scrollSensitivity = 32f;

            string[] headCats = {
                "ADMIN  -  Administrative Procedures",
                "EOP     -  Emergency Operating Procedures",
                "IAP     -  Integrated Abnormal Procedures"
            };
            foreach (var c in headCats) createTreeRow(content, c, string.Empty, 48f, 0f, false, false, null, null);

            // IOP 카테고리 (Step02 의도 클릭 대상!)
            TreeIopWidget = createTreeRow(
                content,
                "IOP     -  Integrated Operating Procedures",
                string.Empty,
                48f,
                0f,
                false,
                true,
                () => mModel.CaptureStep == CaptureSequenceStep.Step02_ProcessLibraryEmpty,
                onClickIopCategory);

            // IOP 하위 컨테이너 (Step03+)
            mTreeSubIop = ControlRoomUIFactory.createPanel(content, "Sub_IOP", Color.clear);
            var subIopVlg = mTreeSubIop.gameObject.AddComponent<VerticalLayoutGroup>();
            subIopVlg.childControlWidth = true; subIopVlg.childForceExpandWidth = true;
            subIopVlg.childControlHeight = true; subIopVlg.childForceExpandHeight = false;
            subIopVlg.spacing = 2f;

            createTreeRow(mTreeSubIop, "A. Unit Shutdown", string.Empty, 38f, 28f, false, false, null, null);
            createTreeRow(mTreeSubIop, "C. Unit Startup", string.Empty, 38f, 28f, false, false, null, null);

            TreePowerOpWidget = createTreeRow(
                mTreeSubIop,
                "D. Power Operation",
                string.Empty,
                38f,
                28f,
                false,
                true,
                () => mModel.CaptureStep == CaptureSequenceStep.Step03_IopExpanded,
                onClickPowerOperation);

            // D. Power Operation 하위 컨테이너 (Step04+)
            mTreeSubPowerOp = ControlRoomUIFactory.createPanel(mTreeSubIop, "Sub_PowerOp", Color.clear);
            var subPoVlg = mTreeSubPowerOp.gameObject.AddComponent<VerticalLayoutGroup>();
            subPoVlg.childControlWidth = true; subPoVlg.childForceExpandWidth = true;
            subPoVlg.childControlHeight = true; subPoVlg.childForceExpandHeight = false;
            subPoVlg.spacing = 2f;

            createTreeRow(mTreeSubPowerOp, "A. Prerequisites", "5", 42f, 56f, false, false, null, null);
            TreePowerAscWidget = createTreeRow(
                mTreeSubPowerOp,
                "B. Power Ascension",
                "9",
                42f,
                56f,
                false,
                true,
                () => mModel.CaptureStep == CaptureSequenceStep.Step19_ProceduresTabForAscension,
                onClickPowerAscensionDoc,
                () => mModel.CaptureStep >= CaptureSequenceStep.Step20_PowerAscensionPreview);

            TreePowerRedWidget = createTreeRow(
                mTreeSubPowerOp,
                "C. Power Reduction",
                "7",
                42f,
                56f,
                true,
                true,
                () => mModel.CaptureStep == CaptureSequenceStep.Step04_PowerOpExpanded,
                onClickPowerReductionDoc,
                () => mModel.CaptureStep >= CaptureSequenceStep.Step05_PowerReductionPreview
                      && mModel.CaptureStep < CaptureSequenceStep.Step20_PowerAscensionPreview);

            createTreeRow(mTreeSubPowerOp, "D. Turbine Bypass Operation", "6", 42f, 56f, false, false, null, null);
            createTreeRow(mTreeSubPowerOp, "E. Flexible Power Operations", "11", 42f, 56f, false, false, null, null);

            createTreeRow(mTreeSubIop, "B. Unit Refueling", string.Empty, 38f, 28f, false, false, null, null);

            string[] tailCats = {
                "BAS     -  Boron Addition System",
                "BPS     -  Backup Power Supply System",
                "CAR     -  Condenser Air Removal System",
                "CE       -  Containment Evacuation System",
                "CNT     -  Containment System",
                "CVC     -  Chemical and Volume Control System",
                "ECC     -  Emergency Core Cooling System",
                "EHV     -  13.8 KV and Switchyard System",
                "MPS     -  Module Protection System",
                "MS      -  Main Steam System",
                "RCCW  -  Reactor Component Cooling Water System"
            };
            foreach (var c in tailCats) createTreeRow(content, c, string.Empty, 48f, 0f, false, false, null, null);
        }

        private E2InteractiveWidget createTreeRow(
            Transform parent,
            string title,
            string stepCnt,
            float height,
            float indent,
            bool isSelectedPowerRed,
            bool trackIntended,
            Func<bool> isIntended,
            Action onClick,
            Func<bool> isSelectedOverride = null)
        {
            var slot = ControlRoomUIFactory.createPanel(parent, "Slot_" + title, Color.clear);
            var le = slot.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height;

            float w = 346f - indent;
            Func<bool> selectedProv = isSelectedOverride
                ?? (isSelectedPowerRed ? () => mModel.CaptureStep >= CaptureSequenceStep.Step05_PowerReductionPreview : (Func<bool>)null);
            var widget = ControlRoomUIFactory.createE2Control(
                slot, "Btn",
                indent, 0f, w, height,
                ControlRoomTheme.BgListItem,
                ControlRoomTheme.AccentBlue,
                selectedProv,
                trackIntended ? isIntended : () => false,
                onClick != null ? () => onClick() : null,
                null, 14f, null, TextAlignmentOptions.MidlineLeft, null, true, ControlRoomTheme.BorderSubtle);

            var lbl = ControlRoomUIFactory.createLabel(widget.transform.Find("Inner"), "T", "  " + title, 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            lbl.rectTransform.offsetMax = new Vector2(-38f, 0f);
            if (!string.IsNullOrEmpty(stepCnt))
            {
                var cnt = ControlRoomUIFactory.createLabel(widget.transform.Find("Inner"), "C", stepCnt + "  ", 14.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineRight);
                cnt.rectTransform.offsetMin = new Vector2(w - 50f, 0f);
            }
            return widget;
        }

        private void buildAutomationsLeftTree(RectTransform leftPane)
        {
            mAutoTreeContainer = ControlRoomUIFactory.createPanel(leftPane, "AutoTreeContainer", Color.clear);
            ControlRoomUIFactory.setTopLeftRect(mAutoTreeContainer, 0f, 48f, 388f, 880f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mAutoTreeContainer, "ColTitle", "  Automation Title", 14f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.MidlineLeft).rectTransform, 8f, 0f, 240f, 24f);

            var list = ControlRoomUIFactory.createPanel(mAutoTreeContainer, "List", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(list, 8f, 26f, 370f, 844f);
            var vlg = list.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 2f; vlg.padding = new RectOffset(2, 16, 2, 12);

            createTreeRow(list, "RCCW - Reactor Component Cooling Water System", string.Empty, 48f, 0f, false, false, null, null);
            createTreeRow(list, "RPC  - Reactor Pool Cooling System", string.Empty, 48f, 0f, false, false, null, null);
            createTreeRow(list, "RXM  - Reactor Module", string.Empty, 48f, 0f, false, false, null, null);

            string[] rxmAutos = {
                "Collapse Pressurizer Steam Bubble",
                "Emergency Flood",
                "Feed Water Power Control",
                "Load Follow",
                "Plant Cooldown",
                "Plant Shutdown",
                "Power Maneuvering",
                "Startup: Pressurize and Secure ECCS",
                "Turbine Bypass Maneuvering"
            };
            foreach (var a in rxmAutos)
            {
                bool isPm = a == "Power Maneuvering";
                var w = ControlRoomUIFactory.createPanel(list, "ASlot_" + a, Color.clear);
                w.gameObject.AddComponent<LayoutElement>().preferredHeight = 46f;
                var btn = ControlRoomUIFactory.createE2Control(
                    w, "Btn", 28f, 0f, 318f, 46f,
                    isPm ? ControlRoomTheme.AccentBlue : ControlRoomTheme.BgListItem,
                    ControlRoomTheme.AccentBlue,
                    isPm ? () => true : (Func<bool>)null,
                    () => false,
                    null,
                    "  " + a, 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, null, true, ControlRoomTheme.BorderSubtle);
            }

            createTreeRow(list, "SCW  - Site Cooling Water System", string.Empty, 48f, 0f, false, false, null, null);
            createTreeRow(list, "SFPC - Spent Fuel Pool Cooling System", string.Empty, 48f, 0f, false, false, null, null);
            createTreeRow(list, "TG   - Turbine-Generator System", string.Empty, 48f, 0f, false, false, null, null);

            mAutoTreeContainer.gameObject.SetActive(false);
        }

        // =====================================================================
        // Procedure Document View (Capture[12..27], [38..44])
        // =====================================================================
        private void buildProcedureDocView(RectTransform centerHost)
        {
            mProcDocRoot = ControlRoomUIFactory.createPanel(centerHost, "ProcDocView", ControlRoomTheme.BgCard);
            mActiveOnlyDecorations.Clear();

            mProcDocHeaderTxt = ControlRoomUIFactory.createLabel(mProcDocRoot, "DocHdr", "Unit06  :  C. Power Reduction", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, FontStyles.Italic);
            ControlRoomUIFactory.setTopLeftRect(mProcDocHeaderTxt.rectTransform, 14f, 4f, 500f, 28f);

            var vp = ControlRoomUIFactory.createPanel(mProcDocRoot, "StepsVP", ControlRoomTheme.BgCard);
            ControlRoomUIFactory.setTopLeftRect(vp, 6f, 34f, 1188f, 838f);
            vp.gameObject.AddComponent<RectMask2D>();
            var content = ControlRoomUIFactory.createPanel(vp, "Content", Color.clear);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0f, 1f);
            content.sizeDelta = new Vector2(0f, 1080f);

            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 6f; vlg.padding = new RectOffset(8, 20, 6, 16);

            mProcDocScroll = vp.gameObject.AddComponent<ScrollRect>();
            mProcDocScroll.viewport = vp; mProcDocScroll.content = content;
            mProcDocScroll.horizontal = false; mProcDocScroll.vertical = true; mProcDocScroll.scrollSensitivity = 32f;

            // Step 1)
            buildActionDocStep(content, "Step1", "1)", "NOTIFY the load dispatcher of intended MW output change.", 50f, false,
                () => mModel.CaptureStep == CaptureSequenceStep.Step05_PowerReductionPreview
                      || mModel.CaptureStep == CaptureSequenceStep.Step06_PowerReductionActive,
                onClickStep1Check,
                out mStep1MinusBox, out var chk1, out mStep1BorderImg, out mStep1CheckImg, out mStep1Bubble, out mStep1Badge1);
            Step1CheckWidget = chk1;

            // Step 2) NOTE
            buildNoteDocStep(content, "2)", "NOTE: Power reduction is done using CONTROL rods to maintain Tave. Boration/Dilution is used only WHEN reactor engineering provides additional reactivity CONTROL guidance.", 66f);

            // Step 3) 전체 행 + N/A 접힘 행 + 우클릭/체크 메뉴 (Capture[22..25])
            mStep3FullRow = ControlRoomUIFactory.createPanel(content, "Step3_Full", Color.clear);
            mStep3FullRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 62f;
            buildActionStepWidgetsInside(mStep3FullRow, "3)", "IF a reactivity CONTROL plan has not been issued, THEN DO NOT SELECT 'USE Dilution' on the power maneuvering automation.", false,
                () => mModel.CaptureStep == CaptureSequenceStep.Step05_PowerReductionPreview
                      || mModel.CaptureStep == CaptureSequenceStep.Step06_PowerReductionActive
                      || mModel.CaptureStep == CaptureSequenceStep.Step07_Step1Checked,
                onClickStep3Check,
                out var s3Minus, out var chk3, out _, out _, out var s3Bub, out _);
            Step3CheckWidget = chk3;
            mActiveOnlyDecorations.Add(s3Minus);
            mActiveOnlyDecorations.Add(s3Bub);

            // Step 3) N/A 접힌 단일 행 (체크박스 32x32와 동일한 정사각형 배지 크기 및 수직 정렬)
            mStep3NaRow = ControlRoomUIFactory.createPanel(content, "Step3_NA", Color.clear);
            mStep3NaRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
            var plusBox = ControlRoomUIFactory.createBorderedBox(mStep3NaRow, "Plus", ControlRoomTheme.BgPanel, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(plusBox, 2f, 10f, 20f, 20f);
            ControlRoomUIFactory.createLabel(plusBox, "L", "+", 13f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mStep3NaRow, "Num", "3)", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold).rectTransform, 26f, 6f, 28f, 28f);
            var naBadge = ControlRoomUIFactory.createBorderedBox(
                mStep3NaRow, "NaBadge",
                ControlRoomTheme.hex("#9A9DA2"),
                ControlRoomTheme.hex("#D4D8DE"),
                1.5f);
            ControlRoomUIFactory.setTopLeftRect(naBadge, 58f, 4f, 32f, 32f);
            Step3NaBadgeRect = naBadge;
            var naTxt = ControlRoomUIFactory.createLabel(naBadge.Find("Inner"), "T", "N/A", 11.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
            naTxt.enableWordWrapping = false;
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mStep3NaRow, "Txt", "IF a reactivity CONTROL plan has not been issued, THEN DO NOT SELECT 'USE Dilution' on the pow...", 15.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform, 102f, 6f, 1030f, 28f);
            mStep3NaRow.gameObject.SetActive(false);

            // Step 4) NOTE
            buildNoteDocStep(content, "4)", "NOTE: The power maneuver automation lowers feed pump speed to lower reactor power. CONTROL rods will automatically maintain Tave at setpoint. Turbine governor valve will automatically maintain main steam header at setpoint.", 82f);

            // Step 5) NOTE
            buildNoteDocStep(content, "5)", "NOTE: Feedwater heater level cautions are expected WHEN reactor power is less than 60% as extraction steam rates are changing.", 62f);

            // Step 6) INITIATE + Link to: Power Maneuvering Automation
            buildActionDocStep(content, "Step6", "6)", "INITIATE power maneuvering automation to desired power level", 80f, true,
                () => mModel.CaptureStep == CaptureSequenceStep.Step14_ReturnedToProcCard, onClickStep6Check,
                out mStep6MinusBox, out var chk6, out mStep6BorderImg, out mStep6CheckImg, out mStep6Bubble, out mStep6Badge1);
            Step6CheckWidget = chk6;

            // Step 7) NOTE
            buildNoteDocStep(content, "7)", "NOTE: An RCS iodine SAMPLE is required to be taken within 1 hour of changing reactor power more than 15% per SR 3.4.8.2", 60f);

            // Step 8) NOTIFY Chemistry
            buildActionDocStep(content, "Step8", "8)", "NOTIFY Chemistry to PERFORM RCS AND Secondary chemistry samples/adds as required by the chemistry CONTROL plan.", 64f, false,
                () => mModel.CaptureStep == CaptureSequenceStep.Step15_Step6Checked, onClickStep8Check,
                out mStep8MinusBox, out var chk8, out mStep8BorderImg, out mStep8CheckImg, out mStep8Bubble, out mStep8Badge1);
            Step8CheckWidget = chk8;

            // Step 9) NOTE
            buildNoteDocStep(content, "9)", "NOTE: Only one main feed pump is required to be operating WHEN less than 80% reactor power. Two may be operating at lower demand.", 62f);

            // Step 10, 10.A, 10.B (Capture[42..44])
            buildPassiveActionRow(content, "10)", "WHEN the power maneuvering automation reaches the final desired power level, THEN PERFORM the following:", 46f, 0f);
            buildPassiveActionRow(content, "10.A)", "IF any alarm OR caution remains, THEN PERFORM actions as listed in the notification response procedures associated with those alarms OR cautions.", 64f, 64f);
            buildPassiveActionRow(content, "10.B)", "IF unit conditions are stable (refer to CONDUCT of Operations), THEN CONSIDER placing unit in PASSIVE CONTROL", 62f, 64f);

            // Step 3) 플로팅 컨텍스트 메뉴 (mProcDocRoot 위 최상단 배치 - Capture[22..24])
            mStep3ContextMenu = ControlRoomUIFactory.createBorderedBox(mProcDocRoot, "Step3ContextMenu", ControlRoomTheme.BgDarkest, ControlRoomTheme.HighlightBlue, 1.5f);
            ControlRoomUIFactory.setTopLeftRect(mStep3ContextMenu, 80f, 206f, 178f, 128f);
            var ctxInner = mStep3ContextMenu.Find("Inner");
            Step3NaMenuWidget = ControlRoomUIFactory.createE2Control(
                ctxInner, "BtnCtxNA", 2f, 4f, 171f, 28f,
                ControlRoomTheme.BgDarkest, ControlRoomTheme.ClickPressBlue,
                null,
                () => mModel.CaptureStep == CaptureSequenceStep.Step08_Step3MenuOpen,
                onClickStep3MenuNa,
                "  N/A", 14.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            ControlRoomUIFactory.createE2Control(ctxInner, "BtnCtxProg", 2f, 34f, 171f, 28f, ControlRoomTheme.BgDarkest, ControlRoomTheme.BgButton, null, () => false, null, "  In Progress", 14.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            ControlRoomUIFactory.createE2Control(ctxInner, "BtnCtxComp", 2f, 64f, 171f, 28f, ControlRoomTheme.BgDarkest, ControlRoomTheme.BgButton, null, () => false, null, "  Complete", 14.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            ControlRoomUIFactory.createE2Control(ctxInner, "BtnCtxAsgn", 2f, 94f, 171f, 28f, ControlRoomTheme.BgDarkest, ControlRoomTheme.BgButton, null, () => false, null, "  Assign Operator  ▶", 14.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            mStep3ContextMenu.gameObject.SetActive(false);

            // 하단 푸터: Preview(Start Procedure) vs Active(Skip / Check / Cancel)
            mPreviewFooter = ControlRoomUIFactory.createPanel(mProcDocRoot, "PreviewFooter", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(mPreviewFooter, 0f, 876f, 1200f, 52f);
            StartProcWidget = ControlRoomUIFactory.createE2Control(
                mPreviewFooter, "BtnStartProc",
                1012f, 8f, 166f, 36f,
                ControlRoomTheme.BgButton, ControlRoomTheme.BgButton,
                null,
                () => mModel.CaptureStep == CaptureSequenceStep.Step05_PowerReductionPreview,
                onClickStartProcedure,
                "Start Procedure", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);

            mActiveFooter = ControlRoomUIFactory.createPanel(mProcDocRoot, "ActiveFooter", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(mActiveFooter, 0f, 876f, 1200f, 52f);
            ControlRoomUIFactory.createE2Control(mActiveFooter, "BtnSkipRem", 662f, 8f, 156f, 36f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "Skip Remaining", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true);
            ControlRoomUIFactory.createE2Control(mActiveFooter, "BtnCheckRem", 830f, 8f, 164f, 36f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "Check Remaining", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true);
            ControlRoomUIFactory.createE2Control(mActiveFooter, "BtnCancelProc", 1006f, 8f, 172f, 36f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "Cancel Procedure", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true);
            mActiveFooter.gameObject.SetActive(false);

            mProcDocRoot.gameObject.SetActive(false);
        }

        private void buildNoteDocStep(Transform parent, string numText, string bodyText, float h, List<RectTransform> decoList = null)
        {
            var row = ControlRoomUIFactory.createPanel(parent, "NoteRow_" + numText, Color.clear);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = h;

            var minus = ControlRoomUIFactory.createBorderedBox(row, "Minus", ControlRoomTheme.BgPanel, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(minus, 2f, 16f, 20f, 20f);
            ControlRoomUIFactory.createLabel(minus, "L", "-", 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
            (decoList ?? mActiveOnlyDecorations).Add(minus);

            var box = ControlRoomUIFactory.createBorderedBox(row, "NoteBox", ControlRoomTheme.BgCard, ControlRoomTheme.HighlightBlue, 1.8f);
            ControlRoomUIFactory.setTopLeftRect(box, 26f, 2f, 1120f, h - 4f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(box, "Num", numText, 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold).rectTransform, 6f, 8f, 30f, 28f);
            var ico = ControlRoomUIFactory.createPanel(box, "Ico", Color.white, mSprites.icoNoteDoc);
            ControlRoomUIFactory.setTopLeftRect(ico, 44f, 10f, 34f, 36f);
            var txt = ControlRoomUIFactory.createLabel(box, "Txt", bodyText, 15.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(txt.rectTransform, 92f, 4f, 1018f, h - 10f);
        }

        private void buildCautionDocStep(Transform parent, string numText, string bodyText, float h, List<RectTransform> decoList = null)
        {
            var row = ControlRoomUIFactory.createPanel(parent, "CautionRow_" + numText, Color.clear);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = h;

            var minus = ControlRoomUIFactory.createBorderedBox(row, "Minus", ControlRoomTheme.BgPanel, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(minus, 2f, 16f, 20f, 20f);
            ControlRoomUIFactory.createLabel(minus, "L", "-", 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
            (decoList ?? mActiveOnlyDecorations).Add(minus);

            var box = ControlRoomUIFactory.createBorderedBox(row, "CautionBox", ControlRoomTheme.BgCard, ControlRoomTheme.StatusCaution, 1.8f);
            ControlRoomUIFactory.setTopLeftRect(box, 26f, 2f, 1120f, h - 4f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(box, "Num", numText, 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold).rectTransform, 6f, 8f, 36f, 28f);
            var ico = ControlRoomUIFactory.createPanel(box, "Ico", Color.white, mSprites.cautionOn);
            ControlRoomUIFactory.setTopLeftRect(ico, 46f, 12f, 32f, 32f);
            ico.GetComponent<Image>().preserveAspect = true;
            var txt = ControlRoomUIFactory.createLabel(box, "Txt", bodyText, 15.2f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(txt.rectTransform, 92f, 4f, 1018f, h - 10f);
        }

        private void buildActionDocStep(
            Transform parent,
            string key,
            string numText,
            string bodyText,
            float height,
            bool hasLink,
            Func<bool> isIntended,
            Action onCheck,
            out RectTransform minusBox,
            out E2InteractiveWidget checkWidget,
            out Image borderImg,
            out Image checkImg,
            out RectTransform bubbleRt,
            out RectTransform badge1Rt)
        {
            var row = ControlRoomUIFactory.createPanel(parent, key, Color.clear);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            buildActionStepWidgetsInside(row, numText, bodyText, hasLink, isIntended, onCheck, out minusBox, out checkWidget, out borderImg, out checkImg, out bubbleRt, out badge1Rt);
            mActiveOnlyDecorations.Add(minusBox);
            mActiveOnlyDecorations.Add(bubbleRt);
        }

        private void buildActionStepWidgetsInside(
            RectTransform row,
            string numText,
            string bodyText,
            bool hasLink,
            Func<bool> isIntended,
            Action onCheck,
            out RectTransform minusBox,
            out E2InteractiveWidget checkWidget,
            out Image borderImg,
            out Image checkImg,
            out RectTransform bubbleRt,
            out RectTransform badge1Rt)
        {
            minusBox = ControlRoomUIFactory.createBorderedBox(row, "Minus", ControlRoomTheme.BgPanel, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(minusBox, 2f, 8f, 20f, 20f);
            ControlRoomUIFactory.createLabel(minusBox, "L", "-", 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(row, "Num", numText, 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold).rectTransform, 26f, 4f, 28f, 28f);

            checkWidget = ControlRoomUIFactory.createE2Control(
                row, "BtnChk_" + numText,
                58f, 3f, 32f, 32f,
                ControlRoomTheme.BgInput, ControlRoomTheme.BgButton,
                null,
                isIntended,
                () => onCheck?.Invoke(),
                null, 14f, null, TextAlignmentOptions.Center, null, true, ControlRoomTheme.CheckboxBorder, 2.0f);
            borderImg = checkWidget.GetComponent<Image>();

            // 체크박스 배경 색상 변경(호버/클릭/refreshVisualColor)과 독립적으로 유지되는 체크마크 이미지
            var checkMarkRt = ControlRoomUIFactory.createPanel(checkWidget.transform.Find("Inner"), "CheckMark", Color.white, mSprites.icoCheckStep);
            ControlRoomUIFactory.setOffsets(checkMarkRt, 3f, 3f, -3f, -3f);
            checkImg = checkMarkRt.GetComponent<Image>();
            checkImg.preserveAspect = true;
            checkMarkRt.gameObject.SetActive(false);

            // 녹색 ① 원형 배지 (체크 완료 시 표시, 참조 이미지 동일 원형 링)
            badge1Rt = ControlRoomUIFactory.createPanel(row, "Badge1", ControlRoomTheme.StatusGreen, mSprites.circleDot);
            ControlRoomUIFactory.setTopLeftRect(badge1Rt, 47f, -4f, 20f, 20f);
            var badge1Inner = ControlRoomUIFactory.createPanel(badge1Rt, "InnerCircle", ControlRoomTheme.BgDarkest, mSprites.circleDot);
            ControlRoomUIFactory.setOffsets(badge1Inner, 2f, 2f, -2f, -2f);
            ControlRoomUIFactory.createLabel(badge1Inner, "N", "1", 11.5f, ControlRoomTheme.StatusGreen, TextAlignmentOptions.Center, FontStyles.Bold);
            badge1Rt.gameObject.SetActive(false);

            // 체크박스 하단 작은 말풍선
            bubbleRt = ControlRoomUIFactory.createPanel(row, "Bubble", Color.white, mSprites.icoSpeechBubble);
            ControlRoomUIFactory.setTopLeftRect(bubbleRt, 65f, 39f, 18f, 16f);

            var txt = ControlRoomUIFactory.createLabel(row, "Txt", bodyText, 15.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(txt.rectTransform, 102f, 4f, 1030f, hasLink ? 34f : 56f);

            if (hasLink)
            {
                Step6LinkWidget = ControlRoomUIFactory.createE2Control(
                    row, "BtnLinkPM",
                    102f, 42f, 410f, 30f,
                    Color.clear, Color.clear,
                    null,
                    () => mModel.CaptureStep == CaptureSequenceStep.Step09_Step3NaCollapsed
                          || (mModel.CaptureStep >= CaptureSequenceStep.Step14_ReturnedToProcCard
                              && mModel.CaptureStep <= CaptureSequenceStep.Step16_Step8Checked),
                    onClickLinkPowerManeuvering,
                    "<u><i>Link to: Power Maneuvering Automation</i></u>", 15.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            }
        }

        private void buildPassiveActionRow(Transform parent, string num, string body, float height, float xOff, List<RectTransform> decoList = null)
        {
            var row = ControlRoomUIFactory.createPanel(parent, "Passive_" + num, Color.clear);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;

            var minus = ControlRoomUIFactory.createBorderedBox(row, "Minus", ControlRoomTheme.BgPanel, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(minus, xOff + 2f, 8f, 20f, 20f);
            ControlRoomUIFactory.createLabel(minus, "L", "-", 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
            (decoList ?? mActiveOnlyDecorations).Add(minus);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(row, "Num", num, 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, FontStyles.Bold).rectTransform, xOff + 28f, 4f, 48f, 26f);

            // 체크박스는 절차서 진입 시점부터 항상 선명히 표시 (호버 전용, 선택 불가)
            ControlRoomUIFactory.createE2Control(row, "Chk_" + num, xOff + 76f, 3f, 32f, 32f, ControlRoomTheme.BgInput, ControlRoomTheme.BgButton, null, () => false, null, null, 14f, null, TextAlignmentOptions.Center, null, true, ControlRoomTheme.CheckboxBorder, 2.0f);

            var bub = ControlRoomUIFactory.createPanel(row, "Bub", Color.white, mSprites.icoSpeechBubble);
            ControlRoomUIFactory.setTopLeftRect(bub, xOff + 84f, 38f, 16f, 15f);
            (decoList ?? mActiveOnlyDecorations).Add(bub);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(row, "Txt", body, 15.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.TopLeft, FontStyles.Bold).rectTransform, xOff + 116f, 4f, 1020f - xOff, height - 6f);
        }

        // =====================================================================
        // Procedure Document View: B. Power Ascension (80% -> 100% 상승 시나리오)
        // =====================================================================
        private void buildPowerAscensionDocView(RectTransform centerHost)
        {
            mAscDocRoot = ControlRoomUIFactory.createPanel(centerHost, "AscDocView", ControlRoomTheme.BgCard);
            mAscActiveOnlyDecorations.Clear();

            var hdrTxt = ControlRoomUIFactory.createLabel(mAscDocRoot, "DocHdr", "Unit06  :  B. Power Ascension", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, FontStyles.Italic);
            ControlRoomUIFactory.setTopLeftRect(hdrTxt.rectTransform, 14f, 4f, 500f, 28f);

            var vp = ControlRoomUIFactory.createPanel(mAscDocRoot, "StepsVP", ControlRoomTheme.BgCard);
            ControlRoomUIFactory.setTopLeftRect(vp, 6f, 34f, 1188f, 838f);
            vp.gameObject.AddComponent<RectMask2D>();
            var content = ControlRoomUIFactory.createPanel(vp, "Content", Color.clear);
            content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f); content.pivot = new Vector2(0f, 1f);
            content.sizeDelta = new Vector2(0f, 1480f);

            var vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true; vlg.childForceExpandHeight = false;
            vlg.spacing = 6f; vlg.padding = new RectOffset(8, 20, 6, 16);

            var ascScroll = vp.gameObject.AddComponent<ScrollRect>();
            ascScroll.viewport = vp; ascScroll.content = content;
            ascScroll.horizontal = false; ascScroll.vertical = true; ascScroll.scrollSensitivity = 32f;

            // Step 1) NOTIFY load dispatcher (선택적 체크 가능)
            var row1 = ControlRoomUIFactory.createPanel(content, "AscStep1", Color.clear);
            row1.gameObject.AddComponent<LayoutElement>().preferredHeight = 50f;
            buildActionStepWidgetsInside(row1, "1)", "NOTIFY the load dispatcher of intended MW output change.", false,
                () => mModel.CaptureStep == CaptureSequenceStep.Step21_PowerAscensionActive && !mModel.isStepChecked("ASC_1"),
                onClickAscStep1Check,
                out var a1Minus, out var a1Chk, out mAscStep1BorderImg, out mAscStep1CheckImg, out var a1Bub, out mAscStep1Badge1);
            AscStep1CheckWidget = a1Chk;
            mAscActiveOnlyDecorations.Add(a1Minus);
            mAscActiveOnlyDecorations.Add(a1Bub);

            // Step 2)~4) NOTE
            buildNoteDocStep(content, "2)", "NOTE: Power ascension is done using CONTROL rods to maintain Tave. Boration/Dilution is used only WHEN reactor engineering provides additional reactivity CONTROL guidance.", 66f, mAscActiveOnlyDecorations);
            buildNoteDocStep(content, "3)", "NOTE: Feed water low flow alarms may ACTUATE WHEN reactor power is less than 50%. Feedwater heater level alarms should clear WHEN reactor power is greater than 60%.", 66f, mAscActiveOnlyDecorations);
            buildNoteDocStep(content, "4)", "NOTE: The power maneuver automation ramps feed pump speed to RAISE reactor power. CONTROL rods will automatically maintain Tave at setpoint. Turbine governor valve will automatically maintain main steam header at setpoint.", 82f, mAscActiveOnlyDecorations);

            // Step 5) INITIATE + Link to: Power Maneuvering Automation
            var row5 = ControlRoomUIFactory.createPanel(content, "AscStep5", Color.clear);
            row5.gameObject.AddComponent<LayoutElement>().preferredHeight = 80f;
            var a5Minus = ControlRoomUIFactory.createBorderedBox(row5, "Minus", ControlRoomTheme.BgPanel, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(a5Minus, 2f, 8f, 20f, 20f);
            ControlRoomUIFactory.createLabel(a5Minus, "L", "-", 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
            mAscActiveOnlyDecorations.Add(a5Minus);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(row5, "Num", "5)", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold).rectTransform, 26f, 4f, 28f, 28f);
            ControlRoomUIFactory.createE2Control(row5, "BtnChk_5", 58f, 3f, 32f, 32f, ControlRoomTheme.BgInput, ControlRoomTheme.BgButton, null, () => false, null, null, 14f, null, TextAlignmentOptions.Center, null, true, ControlRoomTheme.CheckboxBorder, 2.0f);
            var a5Bub = ControlRoomUIFactory.createPanel(row5, "Bubble", Color.white, mSprites.icoSpeechBubble);
            ControlRoomUIFactory.setTopLeftRect(a5Bub, 65f, 39f, 18f, 16f);
            mAscActiveOnlyDecorations.Add(a5Bub);

            var a5Txt = ControlRoomUIFactory.createLabel(row5, "Txt", "INITIATE power maneuvering automation to desired power level", 15.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(a5Txt.rectTransform, 102f, 4f, 1030f, 34f);

            AscStep5LinkWidget = ControlRoomUIFactory.createE2Control(
                row5, "BtnAscLinkPM",
                102f, 42f, 410f, 30f,
                Color.clear, Color.clear,
                null,
                () => mModel.CaptureStep == CaptureSequenceStep.Step21_PowerAscensionActive,
                onClickAscLinkPowerManeuvering,
                "<u><i>Link to: Power Maneuvering Automation</i></u>", 15.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);

            // Step 6)~14.B)
            buildNoteDocStep(content, "6)", "NOTE: An RCS iodine SAMPLE is required to be taken within 1 hour of changing reactor power more than 15% per SR 3.4.8.2", 60f, mAscActiveOnlyDecorations);
            buildPassiveActionRow(content, "7)", "NOTIFY Chemistry to PERFORM RCS AND Secondary chemistry samples/adds as required by the chemistry CONTROL plan.", 64f, 0f, mAscActiveOnlyDecorations);
            buildNoteDocStep(content, "8)", "NOTE: Feed regulating valve position is typically 50% WHEN operating at power. Feed regulating valves may need to be slowly opened IF performing first power ascension following generator sync.", 72f, mAscActiveOnlyDecorations);
            buildCautionDocStep(content, "9)", "CAUTION: Opening feed reg valves too fast could cause loss of superheat AND a reactor TRIP. Allow Tave to stabilize between each adjustment. Adjustments should be done slowly with power maneuver automation in progress.", 86f, mAscActiveOnlyDecorations);
            buildPassiveActionRow(content, "10)", "IF feed regulating valves are not 50% OPEN, THEN SLOWLY OPEN feed regulating valves in equal increments until valves are 50% OPEN.", 62f, 0f, mAscActiveOnlyDecorations);
            buildCautionDocStep(content, "11)", "CAUTION: The dilution controller will CALCULATE total gallons based on temperature deviation. Recommend 200 gallons of dilutions occur at a time AND allow conditions to stabilize for large temperature deviations.", 80f, mAscActiveOnlyDecorations);
            buildPassiveActionRow(content, "12)", "IF CONTROL rods are fully withdrawn AND Tave is less than 543 F, THEN PERFORM dilutions as issued by notification requests to RAISE Tave.", 62f, 0f, mAscActiveOnlyDecorations);
            buildPassiveActionRow(content, "13)", "IF reactor power is greater than 80%, THEN ENSURE two feed pumps are operating", 50f, 0f, mAscActiveOnlyDecorations);
            buildPassiveActionRow(content, "14)", "WHEN the power maneuvering automation reaches the final desired power level, THEN PERFORM the following:", 48f, 0f, mAscActiveOnlyDecorations);
            buildPassiveActionRow(content, "14.A)", "IF any alarm OR caution remains, THEN PERFORM actions as listed in the notification response procedures associated with those alarms OR cautions.", 64f, 64f, mAscActiveOnlyDecorations);
            buildPassiveActionRow(content, "14.B)", "IF unit conditions are stable (refer to CONDUCT of Operations), THEN CONSIDER placing unit in PASSIVE CONTROL.", 62f, 64f, mAscActiveOnlyDecorations);

            // 하단 푸터: Preview(Start Procedure) vs Active
            mAscPreviewFooter = ControlRoomUIFactory.createPanel(mAscDocRoot, "AscPreviewFooter", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(mAscPreviewFooter, 0f, 876f, 1200f, 52f);
            AscStartProcWidget = ControlRoomUIFactory.createE2Control(
                mAscPreviewFooter, "BtnAscStartProc",
                1012f, 8f, 166f, 36f,
                ControlRoomTheme.BgButton, ControlRoomTheme.BgButton,
                null,
                () => mModel.CaptureStep == CaptureSequenceStep.Step20_PowerAscensionPreview,
                onClickAscStartProcedure,
                "Start Procedure", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);

            mAscActiveFooter = ControlRoomUIFactory.createPanel(mAscDocRoot, "AscActiveFooter", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(mAscActiveFooter, 0f, 876f, 1200f, 52f);
            ControlRoomUIFactory.createE2Control(mAscActiveFooter, "BtnAscSkipRem", 662f, 8f, 156f, 36f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "Skip Remaining", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true);
            ControlRoomUIFactory.createE2Control(mAscActiveFooter, "BtnAscCheckRem", 830f, 8f, 164f, 36f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "Check Remaining", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true);
            ControlRoomUIFactory.createE2Control(mAscActiveFooter, "BtnAscCancelProc", 1006f, 8f, 172f, 36f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "Cancel Procedure", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true);
            mAscActiveFooter.gameObject.SetActive(false);

            mAscDocRoot.gameObject.SetActive(false);
        }

        // =====================================================================
        // Power Maneuvering Setup View (Capture[28..34])
        // =====================================================================
        private void buildPowerManeuveringSetupView(RectTransform centerHost)
        {
            mPmSetupRoot = ControlRoomUIFactory.createPanel(centerHost, "PmSetupView", ControlRoomTheme.BgCard);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mPmSetupRoot, "PathHdr", "Unit06  :  Power Maneuvering", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, FontStyles.Italic).rectTransform, 14f, 4f, 400f, 28f);

            // 상단 메트릭 패널
            var topCard = ControlRoomUIFactory.createBorderedBox(mPmSetupRoot, "TopMetrics", ControlRoomTheme.BgPrimary, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(topCard, 6f, 34f, 1188f, 144f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(topCard, "T", "Power Maneuvering", 17f, ControlRoomTheme.TextPrimary).rectTransform, 12f, 4f, 220f, 26f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(topCard, "SqH", ControlRoomTheme.ActuatedOrange), 12f, 38f, 14f, 14f);
            mPmHdrTHot = createMetricPair(topCard, "RCS T Hot:", 30f, 32f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(topCard, "SqC", ControlRoomTheme.TelemetryCyan), 12f, 74f, 14f, 14f);
            mPmHdrTCold = createMetricPair(topCard, "RCS T Cold:", 30f, 68f);
            mPmHdrTAve = createMetricPair(topCard, "RCS T Ave:", 30f, 104f);

            mPmHdrPzrP = createMetricPair(topCard, "PZR Pressure:", 290f, 32f);
            mPmHdrCorePwr = createMetricPair(topCard, "Core Thermal Power:", 290f, 68f);
            mPmHdrNiPwr = createMetricPair(topCard, "Core NI Power:", 290f, 104f);

            mPmHdrMsFlow = createMetricPair(topCard, "MS Flow Rate:", 610f, 32f);
            mPmHdrFwFlow = createMetricPair(topCard, "FW Flow Rate:", 610f, 68f);
            mPmHdrMsP = createMetricPair(topCard, "MS Header Pressure:", 610f, 104f);

            // 미니 십자 제어봉 맵 (+ 모양 8셀 + 가운데 빈칸, 현재 노심 제어봉 스텝 연동)
            mPmSetupCrossRods.Clear();
            var crossCoords = new (int r, int c)[] { (0, 2), (1, 2), (2, 0), (2, 1), (2, 3), (2, 4), (3, 2), (4, 2) };
            foreach (var rc in crossCoords)
            {
                bool isInner = Mathf.Abs(rc.r - 2) <= 1 && Mathf.Abs(rc.c - 2) <= 1;
                var cell = ControlRoomUIFactory.createPanel(topCard, $"Cross_{rc.r}_{rc.c}", ControlRoomTheme.RodCyan);
                ControlRoomUIFactory.setTopLeftRect(cell, 980f + rc.c * 34f, 8f + rc.r * 25f, 31f, 22f);
                var lbl = ControlRoomUIFactory.createLabel(cell, "N", "201", 11f, ControlRoomTheme.BgDarkest, TextAlignmentOptions.Center, FontStyles.Bold);
                mPmSetupCrossRods.Add((cell.GetComponent<Image>(), lbl, isInner));
            }

            // 중간 탭들 (Temperatures 활성 파란색 + 호버 전용 4개)
            string[] tabs = { "Temperatures", "Flow Rates", "Levels", "Pressures", "PT Operation Plot" };
            float tx = 6f;
            for (int i = 0; i < tabs.Length; i++)
            {
                float tw = i == 4 ? 156f : 112f;
                ControlRoomUIFactory.createE2Control(mPmSetupRoot, "PmTab_" + tabs[i], tx, 182f, tw, 30f,
                    i == 0 ? ControlRoomTheme.AccentBlue : ControlRoomTheme.BgPanel, ControlRoomTheme.AccentBlue,
                    i == 0 ? () => true : (Func<bool>)null, () => false, null, tabs[i], 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true);
                tx += tw + 4f;
            }

            // Temperatures 차트 영역 (y=214..456) — 좌측 °F 컬러 밴드(노랑/시안/적) 포함
            var chartPanel = ControlRoomUIFactory.createBorderedBox(mPmSetupRoot, "SetupChartBox", ControlRoomTheme.BgPrimary, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(chartPanel, 6f, 214f, 1188f, 270f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(chartPanel, "DegF", "°F", 11f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.Center).rectTransform, 6f, 12f, 22f, 18f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(chartPanel, "BandY", ControlRoomTheme.StatusCaution), 8f, 44f, 14f, 52f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(chartPanel, "BandC", ControlRoomTheme.TelemetryCyan), 8f, 98f, 14f, 58f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(chartPanel, "BandR", ControlRoomTheme.StatusAlarm), 8f, 158f, 14f, 54f);

            mPmSetupChart = ControlRoomUIFactory.createTrendChart(chartPanel, "PmSetupChart", 34f, 4f, 1146f, 260f, 400f, 600f, 8, 5, true);

            // Capture[28] 초기 진입 시 "Please wait..." 오버레이 (0.35초 후 자동 사라짐)
            mPmChartPleaseWait = ControlRoomUIFactory.createPanel(chartPanel, "PleaseWaitOverlay", new Color(0.32f, 0.34f, 0.36f, 0.92f));
            ControlRoomUIFactory.setTopLeftRect(mPmChartPleaseWait, 84f, 16f, 1080f, 220f);
            ControlRoomUIFactory.createLabel(mPmChartPleaseWait, "PW", "Please wait...", 40f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center);
            mPmChartPleaseWait.gameObject.SetActive(false);

            // 3행 온도 레전드 테이블 (파란 상단 강조선 포함)
            var legTbl = ControlRoomUIFactory.createBorderedBox(mPmSetupRoot, "PmSetupLeg", ControlRoomTheme.BgDarkPane, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(legTbl, 6f, 488f, 1188f, 118f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(legTbl, "BlueLine", ControlRoomTheme.HighlightBlue), 4f, 24f, 1180f, 2f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(legTbl, "H", "      Description                                           Value     Unit     +     Name", 13.5f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.MidlineLeft).rectTransform, 6f, 2f, 1170f, 22f);

            mPmLegTHotV = addPmLegRow(legTbl, 28f, ControlRoomTheme.StatusCaution, "Reactor T-HOT", "°F", "06_RCS_TE_1005A");
            mPmLegTColdV = addPmLegRow(legTbl, 56f, ControlRoomTheme.TelemetryCyan, "Reactor T-COLD", "°F", "06_RCS_TE_0009A");
            mPmLegTAveV = addPmLegRow(legTbl, 84f, ControlRoomTheme.StatusAlarm, "Reactor T-AVE", "°F", "06_RCS_TE_AVE");

            // 하단 Parameters & Execution Summary (Capture[29..34])
            var botBox = ControlRoomUIFactory.createBorderedBox(mPmSetupRoot, "PmBotBox", ControlRoomTheme.BgPanel, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(botBox, 6f, 616f, 1188f, 256f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(botBox, "PT", "Power Maneuvering Parameters", 17f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform, 14f, 10f, 340f, 28f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(botBox, "L1", "Target Power (%):", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform, 14f, 52f, 190f, 30f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(botBox, "L2", "Ramp Rate (%/Hr):", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform, 14f, 90f, 190f, 30f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(botBox, "L3", "Use Dilution:", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform, 14f, 128f, 190f, 30f);

            // Target Power (%) 입력 (감소: 80/300 동시 입력, 상승: 100/20 동시 입력)
            PmTargetFieldWidget = ControlRoomUIFactory.createE2Control(
                botBox, "InputTargetPwr",
                220f, 50f, 120f, 32f,
                ControlRoomTheme.BgInput, ControlRoomTheme.BgButton,
                null,
                () => mModel.CaptureStep == CaptureSequenceStep.Step10_PowerManeuveringSetup
                      || mModel.CaptureStep == CaptureSequenceStep.Step22_AscPmSetup,
                onClickTargetPowerBox,
                "100", 15.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, null, true, ControlRoomTheme.BorderSubtle);
            mPmTargetTxt = PmTargetFieldWidget.transform.Find("Inner/Label").GetComponent<TextMeshProUGUI>();

            // Ramp Rate (%/Hr) 입력 (감소: 80/300 동시 입력, 상승: 100/20 동시 입력)
            PmRateFieldWidget = ControlRoomUIFactory.createE2Control(
                botBox, "InputRampRate",
                220f, 88f, 120f, 32f,
                ControlRoomTheme.BgInput, ControlRoomTheme.BgButton,
                null,
                () => mModel.CaptureStep == CaptureSequenceStep.Step10_PowerManeuveringSetup
                      || mModel.CaptureStep == CaptureSequenceStep.Step22_AscPmSetup,
                onClickRampRateBox,
                "20", 15.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, null, true, ControlRoomTheme.BorderSubtle);
            mPmRateTxt = PmRateFieldWidget.transform.Find("Inner/Label").GetComponent<TextMeshProUGUI>();

            // Use Dilution 호버 전용 체크박스
            ControlRoomUIFactory.createE2Control(botBox, "ChkDilution", 268f, 130f, 22f, 22f, ControlRoomTheme.BgInput, ControlRoomTheme.BgInput, null, () => false, null, null, 12f, null, TextAlignmentOptions.Center, null, true);

            // Execution Summary (Capture[29..34])
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(botBox, "ST", "Execution Summary", 17f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineRight).rectTransform, 840f, 10f, 330f, 28f);
            var sumCard = ControlRoomUIFactory.createBorderedBox(botBox, "SumBox", ControlRoomTheme.BgInput, ControlRoomTheme.TextPrimary, 1.5f);
            ControlRoomUIFactory.setTopLeftRect(sumCard, 856f, 44f, 318f, 132f);
            mPmSummaryTxt = ControlRoomUIFactory.createLabel(sumCard.Find("Inner"), "Val", "", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.TopLeft);
            ControlRoomUIFactory.setOffsets(mPmSummaryTxt.rectTransform, 10f, 8f, -8f, -8f);

            // 하단 푸터: Ready to start + [Validate] + [Execute]
            var footer = ControlRoomUIFactory.createPanel(mPmSetupRoot, "PmSetupFooter", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(footer, 0f, 876f, 1200f, 52f);
            mPmReadyTxt = ControlRoomUIFactory.createLabel(footer, "ReadyTxt", "Ready to start", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(mPmReadyTxt.rectTransform, 796f, 8f, 160f, 36f);
            mPmReadyTxt.gameObject.SetActive(false);

            PmValidateWidget = ControlRoomUIFactory.createE2Control(
                footer, "BtnValidate",
                966f, 8f, 102f, 36f,
                ControlRoomTheme.BgButton, ControlRoomTheme.BgButton,
                null,
                () => mModel.CaptureStep == CaptureSequenceStep.Step11_PmParamsEntered
                      || mModel.CaptureStep == CaptureSequenceStep.Step23_AscPmParamsEntered,
                onClickValidatePm,
                "Validate", 15.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);

            PmExecuteWidget = ControlRoomUIFactory.createE2Control(
                footer, "BtnExecute",
                1076f, 8f, 102f, 36f,
                ControlRoomTheme.BgButton, ControlRoomTheme.BgButton,
                null,
                () => mModel.CaptureStep == CaptureSequenceStep.Step12_PmValidated
                      || mModel.CaptureStep == CaptureSequenceStep.Step24_AscPmValidated,
                onClickExecutePm,
                "Execute", 15.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);

            mPmSetupRoot.gameObject.SetActive(false);
        }

        private TextMeshProUGUI addPmLegRow(RectTransform tbl, float y, Color swatch, string desc, string unit, string tag)
        {
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(tbl, "Sw_" + desc, swatch), 14f, y + 6f, 10f, 10f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(tbl, "D_" + desc, desc, 13.5f, ControlRoomTheme.TextPrimary).rectTransform, 36f, y, 270f, 22f);
            var vLbl = ControlRoomUIFactory.createLabel(tbl, "V_" + desc, "--", 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineRight);
            ControlRoomUIFactory.setTopLeftRect(vLbl.rectTransform, 316f, y, 64f, 22f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(tbl, "U_" + desc, unit, 13.5f, ControlRoomTheme.TextSecondary).rectTransform, 404f, y, 42f, 22f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(tbl, "T_" + desc, tag, 13.5f, ControlRoomTheme.TextSecondary).rectTransform, 470f, y, 260f, 22f);
            return vLbl;
        }

        private TextMeshProUGUI createMetricPair(Transform parent, string key, float x, float y)
        {
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(parent, key + "_K", key, 15.5f, ControlRoomTheme.TextPrimary).rectTransform, x, y, 168f, 26f);
            var v = ControlRoomUIFactory.createLabel(parent, key + "_V", "--", 18.5f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(v.rectTransform, x + 170f, y, 110f, 26f);
            return v;
        }

        // =====================================================================
        // Power Maneuver Running View (Capture[35..49])
        // =====================================================================
        private void buildPowerManeuveringRunView(RectTransform centerHost)
        {
            mPmRunRoot = ControlRoomUIFactory.createPanel(centerHost, "PmRunView", ControlRoomTheme.BgCard);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mPmRunRoot, "PathHdr", "Unit06  :  Power Maneuvering", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, FontStyles.Italic).rectTransform, 14f, 4f, 400f, 28f);

            var top = ControlRoomUIFactory.createPanel(mPmRunRoot, "RunTop", ControlRoomTheme.BgPrimary);
            ControlRoomUIFactory.setTopLeftRect(top, 6f, 34f, 1188f, 326f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(top, "T", "Power Maneuver", 17.5f, ControlRoomTheme.TextPrimary).rectTransform, 10f, 4f, 200f, 28f);

            mRunReqPwr = addRunMetric(top, "Requested Power", 42f);
            mRunCurPwr = addRunMetric(top, "Current Power", 82f);
            mRunCoreP = addRunMetric(top, "Core Pressure", 122f);
            mRunTAvg = addRunMetric(top, "Core T Avg", 162f);
            mRunFwFlow = addRunMetric(top, "FW Flow Rate", 202f);
            mRunMsFlow = addRunMetric(top, "MS Flow Rate", 242f);
            mRunTurbPwr = addRunMetric(top, "Turbine Power", 282f);

            // 설비 심볼 + 진행 바 (Capture[35..49] Governor / Feed Pump A / B / C)
            ControlRoomUIFactory.createStaticPump(top, "IcoGov", 468f, 52f, 42f, mSprites.icoGovCyan);
            mRunGovFill = createEquipmentBar(top, "Governor", "", Color.white, 502f, 20f, out mRunGovPct);

            ControlRoomUIFactory.createStaticPump(top, "IcoPumpA", 468f, 126f, 44f, mSprites.icoPumpCyan);
            mRunPumpAFill = createEquipmentBar(top, "Feed Pump ", "A", ControlRoomTheme.StatusCaution, 502f, 94f, out mRunPumpAPct);

            ControlRoomUIFactory.createStaticPump(top, "IcoPumpB", 468f, 200f, 44f, mSprites.icoPumpCyan);
            mRunPumpBFill = createEquipmentBar(top, "Feed Pump ", "B", ControlRoomTheme.TelemetryCyan, 502f, 168f, out mRunPumpBPct);

            ControlRoomUIFactory.createStaticPump(top, "IcoPumpC", 468f, 274f, 44f, mSprites.icoPumpDark);
            mRunPumpCFill = createEquipmentBar(top, "Feed Pump ", "C", ControlRoomTheme.StatusAlarm, 502f, 242f, out mRunPumpCPct);

            // 37셀 노심 제어봉 맵 (Capture[35], [48], [49] 1:1 재현: (r+c)%2==1 인 16개 제어봉 중 십자 8개가 황색 178로 강하)
            mRunCoreCells.Clear();
            for (int r = 0; r < 7; r++)
            {
                for (int c = 0; c < 7; c++)
                {
                    if ((r == 0 || r == 6) && (c < 2 || c > 4)) continue;
                    if ((r == 1 || r == 5) && (c == 0 || c == 6)) continue;

                    bool isRod = (r + c) % 2 == 1;
                    bool isCrossReg = isRod && (r == 3 || c == 3);
                    bool isInnerCross = isCrossReg && Mathf.Abs(r - 3) <= 1 && Mathf.Abs(c - 3) <= 1;
                    var box = ControlRoomUIFactory.createBorderedBox(top, $"Core_{r}_{c}",
                        isRod ? ControlRoomTheme.RodCyan : ControlRoomTheme.BgDarkPane,
                        ControlRoomTheme.BorderSubtle, 1.5f);
                    ControlRoomUIFactory.setTopLeftRect(box, 800f + c * 48f, 16f + r * 43f, 44f, 38f);
                    var innerImg = box.Find("Inner").GetComponent<Image>();
                    var lbl = ControlRoomUIFactory.createLabel(innerImg.transform, "N", isRod ? "201" : string.Empty, 12.5f, ControlRoomTheme.BgDarkest, TextAlignmentOptions.Center, FontStyles.Bold);
                    mRunCoreCells.Add((innerImg, lbl, isCrossReg, isInnerCross));
                }
            }

            // 탭 Core / FW/MS / Pumps / Control Rods
            string[] runTabs = { "Core", "FW/MS", "Pumps", "Control Rods" };
            float rtx = 6f;
            for (int i = 0; i < runTabs.Length; i++)
            {
                float tw = i == 3 ? 116f : 72f;
                ControlRoomUIFactory.createE2Control(mPmRunRoot, "RunTab_" + runTabs[i], rtx, 366f, tw, 30f,
                    i == 0 ? ControlRoomTheme.AccentBlue : ControlRoomTheme.BgPanel, ControlRoomTheme.AccentBlue,
                    i == 0 ? () => true : (Func<bool>)null, () => false, null, runTabs[i], 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true);
                rtx += tw + 4f;
            }

            // 다중 축 Core 차트 (이미지 2: 빈 격자+X축 시간 / 이미지 3,4,5,6: psia·%·°F 다중 축+컬러 밴드+우측 실시간 곡선)
            var rChartBox = ControlRoomUIFactory.createBorderedBox(mPmRunRoot, "RunChartBox", ControlRoomTheme.BgPrimary, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(rChartBox, 6f, 398f, 1188f, 312f);
            mRunChart = ControlRoomUIFactory.createTrendChart(rChartBox, "RunTrend", 94f, 6f, 1028f, 298f, 0f, 130f, 8, 5, true);
            for (int i = 0; i <= 5; i++)
            {
                var yl = mRunChart.transform.Find($"YLabel_{i}");
                if (yl != null) yl.gameObject.SetActive(false);
                var tl = mRunChart.transform.Find($"Time_{i}");
                if (tl != null) tl.gameObject.SetActive(false);
            }

            mRunChartAxesGroup = ControlRoomUIFactory.createPanel(rChartBox, "RunChartAxesGroup", Color.clear);
            ControlRoomUIFactory.setTopLeftRect(mRunChartAxesGroup, 0f, 0f, 1188f, 312f);

            // 좌측 축 1: PZR Pressure (psia, 1500..2200, 적색 바)
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mRunChartAxesGroup, "UnitPsia", "psia", 11f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.Center).rectTransform, 4f, 6f, 38f, 16f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(mRunChartAxesGroup, "BandP", ControlRoomTheme.StatusAlarm), 6f, 128f, 12f, 52f);
            int[] psiaTicks = { 2200, 2060, 1920, 1780, 1640, 1500 };
            for (int r = 0; r < psiaTicks.Length; r++)
            {
                float ty = 18f + (262f / 5f) * r - 9f;
                var lbl = ControlRoomUIFactory.createLabel(mRunChartAxesGroup, $"PsiaTick_{r}", psiaTicks[r].ToString(), 11f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.MidlineRight);
                ControlRoomUIFactory.setTopLeftRect(lbl.rectTransform, 14f, ty, 38f, 18f);
            }

            // 좌측 축 2: Thermal Power (%, 0..130, 황색 바)
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mRunChartAxesGroup, "UnitPct", "%", 11f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.Center).rectTransform, 64f, 6f, 28f, 16f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(mRunChartAxesGroup, "BandPct", ControlRoomTheme.StatusCaution), 66f, 118f, 12f, 56f);
            int[] pctTicks = { 130, 104, 78, 52, 26, 0 };
            for (int r = 0; r < pctTicks.Length; r++)
            {
                float ty = 18f + (262f / 5f) * r - 9f;
                var lbl = ControlRoomUIFactory.createLabel(mRunChartAxesGroup, $"PctTick_{r}", pctTicks[r].ToString(), 11f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.MidlineRight);
                ControlRoomUIFactory.setTopLeftRect(lbl.rectTransform, 96f, ty, 40f, 18f);
            }

            // 우측 축 3: Core Average Temperature (°F, 500..600, 시안 바)
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mRunChartAxesGroup, "UnitDegF", "°F", 11f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.Center).rectTransform, 1124f, 6f, 30f, 16f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(mRunChartAxesGroup, "BandT", ControlRoomTheme.TelemetryCyan), 1166f, 122f, 12f, 56f);
            int[] tempTicks = { 600, 580, 560, 540, 520, 500 };
            for (int r = 0; r < tempTicks.Length; r++)
            {
                float ty = 18f + (262f / 5f) * r - 9f;
                var lbl = ControlRoomUIFactory.createLabel(mRunChartAxesGroup, $"TempTick_{r}", tempTicks[r].ToString(), 11f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.MidlineLeft);
                ControlRoomUIFactory.setTopLeftRect(lbl.rectTransform, 1114f, ty, 42f, 18f);
            }
            mRunChartAxesGroup.gameObject.SetActive(false);

            // 차트 하단 X축 실시간 타임스탬프 (9개)
            mRunChartTimeLabels.Clear();
            const float plotLeft = 144f;
            const float plotWidth = 964f;
            for (int i = 0; i < 9; i++)
            {
                float tx = plotLeft + (plotWidth / 8f) * i - 34f;
                var tl = ControlRoomUIFactory.createLabel(rChartBox, $"RunTime_{i}", "08:00:00", 10.5f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.Center);
                ControlRoomUIFactory.setTopLeftRect(tl.rectTransform, tx, 286f, 68f, 20f);
                mRunChartTimeLabels.Add(tl);
            }

            // 3행 레전드 테이블
            var rLeg = ControlRoomUIFactory.createBorderedBox(mPmRunRoot, "RunLegTbl", ControlRoomTheme.BgDarkPane, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(rLeg, 6f, 714f, 1188f, 118f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(rLeg, "BLine", ControlRoomTheme.HighlightBlue), 4f, 24f, 1180f, 2f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(rLeg, "H", "      Description                                                       Value      Unit      +      Name", 13.5f, ControlRoomTheme.TextSecondary).rectTransform, 6f, 2f, 1170f, 22f);

            mRunLegPwrV = addPmLegRow(rLeg, 28f, ControlRoomTheme.StatusCaution, "Reactor Power Output from Thermal Power", "%", "06_RXM_PWR_THERMAL");
            mRunLegTAvgV = addPmLegRow(rLeg, 56f, ControlRoomTheme.TelemetryCyan, "Reactor Core Average Temperature", "°F", "06_ICI_TE_CORE_AVERAGE");
            mRunLegPzrPV = addPmLegRow(rLeg, 84f, ControlRoomTheme.StatusAlarm, "PZR pressure", "psia", "06_RCS_PE_1013A");
            mRunLegSwatches.Clear();
            string[] swatchNames =
            {
                "Sw_Reactor Power Output from Thermal Power",
                "Sw_Reactor Core Average Temperature",
                "Sw_PZR pressure"
            };
            foreach (string swName in swatchNames)
            {
                Transform sw = rLeg.Find(swName);
                if (sw != null) mRunLegSwatches.Add(sw.gameObject);
            }

            // 하단 Cancel Automation? [Cancel] (호버 전용, Capture[49])
            var rFoot = ControlRoomUIFactory.createPanel(mPmRunRoot, "RunFoot", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(rFoot, 0f, 876f, 1200f, 52f);
            ControlRoomUIFactory.createE2Control(rFoot, "ChkCancel", 864f, 14f, 22f, 22f, ControlRoomTheme.BgInput, ControlRoomTheme.BgInput, null, () => false, null, null, 12f, null, TextAlignmentOptions.Center, null, true);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(rFoot, "CanTxt", "Cancel Automation?", 15f, ControlRoomTheme.TextPrimary).rectTransform, 894f, 12f, 160f, 26f);
            ControlRoomUIFactory.createE2Control(rFoot, "BtnCancelAuto", 1068f, 8f, 110f, 36f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "Cancel", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true);

            mPmRunRoot.gameObject.SetActive(false);
        }

        private TextMeshProUGUI addRunMetric(Transform parent, string label, float y)
        {
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(parent, label + "_K", label, 16f, ControlRoomTheme.TextPrimary).rectTransform, 14f, y, 168f, 28f);
            var v = ControlRoomUIFactory.createLabel(parent, label + "_V", "--", 19f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(v.rectTransform, 200f, y, 140f, 28f);
            return v;
        }

        private RectTransform createEquipmentBar(Transform parent, string prefix, string suffix, Color suffixColor, float x, float y, out TextMeshProUGUI pctLbl)
        {
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(parent, "L_" + prefix + suffix, prefix, 15.5f, ControlRoomTheme.TextPrimary).rectTransform, x, y, 92f, 24f);
            if (!string.IsNullOrEmpty(suffix))
            {
                ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(parent, "S_" + suffix, suffix, 16f, suffixColor, TextAlignmentOptions.MidlineLeft, FontStyles.Bold).rectTransform, x + 84f, y, 28f, 24f);
            }

            var trk = ControlRoomUIFactory.createBorderedBox(parent, "Trk_" + prefix + suffix, ControlRoomTheme.BgDarkPane, ControlRoomTheme.TextSecondary, 1.5f);
            ControlRoomUIFactory.setTopLeftRect(trk, x, y + 26f, 168f, 20f);
            var fill = ControlRoomUIFactory.createPanel(trk.Find("Inner"), "Fill", new Color(0.86f, 0.88f, 0.90f, 1f));
            fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0.8f, 1f);
            ControlRoomUIFactory.setOffsets(fill, 0f, 0f, 0f, 0f);

            pctLbl = ControlRoomUIFactory.createLabel(parent, "P_" + prefix + suffix, "0.00 %", 16.5f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(pctLbl.rectTransform, x, y + 48f, 140f, 24f);
            return fill;
        }

        // =====================================================================
        // 우측 Active / Completed 카드 패널 (Capture[16..49])
        // =====================================================================
        private void buildRightActivePane(RectTransform parent)
        {
            var pane = ControlRoomUIFactory.createPanel(parent, "RightActivePane", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(pane, 1592f, 46f, 328f, ControlRoomTheme.ContentAreaHeight - 46f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(parent, "DivR", ControlRoomTheme.BorderSubtle), 1590.5f, 46f, 1.5f, ControlRoomTheme.ContentAreaHeight - 46f);

            // 카드 1: Unit06 : C. Power Reduction
            var procTip = ControlRoomUIFactory.createE2Tooltip(pane, "ProcCardTip", "C. Power Reduction", 160f, 94f, 154f, 28f);
            ProcActiveCardWidget = ControlRoomUIFactory.createE2Control(
                pane, "Card_PowerReduction",
                6f, 8f, 316f, 108f,
                ControlRoomTheme.BgDarkPane, ControlRoomTheme.BgButton,
                null,
                () => mModel.CaptureStep == CaptureSequenceStep.Step13_PmRunningCardView,
                onClickProcActiveCard,
                null, 14f, null, TextAlignmentOptions.Center, procTip, true, ControlRoomTheme.HighlightBlue);
            mProcActiveCard = ProcActiveCardWidget.GetComponent<RectTransform>();
            var pIn = mProcActiveCard.Find("Inner");
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(pIn, "Title", "Unit06 : C. Power Reduction", 15f, ControlRoomTheme.TextPrimary).rectTransform, 10f, 6f, 290f, 24f);
            var pBar = ControlRoomUIFactory.createBorderedBox(pIn, "PBar", ControlRoomTheme.BgDarkest, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(pBar, 10f, 34f, 292f, 14f);
            mProcCardProgFill = ControlRoomUIFactory.createPanel(pBar.Find("Inner"), "Fill", ControlRoomTheme.HighlightBlue);
            mProcCardProgFill.anchorMin = Vector2.zero; mProcCardProgFill.anchorMax = new Vector2(0f, 1f);
            mProcCardProgTxt = ControlRoomUIFactory.createLabel(pIn, "Stat", "Status: <color=#3EE890>Active</color>  Progress: 0.0 %", 14.5f, ControlRoomTheme.HighlightBlue, TextAlignmentOptions.MidlineLeft);
            ControlRoomUIFactory.setTopLeftRect(mProcCardProgTxt.rectTransform, 10f, 74f, 250f, 24f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(pIn, "Ico", Color.white, mSprites.icoBookBlue), 278f, 72f, 26f, 26f);
            mProcActiveCard.gameObject.SetActive(false);

            // 카드 1-B: Unit06 : B. Power Ascension (80% -> 100% 상승 절차서 카드)
            AscProcActiveCardWidget = ControlRoomUIFactory.createE2Control(
                pane, "Card_PowerAscension",
                6f, 8f, 316f, 108f,
                ControlRoomTheme.BgDarkPane, ControlRoomTheme.BgButton,
                null,
                () => false,
                onClickAscProcActiveCard,
                null, 14f, null, TextAlignmentOptions.Center, null, true, ControlRoomTheme.HighlightBlue);
            mAscProcActiveCard = AscProcActiveCardWidget.GetComponent<RectTransform>();
            var aIn = mAscProcActiveCard.Find("Inner");
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(aIn, "Title", "Unit06 : B. Power Ascension", 15f, ControlRoomTheme.TextPrimary).rectTransform, 10f, 6f, 290f, 24f);
            var aBar = ControlRoomUIFactory.createBorderedBox(aIn, "ABar", ControlRoomTheme.BgDarkest, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(aBar, 10f, 34f, 292f, 14f);
            mAscProcCardProgFill = ControlRoomUIFactory.createPanel(aBar.Find("Inner"), "Fill", ControlRoomTheme.HighlightBlue);
            mAscProcCardProgFill.anchorMin = Vector2.zero; mAscProcCardProgFill.anchorMax = new Vector2(0f, 1f);
            mAscProcCardProgTxt = ControlRoomUIFactory.createLabel(aIn, "Stat", "Status: <color=#3EE890>Active</color>  Progress: 0.0 %", 14.5f, ControlRoomTheme.HighlightBlue, TextAlignmentOptions.MidlineLeft);
            ControlRoomUIFactory.setTopLeftRect(mAscProcCardProgTxt.rectTransform, 10f, 74f, 250f, 24f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(aIn, "Ico", Color.white, mSprites.icoBookBlue), 278f, 72f, 26f, 26f);
            mAscProcActiveCard.gameObject.SetActive(false);

            // 카드 2: Unit06 : Power Maneuvering (Execute 후 C. Power Reduction 복귀 상태에서는 언제든 클릭해 2번 UI 복귀 가능)
            PmActiveCardWidget = ControlRoomUIFactory.createE2Control(
                pane, "Card_PowerManeuver",
                6f, 124f, 316f, 128f,
                ControlRoomTheme.BgDarkPane, ControlRoomTheme.BgButton,
                null,
                () => mModel.CaptureStep >= CaptureSequenceStep.Step14_ReturnedToProcCard
                      && mModel.CaptureStep <= CaptureSequenceStep.Step16_Step8Checked,
                onClickPmActiveCard,
                null, 14f, null, TextAlignmentOptions.Center, null, true, ControlRoomTheme.HighlightBlue);
            mPmActiveCard = PmActiveCardWidget.GetComponent<RectTransform>();
            var mIn = mPmActiveCard.Find("Inner");
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mIn, "Title", "Unit06 : Power Maneuvering", 15f, ControlRoomTheme.TextPrimary).rectTransform, 10f, 6f, 290f, 24f);
            var mBar = ControlRoomUIFactory.createBorderedBox(mIn, "MBar", ControlRoomTheme.BgDarkest, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(mBar, 10f, 34f, 292f, 14f);
            mPmCardProgFill = ControlRoomUIFactory.createPanel(mBar.Find("Inner"), "Fill", ControlRoomTheme.HighlightBlue);
            mPmCardProgFill.anchorMin = Vector2.zero; mPmCardProgFill.anchorMax = new Vector2(0.07f, 1f);
            mPmCardStatusTxt = ControlRoomUIFactory.createLabel(mIn, "GreenMsg", "Begin Ramp", 13.5f, ControlRoomTheme.TabActiveGreen, TextAlignmentOptions.MidlineLeft);
            ControlRoomUIFactory.setTopLeftRect(mPmCardStatusTxt.rectTransform, 10f, 68f, 292f, 24f);
            mPmCardProgTxt = ControlRoomUIFactory.createLabel(mIn, "Stat", "Status: <color=#3EE890>Active</color>  Progress: 7.1 %", 14.5f, ControlRoomTheme.HighlightBlue, TextAlignmentOptions.MidlineLeft);
            ControlRoomUIFactory.setTopLeftRect(mPmCardProgTxt.rectTransform, 10f, 94f, 250f, 24f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(mIn, "Ico", Color.white, mSprites.icoGearListBlue), 278f, 92f, 26f, 26f);
            mPmActiveCard.gameObject.SetActive(false);
        }

        // =====================================================================
        // 의도된 단계 전이 핸들러들 (Capture[00 -> 49] 정확히 1:1 매칭)
        // =====================================================================
        private void onClickPlaySimulator()
        {
            if (mModel.CaptureStep != CaptureSequenceStep.Step00_InstructorPaused) return;
            mModel.startSimulatorRun();
        }

        private void onClickBottomProcessLibrary()
        {
            if (mModel.CaptureStep != CaptureSequenceStep.Step01_InstructorRunning) return;
            mModel.setCaptureStep(CaptureSequenceStep.Step02_ProcessLibraryEmpty);
            navigateToScreen(MonitorScreenId.ProcessLibrary);
        }

        private void onClickIopCategory()
        {
            if (mModel.CaptureStep != CaptureSequenceStep.Step02_ProcessLibraryEmpty) return;
            mModel.setCaptureStep(CaptureSequenceStep.Step03_IopExpanded);
        }

        private void onClickPowerOperation()
        {
            if (mModel.CaptureStep != CaptureSequenceStep.Step03_IopExpanded) return;
            mModel.setCaptureStep(CaptureSequenceStep.Step04_PowerOpExpanded);
        }

        private void onClickPowerReductionDoc()
        {
            if (mModel.CaptureStep != CaptureSequenceStep.Step04_PowerOpExpanded) return;
            mModel.setCaptureStep(CaptureSequenceStep.Step05_PowerReductionPreview);
            mGuide.notifyPowerReductionOpened();
        }

        private void onClickStartProcedure()
        {
            if (mModel.CaptureStep != CaptureSequenceStep.Step05_PowerReductionPreview) return;
            mModel.setCaptureStep(CaptureSequenceStep.Step06_PowerReductionActive);
        }

        private void onClickStep1Check()
        {
            if (mModel.CaptureStep != CaptureSequenceStep.Step05_PowerReductionPreview
                && mModel.CaptureStep != CaptureSequenceStep.Step06_PowerReductionActive) return;
            mModel.setCaptureStep(CaptureSequenceStep.Step07_Step1Checked);
        }

        private void onClickStep3Check()
        {
            if (mModel.CaptureStep != CaptureSequenceStep.Step05_PowerReductionPreview
                && mModel.CaptureStep != CaptureSequenceStep.Step06_PowerReductionActive
                && mModel.CaptureStep != CaptureSequenceStep.Step07_Step1Checked) return;
            mModel.setCaptureStep(CaptureSequenceStep.Step08_Step3MenuOpen);
        }

        private void onClickStep3MenuNa()
        {
            if (mModel.CaptureStep != CaptureSequenceStep.Step08_Step3MenuOpen) return;
            mModel.setCaptureStep(CaptureSequenceStep.Step09_Step3NaCollapsed);
        }

        private void onClickLinkPowerManeuvering()
        {
            if (mModel.CaptureStep == CaptureSequenceStep.Step09_Step3NaCollapsed)
            {
                mPleaseWaitTimer = 0.35f;
                if (mPmChartPleaseWait != null) mPmChartPleaseWait.gameObject.SetActive(true);
                mModel.setCaptureStep(CaptureSequenceStep.Step10_PowerManeuveringSetup);
                return;
            }

            if (mModel.CaptureStep >= CaptureSequenceStep.Step14_ReturnedToProcCard
                && mModel.CaptureStep <= CaptureSequenceStep.Step16_Step8Checked)
            {
                mModel.setCaptureStep(CaptureSequenceStep.Step17_ReturnedToPmRamp80);
            }
        }

        private void onClickTargetPowerBox()
        {
            onClickPowerManeuverParams();
        }

        private void onClickRampRateBox()
        {
            onClickPowerManeuverParams();
        }

        /// <summary>Target Power 및 Ramp Rate 두 값을 현재 시나리오 단계에 맞춰 동시 입력합니다(감발: 80/300, 상승: 100/20).</summary>
        private void onClickPowerManeuverParams()
        {
            if (mModel.CaptureStep == CaptureSequenceStep.Step10_PowerManeuveringSetup)
            {
                mModel.setCaptureStep(CaptureSequenceStep.Step11_PmParamsEntered);
            }
            else if (mModel.CaptureStep == CaptureSequenceStep.Step22_AscPmSetup)
            {
                mModel.setCaptureStep(CaptureSequenceStep.Step23_AscPmParamsEntered);
            }
        }

        private void onClickValidatePm()
        {
            if (mModel.CaptureStep == CaptureSequenceStep.Step11_PmParamsEntered)
            {
                mModel.setCaptureStep(CaptureSequenceStep.Step12_PmValidated);
            }
            else if (mModel.CaptureStep == CaptureSequenceStep.Step23_AscPmParamsEntered)
            {
                mModel.setCaptureStep(CaptureSequenceStep.Step24_AscPmValidated);
            }
        }

        private void onClickExecutePm()
        {
            if (mModel.CaptureStep == CaptureSequenceStep.Step12_PmValidated)
            {
                mModel.setCaptureStep(CaptureSequenceStep.Step13_PmRunningCardView);
            }
            else if (mModel.CaptureStep == CaptureSequenceStep.Step24_AscPmValidated)
            {
                mModel.setCaptureStep(CaptureSequenceStep.Step25_AscPmRamp100);
            }
        }

        private void onClickProceduresSubTab()
        {
            if (mModel.CaptureStep != CaptureSequenceStep.Step18_Ramp80Completed) return;
            mModel.setCaptureStep(CaptureSequenceStep.Step19_ProceduresTabForAscension);
        }

        private void onClickPowerAscensionDoc()
        {
            if (mModel.CaptureStep != CaptureSequenceStep.Step19_ProceduresTabForAscension) return;
            mModel.setCaptureStep(CaptureSequenceStep.Step20_PowerAscensionPreview);
        }

        private void onClickAscStartProcedure()
        {
            if (mModel.CaptureStep != CaptureSequenceStep.Step20_PowerAscensionPreview) return;
            mModel.setCaptureStep(CaptureSequenceStep.Step21_PowerAscensionActive);
        }

        private void onClickAscStep1Check()
        {
            if (mModel.CaptureStep != CaptureSequenceStep.Step21_PowerAscensionActive) return;
            mModel.toggleProcedureStep("ASC_1");
        }

        private void onClickAscLinkPowerManeuvering()
        {
            if (mModel.CaptureStep != CaptureSequenceStep.Step21_PowerAscensionActive) return;
            mPleaseWaitTimer = 0.35f;
            if (mPmChartPleaseWait != null) mPmChartPleaseWait.gameObject.SetActive(true);
            mModel.setCaptureStep(CaptureSequenceStep.Step22_AscPmSetup);
        }

        private void onClickAscProcActiveCard()
        {
            // 시나리오 1 상승 Active 카드는 표시 전용(호버 반응 유지)
        }

        private void onClickProcActiveCard()
        {
            if (mModel.CaptureStep != CaptureSequenceStep.Step13_PmRunningCardView) return;
            mModel.setCaptureStep(CaptureSequenceStep.Step14_ReturnedToProcCard);
        }

        private void onClickStep6Check()
        {
            if (mModel.CaptureStep != CaptureSequenceStep.Step14_ReturnedToProcCard) return;
            mModel.setCaptureStep(CaptureSequenceStep.Step15_Step6Checked);
        }

        private void onClickStep8Check()
        {
            if (mModel.CaptureStep != CaptureSequenceStep.Step15_Step6Checked) return;
            mModel.setCaptureStep(CaptureSequenceStep.Step16_Step8Checked);
            if (mProcDocScroll != null) mProcDocScroll.verticalNormalizedPosition = 0.42f;
        }

        private void onClickPmActiveCard()
        {
            if (mModel.CaptureStep < CaptureSequenceStep.Step14_ReturnedToProcCard
                || mModel.CaptureStep > CaptureSequenceStep.Step16_Step8Checked) return;
            mModel.setCaptureStep(CaptureSequenceStep.Step17_ReturnedToPmRamp80);
        }

        /// <summary>현재 시나리오 단계에서 3D 가이드 화살표가 가리켜야 할 UI 버튼 RectTransform을 반환합니다(안내 종료 시 null).</summary>
        public RectTransform getCurrentScenarioGuideTargetRect()
        {
            try
            {
                switch (mModel.CaptureStep)
                {
                    case CaptureSequenceStep.Step00_InstructorPaused:
                        return InstPlayButtonWidget != null ? InstPlayButtonWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step01_InstructorRunning:
                        return mChrome.ProcessLibraryNavWidget != null ? mChrome.ProcessLibraryNavWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step02_ProcessLibraryEmpty:
                        return TreeIopWidget != null ? TreeIopWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step03_IopExpanded:
                        return TreePowerOpWidget != null ? TreePowerOpWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step04_PowerOpExpanded:
                        return TreePowerRedWidget != null ? TreePowerRedWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step05_PowerReductionPreview:
                        return StartProcWidget != null ? StartProcWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step06_PowerReductionActive:
                        return Step1CheckWidget != null ? Step1CheckWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step07_Step1Checked:
                        return Step3CheckWidget != null ? Step3CheckWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step08_Step3MenuOpen:
                        return Step3NaMenuWidget != null ? Step3NaMenuWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step09_Step3NaCollapsed:
                        return Step6LinkWidget != null ? Step6LinkWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step10_PowerManeuveringSetup:
                        return PmTargetFieldWidget != null ? PmTargetFieldWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step11_PmParamsEntered:
                        return PmValidateWidget != null ? PmValidateWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step12_PmValidated:
                        return PmExecuteWidget != null ? PmExecuteWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step13_PmRunningCardView:
                        return ProcActiveCardWidget != null ? ProcActiveCardWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step14_ReturnedToProcCard:
                        return Step6CheckWidget != null ? Step6CheckWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step15_Step6Checked:
                        return Step8CheckWidget != null ? Step8CheckWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step16_Step8Checked:
                        return PmActiveCardWidget != null ? PmActiveCardWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step18_Ramp80Completed:
                        return SubTabProcWidget != null ? SubTabProcWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step19_ProceduresTabForAscension:
                        return TreePowerAscWidget != null ? TreePowerAscWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step20_PowerAscensionPreview:
                        return AscStartProcWidget != null ? AscStartProcWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step21_PowerAscensionActive:
                        return AscStep5LinkWidget != null ? AscStep5LinkWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step22_AscPmSetup:
                        return PmTargetFieldWidget != null ? PmTargetFieldWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step23_AscPmParamsEntered:
                        return PmValidateWidget != null ? PmValidateWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step24_AscPmValidated:
                        return PmExecuteWidget != null ? PmExecuteWidget.GetComponent<RectTransform>() : null;
                    case CaptureSequenceStep.Step25_AscPmRamp100:
                    case CaptureSequenceStep.Step26_Scenario1Completed:
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MainMonitorController] getCurrentScenarioGuideTargetRect 오류: {ex}");
                return null;
            }
        }

        // =====================================================================
        // 전체 UI 상태 동기화
        // =====================================================================
        public void refreshUI()
        {
            try
            {
                var step = mModel.CaptureStep;
                string screenTitle = step >= CaptureSequenceStep.Step02_ProcessLibraryEmpty ? "Process Library" : "Instructor Station";
                CurrentScreenId = step >= CaptureSequenceStep.Step02_ProcessLibraryEmpty ? MonitorScreenId.ProcessLibrary : MonitorScreenId.InstructorStation;

                mChrome.updateHeaderAndClock(
                    mModel.ActiveUnitCode,
                    "Special",
                    screenTitle,
                    mModel.ActiveAlarmCount,
                    mModel.ActiveCautionCount,
                    mModel.NoticeBadgeCount,
                    null,
                    mSprites);

                bool inProcLib = step >= CaptureSequenceStep.Step02_ProcessLibraryEmpty;
                mInstructorRoot.gameObject.SetActive(!inProcLib);
                mProcessLibraryRoot.gameObject.SetActive(inProcLib);

                // Instructor Station 갱신
                mInstStatusTxt.text = $"Simulator Status:  {(mModel.IsSimulatorPaused ? "Paused" : "Running")}";
                mInstRunTimeTxt.text = $"Run Time:   {mModel.getFormattedRunTime()}";

                // Process Library 트리/탭 전환
                mInAutomationMode = (step >= CaptureSequenceStep.Step10_PowerManeuveringSetup && step <= CaptureSequenceStep.Step13_PmRunningCardView)
                                    || step == CaptureSequenceStep.Step17_ReturnedToPmRamp80
                                    || step == CaptureSequenceStep.Step18_Ramp80Completed
                                    || (step >= CaptureSequenceStep.Step22_AscPmSetup && step <= CaptureSequenceStep.Step26_Scenario1Completed);

                mSubTabProcTxt.color = mInAutomationMode ? ControlRoomTheme.TextPrimary : ControlRoomTheme.TabActiveGreen;
                mSubTabAutoTxt.color = mInAutomationMode ? ControlRoomTheme.TabActiveGreen : ControlRoomTheme.TextPrimary;
                mProcTreeContainer.gameObject.SetActive(!mInAutomationMode);
                mAutoTreeContainer.gameObject.SetActive(mInAutomationMode);

                mTreeSubIop.gameObject.SetActive(step >= CaptureSequenceStep.Step03_IopExpanded);
                mTreeSubPowerOp.gameObject.SetActive(step >= CaptureSequenceStep.Step04_PowerOpExpanded);

                // 중앙 작업 영역 5개 뷰 중 하나 표시
                bool showEmpty = (step >= CaptureSequenceStep.Step02_ProcessLibraryEmpty && step <= CaptureSequenceStep.Step04_PowerOpExpanded)
                                 || step == CaptureSequenceStep.Step19_ProceduresTabForAscension;
                bool showDoc = (step >= CaptureSequenceStep.Step05_PowerReductionPreview && step <= CaptureSequenceStep.Step09_Step3NaCollapsed)
                               || (step >= CaptureSequenceStep.Step14_ReturnedToProcCard && step <= CaptureSequenceStep.Step16_Step8Checked);
                bool showAscDoc = step == CaptureSequenceStep.Step20_PowerAscensionPreview
                                  || step == CaptureSequenceStep.Step21_PowerAscensionActive;
                bool showPmSetup = (step >= CaptureSequenceStep.Step10_PowerManeuveringSetup && step <= CaptureSequenceStep.Step12_PmValidated)
                                   || (step >= CaptureSequenceStep.Step22_AscPmSetup && step <= CaptureSequenceStep.Step24_AscPmValidated);
                bool showPmRun = step == CaptureSequenceStep.Step13_PmRunningCardView
                                 || step == CaptureSequenceStep.Step17_ReturnedToPmRamp80
                                 || step == CaptureSequenceStep.Step18_Ramp80Completed
                                 || step == CaptureSequenceStep.Step25_AscPmRamp100
                                 || step == CaptureSequenceStep.Step26_Scenario1Completed;

                mCenterEmptyPane.gameObject.SetActive(showEmpty);
                mProcDocRoot.gameObject.SetActive(showDoc);
                mAscDocRoot.gameObject.SetActive(showAscDoc);
                mPmSetupRoot.gameObject.SetActive(showPmSetup);
                mPmRunRoot.gameObject.SetActive(showPmRun);

                // 절차서 문서 모드 & 체크 상태 (C. Power Reduction)
                bool isProcActiveMode = step >= CaptureSequenceStep.Step06_PowerReductionActive;
                mPreviewFooter.gameObject.SetActive(!isProcActiveMode);
                mActiveFooter.gameObject.SetActive(isProcActiveMode);
                foreach (var dec in mActiveOnlyDecorations) if (dec != null) dec.gameObject.SetActive(isProcActiveMode);
                Step1CheckWidget.gameObject.SetActive(true);
                Step3CheckWidget.gameObject.SetActive(true);
                Step6CheckWidget.gameObject.SetActive(true);
                Step8CheckWidget.gameObject.SetActive(true);

                // B. Power Ascension 문서 모드 & 1번 선택적 체크 상태
                bool isAscActiveMode = step >= CaptureSequenceStep.Step21_PowerAscensionActive;
                if (mAscPreviewFooter != null) mAscPreviewFooter.gameObject.SetActive(!isAscActiveMode);
                if (mAscActiveFooter != null) mAscActiveFooter.gameObject.SetActive(isAscActiveMode);
                foreach (var dec in mAscActiveOnlyDecorations) if (dec != null) dec.gameObject.SetActive(isAscActiveMode);
                applyCheckStepVisual(mAscStep1BorderImg, mAscStep1CheckImg, mAscStep1Badge1, mModel.isStepChecked("ASC_1"));

                bool c1 = step >= CaptureSequenceStep.Step07_Step1Checked;
                applyCheckStepVisual(mStep1BorderImg, mStep1CheckImg, mStep1Badge1, c1);

                bool s3Na = step >= CaptureSequenceStep.Step09_Step3NaCollapsed;
                mStep3FullRow.gameObject.SetActive(!s3Na);
                mStep3NaRow.gameObject.SetActive(s3Na);
                mStep3ContextMenu.gameObject.SetActive(step == CaptureSequenceStep.Step08_Step3MenuOpen);

                bool c6 = step >= CaptureSequenceStep.Step15_Step6Checked;
                applyCheckStepVisual(mStep6BorderImg, mStep6CheckImg, mStep6Badge1, c6);

                bool c8 = step >= CaptureSequenceStep.Step16_Step8Checked;
                applyCheckStepVisual(mStep8BorderImg, mStep8CheckImg, mStep8Badge1, c8);
                if (mProcDocScroll != null && step == CaptureSequenceStep.Step16_Step8Checked)
                {
                    mProcDocScroll.verticalNormalizedPosition = 0.42f;
                }

                // Power Maneuvering Setup 갱신
                mPmHdrTHot.text = $"{mModel.RcsTHotF:F0} °F";
                mPmHdrTCold.text = $"{mModel.RcsTColdF:F0} °F";
                mPmHdrTAve.text = $"{mModel.RcsTAveF:F0} °F";
                mPmHdrPzrP.text = $"{mModel.PzrPressurePsia:F0} psia";
                mPmHdrCorePwr.text = $"{mModel.CoreThermalPowerPct:F1} %";
                mPmHdrNiPwr.text = $"{mModel.CoreNiPowerPct:F1} %";
                mPmHdrMsFlow.text = $"{mModel.MsFlowLbS:F0} lb/s";
                mPmHdrFwFlow.text = $"{mModel.FwFlowLbS:F0} lb/s";
                mPmHdrMsP.text = $"{mModel.MsHeaderPressurePsia:F0} psia";

                Color setupCrossCol = mModel.ControlRodSteps < 200 ? ControlRoomTheme.RodYellow : ControlRoomTheme.RodCyan;
                for (int i = 0; i < mPmSetupCrossRods.Count; i++)
                {
                    var rod = mPmSetupCrossRods[i];
                    if (rod.bg != null) rod.bg.color = setupCrossCol;
                    if (rod.lbl != null)
                    {
                        int rodVal = rod.isInner ? mModel.ControlRodInnerSteps : mModel.ControlRodOuterCrossSteps;
                        rod.lbl.text = rodVal.ToString();
                    }
                }

                mPmLegTHotV.text = $"{mModel.RcsTHotF:F0}";
                mPmLegTColdV.text = $"{mModel.RcsTColdF:F0}";
                mPmLegTAveV.text = $"{mModel.RcsTAveF:F0}";

                mPmSetupChart.updateSeries(0, mModel.THotHistory, ControlRoomTheme.StatusCaution, 2.5f);
                mPmSetupChart.updateSeries(1, mModel.TAveHistory, ControlRoomTheme.StatusAlarm, 2.5f);
                mPmSetupChart.updateSeries(2, mModel.TColdHistory, ControlRoomTheme.TelemetryCyan, 2.5f);

                mPmTargetTxt.text = $"{mModel.PendingTargetPowerPct:F0}";
                mPmRateTxt.text = $"{mModel.PendingRampRatePctHr:F0}";
                float initPct = mModel.CoreThermalPowerPct;
                float initMw = initPct * 1.6f;
                float tgtMw = mModel.PendingTargetPowerPct * 1.6f;
                float rtMw = mModel.PendingRampRatePctHr * 1.6f;
                mPmSummaryTxt.text = $"Initial Power:  {initPct:F2}% ( {initMw:F1}MW )\nTarget Power: {mModel.PendingTargetPowerPct:F0}% ( {tgtMw:F0}MW )\nRate: {mModel.PendingRampRatePctHr:F0}%/Hr ( {rtMw:F0}MW/Hr )\nDuration:  {mModel.PowerManeuverDurationText}";
                mPmReadyTxt.gameObject.SetActive(step == CaptureSequenceStep.Step12_PmValidated || step == CaptureSequenceStep.Step24_AscPmValidated);

                // Power Maneuver Running 대시보드 갱신
                mRunReqPwr.text = $"{mModel.RequestedPowerPct:F0} %";
                mRunCurPwr.text = $"{mModel.CoreThermalPowerPct:F1} %";
                mRunCoreP.text = $"{mModel.PzrPressurePsia:F0} psia";
                mRunTAvg.text = $"{mModel.RcsTAveF:F0} °F";
                mRunFwFlow.text = $"{mModel.FwFlowLbS:F0} lb/s";
                mRunMsFlow.text = $"{mModel.MsFlowLbS:F0} lb/s";
                mRunTurbPwr.text = $"{mModel.TurbinePowerPct:F1} %";

                updateBar(mRunGovFill, mRunGovPct, mModel.GovernorPct, false);
                updateBar(mRunPumpAFill, mRunPumpAPct, mModel.FwPumpASpeedPct, false);
                updateBar(mRunPumpBFill, mRunPumpBPct, mModel.FwPumpBSpeedPct, false);
                updateBar(mRunPumpCFill, mRunPumpCPct, mModel.FwPumpCSpeedPct, true);

                Color crossCol = mModel.ControlRodSteps < 200 ? ControlRoomTheme.RodYellow : ControlRoomTheme.RodCyan;
                foreach (var cell in mRunCoreCells)
                {
                    if (!string.IsNullOrEmpty(cell.lbl.text))
                    {
                        cell.bg.color = cell.isCrossRegRod ? crossCol : ControlRoomTheme.RodCyan;
                        if (cell.isCrossRegRod)
                        {
                            int rodVal = cell.isInnerCross ? mModel.ControlRodInnerSteps : mModel.ControlRodOuterCrossSteps;
                            cell.lbl.text = rodVal.ToString();
                        }
                        else
                        {
                            cell.lbl.text = mModel.ControlRodOuterSteps.ToString();
                        }
                    }
                }

                bool showRunAxes = mModel.IsRunChartAxesVisible;
                if (mRunChartAxesGroup != null) mRunChartAxesGroup.gameObject.SetActive(showRunAxes);
                for (int i = 0; i < mRunLegSwatches.Count; i++)
                {
                    if (mRunLegSwatches[i] != null) mRunLegSwatches[i].SetActive(showRunAxes);
                }

                // Run 차트 X축 타임스탬프 (최근 5분 창)
                float endSec = mModel.RunChartDisplayClockSeconds;
                float startSec = endSec - 300f;
                for (int i = 0; i < mRunChartTimeLabels.Count; i++)
                {
                    int sec = Mathf.Max(0, Mathf.FloorToInt(Mathf.Lerp(startSec, endSec, i / 8f))) % 86400;
                    int hh = sec / 3600;
                    int mm = (sec % 3600) / 60;
                    int ss = sec % 60;
                    mRunChartTimeLabels[i].text = $"{hh:00}:{mm:00}:{ss:00}";
                }

                float visRatio = Mathf.Clamp01(mModel.RunChartVisibleRatio);
                if (!showRunAxes || visRatio <= 0.001f)
                {
                    mRunChart.clearAllSeries();
                }
                else
                {
                    mRunTAveScaled.Clear();
                    for (int i = 0; i < mModel.TAveHistory.Count; i++)
                    {
                        mRunTAveScaled.Add(Mathf.InverseLerp(500f, 600f, mModel.TAveHistory[i]) * 130f);
                    }
                    mRunPzrScaled.Clear();
                    for (int i = 0; i < mModel.PzrPressHistory.Count; i++)
                    {
                        mRunPzrScaled.Add(Mathf.InverseLerp(1500f, 2200f, mModel.PzrPressHistory[i]) * 130f);
                    }

                    float chartStartX = 1f - visRatio;
                    mRunChart.updateSeries(0, mModel.PowerHistory, ControlRoomTheme.StatusCaution, 2.5f, chartStartX, 1f);
                    mRunChart.updateSeries(1, mRunTAveScaled, ControlRoomTheme.TelemetryCyan, 2.3f, chartStartX, 1f);
                    mRunChart.updateSeries(2, mRunPzrScaled, ControlRoomTheme.StatusAlarm, 2.3f, chartStartX, 1f);
                }
                mRunLegPwrV.text = $"{mModel.CoreThermalPowerPct:F1}";
                mRunLegTAvgV.text = $"{mModel.RcsTAveF:F0}";
                mRunLegPzrPV.text = $"{mModel.PzrPressurePsia:F0}";

                // 우측 Active 카드 표시·진행률·순서 (C. Power Reduction / B. Power Ascension / Power Maneuvering)
                bool hasRedCard = isProcActiveMode && step < CaptureSequenceStep.Step21_PowerAscensionActive;
                bool hasAscCard = step >= CaptureSequenceStep.Step21_PowerAscensionActive;
                mProcActiveCard.gameObject.SetActive(hasRedCard);
                if (mAscProcActiveCard != null) mAscProcActiveCard.gameObject.SetActive(hasAscCard);
                bool hasPmCard = step >= CaptureSequenceStep.Step13_PmRunningCardView;
                mPmActiveCard.gameObject.SetActive(hasPmCard);

                float procPct = c8 ? 57.1f : (c6 ? 42.9f : (s3Na ? 28.6f : (c1 ? 14.3f : 0.0f)));
                mProcCardProgFill.anchorMax = new Vector2(Mathf.Clamp01(procPct / 100f), 1f);
                mProcCardProgTxt.text = $"Status: <color=#3EE890>Active</color>  Progress: {procPct:F1} %";

                if (hasAscCard && mAscProcActiveCard != null)
                {
                    bool ascDone = step >= CaptureSequenceStep.Step26_Scenario1Completed || mModel.HasReached100PercentAfterReduction;
                    float ascPct = ascDone ? 100.0f : (step >= CaptureSequenceStep.Step25_AscPmRamp100 ? 55.6f : (mModel.isStepChecked("ASC_1") ? 11.1f : 0.0f));
                    mAscProcCardProgFill.anchorMax = new Vector2(Mathf.Clamp01(ascPct / 100f), 1f);
                    string ascStatus = ascDone ? "Completed" : "Active";
                    mAscProcCardProgTxt.text = $"Status: <color=#3EE890>{ascStatus}</color>  Progress: {ascPct:F1} %";
                }

                if (hasPmCard)
                {
                    if (hasAscCard && mAscProcActiveCard != null)
                    {
                        bool pmOnTop = step >= CaptureSequenceStep.Step25_AscPmRamp100;
                        ControlRoomUIFactory.setTopLeftRect(mPmActiveCard, 6f, pmOnTop ? 8f : 124f, 316f, 128f);
                        ControlRoomUIFactory.setTopLeftRect(mAscProcActiveCard, 6f, pmOnTop ? 144f : 8f, 316f, 108f);
                    }
                    else
                    {
                        bool pmOnTop = step >= CaptureSequenceStep.Step15_Step6Checked;
                        ControlRoomUIFactory.setTopLeftRect(mPmActiveCard, 6f, pmOnTop ? 8f : 124f, 316f, 128f);
                        ControlRoomUIFactory.setTopLeftRect(mProcActiveCard, 6f, pmOnTop ? 144f : 8f, 316f, 108f);
                    }

                    float pmPct = Mathf.Max(7.1f, mModel.PowerManeuverProgressPct);
                    mPmCardProgFill.anchorMax = new Vector2(Mathf.Clamp01(pmPct / 100f), 1f);
                    mPmCardStatusTxt.text = string.IsNullOrEmpty(mModel.PowerManeuverStatusText) ? "Begin Ramp" : mModel.PowerManeuverStatusText;
                    string pmStatusState = (step >= CaptureSequenceStep.Step26_Scenario1Completed || mModel.HasReached100PercentAfterReduction) ? "Completed" : "Active";
                    mPmCardProgTxt.text = $"Status: <color=#3EE890>{pmStatusState}</color>  Progress: {pmPct:F1} %";
                }
                else
                {
                    ControlRoomUIFactory.setTopLeftRect(mProcActiveCard, 6f, 8f, 316f, 108f);
                    if (mAscProcActiveCard != null) ControlRoomUIFactory.setTopLeftRect(mAscProcActiveCard, 6f, 8f, 316f, 108f);
                }

                // 모든 인터랙티브 버튼의 호버/기본 색상 상태 재동기화
                mChrome.refreshHoverWidgetColors();
                SubTabProcWidget?.refreshVisualColor();
                InstPlayButtonWidget.refreshVisualColor();
                TreeIopWidget.refreshVisualColor();
                TreePowerOpWidget.refreshVisualColor();
                TreePowerAscWidget?.refreshVisualColor();
                TreePowerRedWidget.refreshVisualColor();
                StartProcWidget.refreshVisualColor();
                Step1CheckWidget.refreshVisualColor();
                Step3CheckWidget.refreshVisualColor();
                Step3NaMenuWidget.refreshVisualColor();
                Step6LinkWidget.refreshVisualColor();
                Step6CheckWidget.refreshVisualColor();
                Step8CheckWidget.refreshVisualColor();
                AscStartProcWidget?.refreshVisualColor();
                AscStep1CheckWidget?.refreshVisualColor();
                AscStep5LinkWidget?.refreshVisualColor();
                PmTargetFieldWidget.refreshVisualColor();
                PmRateFieldWidget.refreshVisualColor();
                PmValidateWidget.refreshVisualColor();
                PmExecuteWidget.refreshVisualColor();
                ProcActiveCardWidget.refreshVisualColor();
                AscProcActiveCardWidget?.refreshVisualColor();
                PmActiveCardWidget.refreshVisualColor();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MainMonitorController] refreshUI 오류: {ex}");
            }
        }

        private void applyCheckStepVisual(Image borderImg, Image checkMarkImg, RectTransform badge1Rt, bool isChecked)
        {
            if (borderImg != null)
            {
                borderImg.color = isChecked ? ControlRoomTheme.HighlightBlue : ControlRoomTheme.CheckboxBorder;
            }
            if (checkMarkImg != null)
            {
                checkMarkImg.sprite = mSprites.icoCheckStep;
                checkMarkImg.color = Color.white;
                checkMarkImg.gameObject.SetActive(isChecked);
            }
            if (badge1Rt != null)
            {
                badge1Rt.gameObject.SetActive(isChecked);
            }
        }

        private void updateBar(RectTransform fillRt, TextMeshProUGUI pctLbl, float pct, bool isRedOff)
        {
            pctLbl.text = $"{pct:F2} %";
            pctLbl.color = isRedOff ? ControlRoomTheme.TelemetryGreen : ControlRoomTheme.TelemetryGreen;
            fillRt.anchorMax = new Vector2(Mathf.Clamp01(pct / 100f), 1f);
        }
    }
}
