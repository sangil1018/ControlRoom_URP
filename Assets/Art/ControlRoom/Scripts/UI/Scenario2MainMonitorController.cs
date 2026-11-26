using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ControlRoom.UI
{
    /// <summary>
    /// 시나리오 2 중앙 메인 모니터 컨트롤러입니다.
    /// 0.25초 단위 영상 분석(비상절차서_LOCA_1-2.mp4) 및 E2 교육자료(p.82·84·85·86·87)의
    /// 7개 계통/절차 화면, 상단 3단계 브레드크럼 드롭다운, 뒤로가기(<), Tag 토글, 경보창,
    /// 밸브/펌프 3줄 팝업, CVC Makeup(Boric Acid Only + Pump B), Isolation SHOW ALL, RCCW 탱크 확인을 100% 구현합니다.
    /// </summary>
    public class Scenario2MainMonitorController
    {
        private readonly Scenario2ControllerModel mModel;
        private readonly MonitorCommonChrome mChrome = new MonitorCommonChrome();
        private ControlRoomSpritePack mSprites;

        public MonitorCommonChrome Chrome => mChrome;

        // 7개 화면 루트
        private RectTransform mCeRoot;
        private RectTransform mModOverviewRoot;
        private RectTransform mCvcRecircRoot;
        private RectTransform mCvcHeatingRoot;
        private RectTransform mCvcDeminsRoot;
        private RectTransform mProcLibRoot;
        private RectTransform mRccwRoot;

        // 공용 우측 패널(Processes / Active+Completed + Details)
        private RectTransform mSharedRightPane;
        private TextMeshProUGUI mRightTopHdrTxt;
        private RectTransform mMakeupActiveCard;
        private RectTransform mMakeupCardProgFill;
        private TextMeshProUGUI mMakeupCardStatusTxt, mMakeupCardProgTxt;
        private RectTransform mDetDividerRt;
        private TextMeshProUGUI mDetHdrTxt;
        private RectTransform mDetailsStandardGroup, mDetailsRecircControlGroup;
        private TextMeshProUGUI mDetTagTxt, mDetDescTxt, mDetPosTxt, mDetValTxt, mDetStateTxt;
        private TextMeshProUGUI mDetRecircModeTxt;
        private Image mDetRecircLockImg;

        // 상단 드롭다운 & 경보창 오버레이
        private RectTransform mUnitDropdown;
        private RectTransform mCategoryDropdown;
        private RectTransform mPrimaryScreenDropdown;
        private RectTransform mPrimary06Group;
        private RectTransform mPrimary0AGroup;
        private RectTransform mCvcSubDropdown;
        private RectTransform mAlarmDropdown;
        private TextMeshProUGUI mAlarmRow0Txt, mAlarmRow1Txt, mAlarmRow2Txt;

        // 화면 1: CE (Containment Evacuation)
        private Image mCe0130Img, mCe0128Img;
        private RectTransform mCe0130Popup, mCe0128Popup;
        private RectTransform mCeTagOverlay;
        private TextMeshProUGUI mCeRt1011Txt, mCeCnvPressTxt, mCeLeakRateTxt, mCeSampleLevelTxt, mCeSampleCpsTxt, mCeFilterDpTxt;

        // 화면 2: Module Overview (FW/MS vs Isolation + SHOW ALL)
        private RectTransform mModFwmsPanel, mModIsoPanel;
        private RectTransform mIsoTagOverlay;
        private TextMeshProUGUI mModCnvPressTxt, mModPzrLevelTxt, mModPowerTxt, mModTaveTxt;
        private Image mIsoCvcISV329Img, mIsoCvcISV331Img;

        // 화면 3: CVC Recirc & Makeup
        private Image mRecircPumpAImg, mRecircPumpBImg, mRecircLockImg;
        private TextMeshProUGUI mRecircModeLbl;
        private RectTransform mPumpAPopup;
        private RectTransform mRecircTagOverlay;

        // 화면 4: CVC Heating & Spray
        private Image mMov0318Img, mAov0339Img;
        private RectTransform mMov0318Popup, mAov0339Popup;
        private RectTransform mHeatingTagOverlay;
        private TextMeshProUGUI mHeatSprayFlowTxt, mHeatInjFlowTxt, mHeatDischFlowTxt;

        // 화면 5: CVC Demins & Letdowns
        private TextMeshProUGUI mDeminsFlowTxt;
        private RectTransform mDeminsTagOverlay;

        // 화면 6: Process Library (Procedures <-> Automations -> CVC Makeup)
        private TextMeshProUGUI mProcTabTxt, mAutoTabTxt;
        private RectTransform mProcSearchRowRt, mProcTreePane, mAutoLeftPane;
        private RectTransform mAutoTreeRootPane, mCvcSubTreePane;
        private TextMeshProUGUI mAutoSearchInputTxt;
        private RectTransform mMakeupEmptyPane, mMakeupSetupPane, mMakeupRunPane;
        private Image mMakeupBoricDot, mMakeupBlendRadioDot;
        private TextMeshProUGUI mMakeupSetupPzrTxt, mMakeupSummaryTxt, mMakeupReadyTxt;
        private TextMeshProUGUI mMakeupRunCurLvlTxt, mMakeupRunFlowTxt, mMakeupRunStatusBanner;

        // 화면 7: 0A RCCW Overview
        private TextMeshProUGUI mRccwTankLvlTxt;

        // 모든 인터랙티브 위젯 (테스트 및 3D 가이드 화살표용)
        public E2InteractiveWidget Unit0AWidget { get; private set; }
        public E2InteractiveWidget CatPrimaryWidget { get; private set; }
        public E2InteractiveWidget PrimCeWidget { get; private set; }
        public E2InteractiveWidget PrimCvcWidget { get; private set; }
        public E2InteractiveWidget Prim0ARccwWidget { get; private set; }
        public E2InteractiveWidget SubDeminsWidget { get; private set; }
        public E2InteractiveWidget SubHeatingWidget { get; private set; }
        public E2InteractiveWidget SubRecircWidget { get; private set; }

        public E2InteractiveWidget CeAov0130Widget { get; private set; }
        public E2InteractiveWidget Ce0130OpenWidget { get; private set; }
        public E2InteractiveWidget CeAov0128Widget { get; private set; }
        public E2InteractiveWidget Ce0128CloseWidget { get; private set; }
        public E2InteractiveWidget CeSampleLevelWidget { get; private set; }

        public E2InteractiveWidget ModIsoTabWidget { get; private set; }
        public E2InteractiveWidget IsoShowAllWidget { get; private set; }
        public E2InteractiveWidget IsoCvcCheckWidget { get; private set; }

        public E2InteractiveWidget RecircLockWidget { get; private set; }
        public E2InteractiveWidget RecircSwitchModesWidget { get; private set; }
        public E2InteractiveWidget RecircPumpAWidget { get; private set; }
        public E2InteractiveWidget PumpAStopWidget { get; private set; }

        public E2InteractiveWidget Mov0318Widget { get; private set; }
        public E2InteractiveWidget Mov0318CloseWidget { get; private set; }
        public E2InteractiveWidget Aov0339Widget { get; private set; }
        public E2InteractiveWidget Aov0339CloseWidget { get; private set; }

        public E2InteractiveWidget ProcLibAutoTabWidget { get; private set; }
        public E2InteractiveWidget ProcLibSearchWidget { get; private set; }
        public E2InteractiveWidget TreeCvcMakeupWidget { get; private set; }
        public E2InteractiveWidget MakeupValidateWidget { get; private set; }
        public E2InteractiveWidget MakeupExecuteWidget { get; private set; }

        public E2InteractiveWidget RccwExpTankWidget { get; private set; }

        private readonly List<E2InteractiveWidget> mAllWidgets = new List<E2InteractiveWidget>();

        public Scenario2MainMonitorController(Scenario2ControllerModel model)
        {
            mModel = model ?? throw new ArgumentNullException(nameof(model));
        }

        /// <summary>중앙 메인 모니터 UI(크롬, 7개 화면, 공용 사이드바, 3단계 드롭다운/팝업)를 구축합니다.</summary>
        public void buildUI(Transform canvasRoot, ControlRoomSpritePack sprites)
        {
            try
            {
                mSprites = sprites ?? new ControlRoomSpritePack();
                mAllWidgets.Clear();

                mChrome.buildChrome(
                    canvasRoot,
                    true,
                    mSprites,
                    () => mModel.CurrentStep == Scenario2SequenceStep.S2_23_DeminsChecked,
                    onClickBottomProcessLibrary,
                    "net.tcp://SIMLABHS01:9080/service/tcp");

                mChrome.configureScenario2Interactions(
                    () => mModel.CurrentStep == Scenario2SequenceStep.S2_49_ModOverviewRechecked,
                    onClickUnitBox,
                    () => mModel.CurrentStep == Scenario2SequenceStep.S2_00_ProcLibInitial
                          || mModel.CurrentStep == Scenario2SequenceStep.S2_32_ModOverviewBeforeRecirc
                          || mModel.CurrentStep == Scenario2SequenceStep.S2_44_IsoValvesChecked
                          || mModel.CurrentStep == Scenario2SequenceStep.S2_51_Unit0ASelected,
                    onClickCategoryBox,
                    () => mModel.CurrentStep == Scenario2SequenceStep.S2_09_CeRecheckBeforeCvc
                          || mModel.CurrentStep == Scenario2SequenceStep.S2_12_CvcRecircChecked
                          || mModel.CurrentStep == Scenario2SequenceStep.S2_14_CeCheckedForTrip
                          || mModel.CurrentStep == Scenario2SequenceStep.S2_21_CvcsValvesClosed,
                    onClickScreenBox,
                    () => mModel.CurrentStep == Scenario2SequenceStep.S2_29_CvcMakeupRunning
                          || mModel.CurrentStep == Scenario2SequenceStep.S2_30_AlarmDropdownOpen,
                    onClickAlarmBadge,
                    () => mModel.CurrentStep == Scenario2SequenceStep.S2_07_CeFlowRerouted
                          || mModel.CurrentStep == Scenario2SequenceStep.S2_31_AlarmChecked
                          || mModel.CurrentStep == Scenario2SequenceStep.S2_40_RecircPumpsStopped
                          || mModel.CurrentStep == Scenario2SequenceStep.S2_48_CeSampleChecked,
                    onClickBottomUnitOverview,
                    () => mModel.CurrentStep == Scenario2SequenceStep.S2_08_ModOverviewChecked,
                    onClickBackNav,
                    () => mModel.IsTagLabelsVisible,
                    () => mModel.toggleTagLabels());

                var host = mChrome.ContentArea;
                buildCeScreen(host);
                buildModuleOverviewScreen(host);
                buildCvcRecircScreen(host);
                buildCvcHeatingScreen(host);
                buildCvcDeminsScreen(host);
                buildProcessLibraryScreen(host);
                buildRccwScreen(host);

                buildSharedRightPane(host);
                buildTopDropdownOverlays(mChrome.Root);

                refreshUI();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Scenario2MainMonitorController] buildUI 오류: {ex}");
            }
        }

        public void tickChrome(int heartbeatIndex)
        {
            mChrome.updateHeartbeat(heartbeatIndex);
            mChrome.refreshDeviceClock();
        }

        // =====================================================================
        // 화면 1: Primary > (CE) Containment Evacuation
        // =====================================================================
        private void buildCeScreen(RectTransform host)
        {
            mCeRoot = ControlRoomUIFactory.createPanel(host, "Screen_CE", ControlRoomTheme.BgPrimary);
            ControlRoomUIFactory.setTopLeftRect(mCeRoot, 0f, 0f, 1590f, ControlRoomTheme.ContentAreaHeight);

            var bgPid = ControlRoomUIFactory.createPanel(mCeRoot, "CePid", Color.white, mSprites.diagCePid);
            ControlRoomUIFactory.setTopLeftRect(bgPid, 0f, 0f, 1590f, ControlRoomTheme.ContentAreaHeight);
            if (mSprites.diagCePid == null) bgPid.GetComponent<Image>().color = ControlRoomTheme.BgPrimary;

            mCeCnvPressTxt = ControlRoomUIFactory.createLabel(mCeRoot, "CeCnvP", "1.02 psia", 16f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(mCeCnvPressTxt.rectTransform, 146f, 616f, 140f, 26f);

            mCeLeakRateTxt = ControlRoomUIFactory.createLabel(mCeRoot, "CeLeak", "5.88 gpm", 16f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(mCeLeakRateTxt.rectTransform, 146f, 704f, 140f, 26f);

            mCeRt1011Txt = ControlRoomUIFactory.createLabel(mCeRoot, "CeRt1011", "54.0 cps\n0.014 lb/s", 16f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.Center, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(mCeRt1011Txt.rectTransform, 560f, 52f, 140f, 44f);

            mCeFilterDpTxt = ControlRoomUIFactory.createLabel(mCeRoot, "CeFltDp", "0.003 psi   0.003 psi   0.003 psi", 14.5f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.Center, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(mCeFilterDpTxt.rectTransform, 880f, 174f, 390f, 24f);

            // CE Sample Vessel (06_CE_LIT_1009) 클릭 확인 영역
            CeSampleLevelWidget = ControlRoomUIFactory.createE2Control(
                mCeRoot, "BtnCeSampleVessel", 586f, 620f, 114f, 140f,
                new Color(0f, 0f, 0f, 0.05f), ControlRoomTheme.AccentBlue,
                null,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_47_CeLeakRechecked,
                onClickCeSampleVessel,
                null, 14f, null, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);
            mAllWidgets.Add(CeSampleLevelWidget);

            mCeSampleLevelTxt = ControlRoomUIFactory.createLabel(mCeRoot, "CeSampLvl", "0.001 ft", 15f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.Center, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(mCeSampleLevelTxt.rectTransform, 588f, 710f, 100f, 24f);

            mCeSampleCpsTxt = ControlRoomUIFactory.createLabel(mCeRoot, "CeSampCps", "54.0 cps", 15f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(mCeSampleCpsTxt.rectTransform, 696f, 728f, 110f, 24f);

            // Vacuum Pump A (ON 녹색) / B (OFF 검정)
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(mCeRoot, "VacPumpA", Color.white, mSprites.pumpOn), 312f, 250f, 52f, 52f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(mCeRoot, "VacPumpB", Color.white, mSprites.pumpOff), 312f, 386f, 52f, 52f);

            // CE-AOV-0130 (GRW 격리 밸브 — 초기 닫힘, 클릭해 OPEN)
            CeAov0130Widget = createValveControl(
                mCeRoot, "Valve_CE_AOV_0130", 886f, 286f, 44f,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_03_CeInitialAlarm,
                onClickCeAov0130, out mCe0130Img);

            // CE-AOV-0128 (RBV 격리 밸브 — 초기 열림, 클릭해 CLOSE)
            CeAov0128Widget = createValveControl(
                mCeRoot, "Valve_CE_AOV_0128", 1328f, 114f, 44f,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_05_Ce0130Opened,
                onClickCeAov0128, out mCe0128Img);

            // 하단 Tag Labels 토글 시 표시되는 흰색 태그 박스 오버레이
            mCeTagOverlay = ControlRoomUIFactory.createPanel(mCeRoot, "CeTagsOverlay", Color.clear);
            addPidTagBadge(mCeTagOverlay, "VP\n0001A", 316f, 212f, 48f, 30f);
            addPidTagBadge(mCeTagOverlay, "VP\n0001B", 316f, 442f, 48f, 30f);
            addPidTagBadge(mCeTagOverlay, "AOV\n0130", 884f, 334f, 48f, 30f);
            addPidTagBadge(mCeTagOverlay, "AOV\n0128", 1326f, 162f, 48f, 30f);
            addPidTagBadge(mCeTagOverlay, "AOV\n0117", 574f, 224f, 48f, 30f);
            mCeTagOverlay.gameObject.SetActive(false);

            // 밸브 3줄 컨텍스트 팝업 (Open / Close / Set Position)
            mCe0130Popup = createValvePopup(mCeRoot, "Popup_0130", 934f, 266f, "06_CE_AOV_0130", true,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_04_Ce0130PopupOpen, onClickCe0130Open, out var openBtn);
            Ce0130OpenWidget = openBtn;

            mCe0128Popup = createValvePopup(mCeRoot, "Popup_0128", 1180f, 156f, "06_CE_AOV_0128", false,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_06_Ce0128PopupOpen, onClickCe0128Close, out var closeBtn);
            Ce0128CloseWidget = closeBtn;

            mCeRoot.gameObject.SetActive(false);
        }

        // =====================================================================
        // 화면 2: Special > Module Overview (FW/MS + Isolation 탭 + SHOW ALL)
        // =====================================================================
        private void buildModuleOverviewScreen(RectTransform host)
        {
            mModOverviewRoot = ControlRoomUIFactory.createPanel(host, "Screen_ModuleOverview", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(mModOverviewRoot, 0f, 0f, 1590f, ControlRoomTheme.ContentAreaHeight);

            var leftCol = ControlRoomUIFactory.createPanel(mModOverviewRoot, "ModLeft", ControlRoomTheme.BgPrimary);
            ControlRoomUIFactory.setTopLeftRect(leftCol, 0f, 0f, 418f, ControlRoomTheme.ContentAreaHeight);
            var rxm = ControlRoomUIFactory.createPanel(leftCol, "RxmCut", Color.white, mSprites.diagNpmCutaway);
            ControlRoomUIFactory.setTopLeftRect(rxm, 148f, 120f, 236f, 650f);

            mModCnvPressTxt = addModLeftMetric(leftCol, "CNV Pressure:", 584f);
            mModPzrLevelTxt = addModLeftMetric(leftCol, "PZR Level:", 692f);
            mModPowerTxt = addModLeftMetric(leftCol, "Core Power:", 800f);
            mModTaveTxt = addModLeftMetric(leftCol, "RCS T-Ave:", 874f);

            var topTabBar = ControlRoomUIFactory.createPanel(mModOverviewRoot, "TopTabs", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(topTabBar, 422f, 4f, 1164f, 34f);
            string[] topTabs = { "FW/MS", "CVC", "PZR", "RCS", "CHR", "Containment", "Power Maneuvering", "Isolation" };
            float tx = 4f;
            for (int i = 0; i < topTabs.Length; i++)
            {
                float tw = topTabs[i].Length > 11 ? 156f : 92f;
                var w = ControlRoomUIFactory.createE2Control(
                    topTabBar, "MT_" + topTabs[i], tx, 2f, tw, 30f,
                    i == 0 ? ControlRoomTheme.AccentBlue : ControlRoomTheme.BgPanel, ControlRoomTheme.AccentBlue,
                    i == 0 ? () => true : (Func<bool>)null, () => false, null, topTabs[i], 13.5f);
                mAllWidgets.Add(w);
                tx += tw + 4f;
            }

            var chartBox = ControlRoomUIFactory.createBorderedBox(mModOverviewRoot, "ModChartBox", ControlRoomTheme.BgPrimary, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(chartBox, 422f, 42f, 1164f, 356f);
            var chart = ControlRoomUIFactory.createTrendChart(chartBox, "ModTrend", 16f, 8f, 1130f, 336f, 0f, 160f, 8, 5, true);
            chart.updateSeries(0, new[] { 151f, 151f, 151f, 151f }, ControlRoomTheme.StatusCaution, 2.5f);

            var botTabBar = ControlRoomUIFactory.createPanel(mModOverviewRoot, "BotTabs", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(botTabBar, 422f, 520f, 1164f, 36f);
            string[] botTabs = { "FW/MS", "CVC", "RCS", "ECC/DHR", "CNT", "CRDS", "Isolation", "PZR" };
            float btx = 4f;
            for (int i = 0; i < botTabs.Length; i++)
            {
                bool isFwms = botTabs[i] == "FW/MS";
                bool isIso = botTabs[i] == "Isolation";
                float tw = 96f;
                var w = ControlRoomUIFactory.createE2Control(
                    botTabBar, "MBT_" + botTabs[i], btx, 2f, tw, 32f,
                    ControlRoomTheme.BgPanel, ControlRoomTheme.AccentBlue,
                    isFwms ? (Func<bool>)(() => !mModel.IsModuleIsoTabSelected) : (isIso ? (Func<bool>)(() => mModel.IsModuleIsoTabSelected) : null),
                    isIso ? (Func<bool>)(() => mModel.CurrentStep == Scenario2SequenceStep.S2_41_ModOverviewForIso) : (() => false),
                    isIso ? onClickModIsoTab : null,
                    botTabs[i], 14f);
                if (isIso) ModIsoTabWidget = w;
                mAllWidgets.Add(w);
                btx += tw + 4f;
            }

            mModFwmsPanel = ControlRoomUIFactory.createBorderedBox(mModOverviewRoot, "ModFwmsPanel", ControlRoomTheme.BgPrimary, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(mModFwmsPanel, 422f, 560f, 1164f, 408f);
            var fwms = ControlRoomUIFactory.createPanel(mModFwmsPanel, "FwmsPic", Color.white, mSprites.diagFwmsMimic);
            ControlRoomUIFactory.setOffsets(fwms, 12f, 12f, -12f, -12f);

            mModIsoPanel = ControlRoomUIFactory.createBorderedBox(mModOverviewRoot, "ModIsoPanel", ControlRoomTheme.BgPrimary, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(mModIsoPanel, 422f, 560f, 1164f, 408f);
            buildIsolationMatrix(mModIsoPanel);
            mModIsoPanel.gameObject.SetActive(false);

            mModOverviewRoot.gameObject.SetActive(false);
        }

        private void buildIsolationMatrix(RectTransform panel)
        {
            string[] col1 = { "CE", "CFD", "CVC DG", "CVC D", "CVC I", "CVC S" };
            string[] col1TagsLeft = { "ISV\n0101", "ISV\n0128", "ISV\n0401", "ISV\n0334", "ISV\n0331", "ISV\n0325" };
            string[] col1TagsRight = { "ISV\n0102", "ISV\n0129", "ISV\n0402", "ISV\n0336", "ISV\n0329", "ISV\n0323" };

            string[] col2 = { "DHR 1", "DHR 2", "FW A", "FW B", "MS A", "MS B" };
            string[] col2TagsLeft = { "HOV\n1101", "HOV\n1201", "HOV\n0132", "HOV\n0232", "HOV\n0101", "HOV\n0201" };
            string[] col2TagsRight = { "HOV\n1102", "HOV\n1202", "", "", "", "" };

            string[] col3 = { "RCCW R", "RCCW S" };
            string[] col3TagsLeft = { "ISV\n0190", "ISV\n0185" };
            string[] col3TagsRight = { "ISV\n0191", "ISV\n0184" };

            mIsoTagOverlay = ControlRoomUIFactory.createPanel(panel, "IsoTagOverlay", Color.clear);

            for (int i = 0; i < col1.Length; i++)
            {
                float ry = 18f + i * 52f;
                ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(panel, "C1_" + i, col1[i], 15f, ControlRoomTheme.TextPrimary).rectTransform, 28f, ry, 92f, 32f);
                bool open = i == 0 || i == 3 || i == 4 || i == 5;

                if (col1[i] == "CVC I")
                {
                    mIsoCvcISV331Img = ControlRoomUIFactory.createStaticValve(panel, 160f, ry + 16f, 34f, mSprites.valveOpen);
                    mIsoCvcISV329Img = ControlRoomUIFactory.createStaticValve(panel, 226f, ry + 16f, 34f, mSprites.valveOpen);
                }
                else if (col1[i] == "CVC S")
                {
                    var btn = ControlRoomUIFactory.createE2Control(
                        panel, "BtnIsoCvcS", 128f, ry - 4f, 152f, 42f,
                        ControlRoomTheme.BgPanel, ControlRoomTheme.AccentBlue,
                        null,
                        () => mModel.CurrentStep == Scenario2SequenceStep.S2_43_IsoShowAllActive,
                        onClickIsoCvcCheck,
                        null, 14f, null, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);
                    IsoCvcCheckWidget = btn;
                    mAllWidgets.Add(btn);
                    ControlRoomUIFactory.createStaticValve(btn.GetComponent<RectTransform>(), 42f, 21f, 34f, mSprites.valveOpen);
                    ControlRoomUIFactory.createStaticValve(btn.GetComponent<RectTransform>(), 110f, 21f, 34f, mSprites.valveOpen);
                }
                else
                {
                    ControlRoomUIFactory.createStaticValve(panel, 160f, ry + 16f, 34f, open ? mSprites.valveOpen : mSprites.valveClosed);
                    ControlRoomUIFactory.createStaticValve(panel, 226f, ry + 16f, 34f, open ? mSprites.valveOpen : mSprites.valveClosed);
                }

                addPidTagBadge(mIsoTagOverlay, col1TagsLeft[i], 138f, ry - 2f, 48f, 34f);
                addPidTagBadge(mIsoTagOverlay, col1TagsRight[i], 204f, ry - 2f, 48f, 34f);
            }

            // 영상 f_263 / f_274 기준: DHR 1~2는 2개, FW A/B · MS A/B는 각 1개 밸브
            for (int i = 0; i < col2.Length; i++)
            {
                float ry = 18f + i * 52f;
                ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(panel, "C2_" + i, col2[i], 15f, ControlRoomTheme.TextPrimary).rectTransform, 370f, ry, 92f, 32f);
                bool open = i >= 2;
                ControlRoomUIFactory.createStaticValve(panel, 490f, ry + 16f, 34f, open ? mSprites.valveOpen : mSprites.valveClosed);
                addPidTagBadge(mIsoTagOverlay, col2TagsLeft[i], 468f, ry - 2f, 48f, 34f);

                if (i < 2)
                {
                    ControlRoomUIFactory.createStaticValve(panel, 556f, ry + 16f, 34f, mSprites.valveClosed);
                    addPidTagBadge(mIsoTagOverlay, col2TagsRight[i], 534f, ry - 2f, 48f, 34f);
                }
            }

            for (int i = 0; i < col3.Length; i++)
            {
                float ry = 18f + i * 52f;
                ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(panel, "C3_" + i, col3[i], 15f, ControlRoomTheme.TextPrimary).rectTransform, 700f, ry, 92f, 32f);
                ControlRoomUIFactory.createStaticValve(panel, 824f, ry + 16f, 34f, mSprites.valveOpen);
                ControlRoomUIFactory.createStaticValve(panel, 890f, ry + 16f, 34f, mSprites.valveOpen);
                addPidTagBadge(mIsoTagOverlay, col3TagsLeft[i], 802f, ry - 2f, 48f, 34f);
                addPidTagBadge(mIsoTagOverlay, col3TagsRight[i], 868f, ry - 2f, 48f, 34f);
            }

            mIsoTagOverlay.gameObject.SetActive(false);

            // 하단 중앙 SHOW ALL 토글 버튼 (영상 04:17~04:42)
            IsoShowAllWidget = ControlRoomUIFactory.createE2Control(
                panel, "BtnIsoShowAll", 630f, 354f, 130f, 34f,
                ControlRoomTheme.BgButton, ControlRoomTheme.AccentBlue,
                () => mModel.IsIsoShowAllActive,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_42_IsoTabOpened,
                onClickIsoShowAll,
                "SHOW ALL", 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);
            mAllWidgets.Add(IsoShowAllWidget);
        }

        private TextMeshProUGUI addModLeftMetric(RectTransform parent, string key, float y)
        {
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(parent, key + "_K", key, 14f, ControlRoomTheme.TextPrimary).rectTransform, 8f, y, 130f, 22f);
            var val = ControlRoomUIFactory.createLabel(parent, key + "_V", "--", 18f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(val.rectTransform, 8f, y + 22f, 134f, 26f);
            return val;
        }

        // =====================================================================
        // 화면 3: Primary > (CVC) > Recirc & Makeup
        // =====================================================================
        private void buildCvcRecircScreen(RectTransform host)
        {
            mCvcRecircRoot = ControlRoomUIFactory.createPanel(host, "Screen_CvcRecirc", ControlRoomTheme.BgPrimary);
            ControlRoomUIFactory.setTopLeftRect(mCvcRecircRoot, 0f, 0f, 1590f, ControlRoomTheme.ContentAreaHeight);

            var pid = ControlRoomUIFactory.createPanel(mCvcRecircRoot, "Pid", Color.white, mSprites.diagCvcRecirc);
            ControlRoomUIFactory.setTopLeftRect(pid, 0f, 0f, 1590f, ControlRoomTheme.ContentAreaHeight);
            if (mSprites.diagCvcRecirc == null) pid.GetComponent<Image>().color = ControlRoomTheme.BgPrimary;

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mCvcRecircRoot, "ExpT", "2.35 ft\n282 psia", 15f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.MidlineLeft, FontStyles.Bold).rectTransform, 32f, 650f, 120f, 44f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mCvcRecircRoot, "MkF", "0 gpm\n0.05 gpm", 15f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.MidlineLeft, FontStyles.Bold).rectTransform, 654f, 108f, 120f, 48f);

            // CVC Recirculation Lock 버튼 + 모드 라벨
            RecircLockWidget = ControlRoomUIFactory.createE2Control(
                mCvcRecircRoot, "BtnRecircLock", 322f, 624f, 46f, 46f,
                ControlRoomTheme.BgPanel, ControlRoomTheme.AccentBlue,
                null,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_36_RecircForPumpStop,
                onClickRecircLock,
                null, 12f, null, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);
            mAllWidgets.Add(RecircLockWidget);
            mRecircLockImg = ControlRoomUIFactory.createPanel(RecircLockWidget.transform.Find("Inner"), "LockIco", Color.white, mSprites.lockClosed).GetComponent<Image>();
            mRecircLockImg.preserveAspect = true;

            mRecircModeLbl = ControlRoomUIFactory.createLabel(mCvcRecircRoot, "RecircModeLbl", "Recirculation", 14.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            ControlRoomUIFactory.setTopLeftRect(mRecircModeLbl.rectTransform, 376f, 634f, 140f, 26f);

            RecircPumpAWidget = ControlRoomUIFactory.createE2Control(
                mCvcRecircRoot, "BtnPumpA", 362f, 558f, 54f, 54f,
                Color.clear, ControlRoomTheme.AccentBlue,
                null,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_38_RecircManualMode,
                onClickRecircPumpA);
            mAllWidgets.Add(RecircPumpAWidget);
            mRecircPumpAImg = ControlRoomUIFactory.createPanel(RecircPumpAWidget.transform, "PumpImg", Color.white, mSprites.pumpOn).GetComponent<Image>();
            mRecircPumpAImg.preserveAspect = true;

            mRecircPumpBImg = ControlRoomUIFactory.createStaticPump(mCvcRecircRoot, "PumpB", 389f, 706f, 54f, mSprites.pumpOff);

            mRecircTagOverlay = ControlRoomUIFactory.createPanel(mCvcRecircRoot, "RecircTags", Color.clear);
            addPidTagBadge(mRecircTagOverlay, "P\n0004A", 365f, 520f, 50f, 30f);
            addPidTagBadge(mRecircTagOverlay, "P\n0004B", 365f, 764f, 50f, 30f);
            mRecircTagOverlay.gameObject.SetActive(false);

            // 펌프 A 컨텍스트 팝업 (Start / Stop / Set Speed)
            mPumpAPopup = ControlRoomUIFactory.createBorderedBox(mCvcRecircRoot, "Popup_PumpA", ControlRoomTheme.BgDarkest, ControlRoomTheme.HighlightBlue, 1.5f);
            ControlRoomUIFactory.setTopLeftRect(mPumpAPopup, 424f, 546f, 168f, 114f);
            var pIn = mPumpAPopup.Find("Inner");
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(pIn, "T", "06_CVC_P_0004A", 12.5f, ControlRoomTheme.TextSecondary).rectTransform, 8f, 4f, 152f, 20f);
            mAllWidgets.Add(ControlRoomUIFactory.createE2Control(pIn, "BtnStart", 4f, 26f, 158f, 26f, ControlRoomTheme.BgDarkPane, ControlRoomTheme.BgButton, null, () => false, null, "  Start", 13.5f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.MidlineLeft));
            PumpAStopWidget = ControlRoomUIFactory.createE2Control(
                pIn, "BtnStop", 4f, 54f, 158f, 26f,
                ControlRoomTheme.BgDarkPane, ControlRoomTheme.ClickPressBlue,
                null,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_39_PumpAPopupOpen,
                onClickPumpAStop,
                "  Stop", 13.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            mAllWidgets.Add(PumpAStopWidget);
            mAllWidgets.Add(ControlRoomUIFactory.createE2Control(pIn, "BtnSpeed", 4f, 82f, 158f, 26f, ControlRoomTheme.BgDarkPane, ControlRoomTheme.BgButton, null, () => false, null, "  Set Speed", 13.5f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.MidlineLeft));
            mPumpAPopup.gameObject.SetActive(false);

            mCvcRecircRoot.gameObject.SetActive(false);
        }

        // =====================================================================
        // 화면 4: Primary > (CVC) > Heating & Spray
        // (영상 f_139/f_141 기준 MOV-0318=중앙좌측 주입관, AOV-0339=좌측하단 복귀관)
        // =====================================================================
        private void buildCvcHeatingScreen(RectTransform host)
        {
            mCvcHeatingRoot = ControlRoomUIFactory.createPanel(host, "Screen_CvcHeating", ControlRoomTheme.BgPrimary);
            ControlRoomUIFactory.setTopLeftRect(mCvcHeatingRoot, 0f, 0f, 1590f, ControlRoomTheme.ContentAreaHeight);

            var pid = ControlRoomUIFactory.createPanel(mCvcHeatingRoot, "Pid", Color.white, mSprites.diagCvcHeatingSpray);
            ControlRoomUIFactory.setTopLeftRect(pid, 0f, 0f, 1590f, ControlRoomTheme.ContentAreaHeight);
            if (mSprites.diagCvcHeatingSpray == null) pid.GetComponent<Image>().color = ControlRoomTheme.BgPrimary;

            mHeatSprayFlowTxt = ControlRoomUIFactory.createLabel(mCvcHeatingRoot, "SprayF", "0.37 gpm", 16f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(mHeatSprayFlowTxt.rectTransform, 800f, 134f, 120f, 26f);
            mHeatInjFlowTxt = ControlRoomUIFactory.createLabel(mCvcHeatingRoot, "InjF", "22.1 gpm", 16f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(mHeatInjFlowTxt.rectTransform, 650f, 238f, 120f, 26f);
            mHeatDischFlowTxt = ControlRoomUIFactory.createLabel(mCvcHeatingRoot, "DischF", "22.5 gpm", 16f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(mHeatDischFlowTxt.rectTransform, 756f, 384f, 120f, 26f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mCvcHeatingRoot, "Press1841", "1841 psia\n422 °F", 15f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.MidlineLeft, FontStyles.Bold).rectTransform, 692f, 356f, 110f, 44f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mCvcHeatingRoot, "Press1839", "1839 psia\n497 °F", 15f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.MidlineLeft, FontStyles.Bold).rectTransform, 396f, 756f, 110f, 44f);

            // CVC-MOV-0318 (중앙 좌측 주입 배관 y=376)
            Mov0318Widget = createValveControl(
                mCvcHeatingRoot, "Valve_MOV_0318", 640f, 376f, 44f,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_17_HeatingSprayOpened,
                onClickMov0318, out mMov0318Img);

            // CVC-AOV-0339 (좌측 하단 복귀 배관 y=774 — 0.25초 영상 f_141/f_145 실측 위치!)
            Aov0339Widget = createValveControl(
                mCvcHeatingRoot, "Valve_AOV_0339", 340f, 774f, 44f,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_19_Mov0318Closed,
                onClickAov0339, out mAov0339Img);

            mHeatingTagOverlay = ControlRoomUIFactory.createPanel(mCvcHeatingRoot, "HeatTags", Color.clear);
            addPidTagBadge(mHeatingTagOverlay, "MOV\n0318", 636f, 424f, 52f, 30f);
            addPidTagBadge(mHeatingTagOverlay, "AOV\n0339", 336f, 822f, 52f, 30f);
            mHeatingTagOverlay.gameObject.SetActive(false);

            mMov0318Popup = createValvePopup(mCvcHeatingRoot, "Popup_0318", 690f, 346f, "06_CVC_MOV_0318", false,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_18_Mov0318PopupOpen, onClickMov0318Close, out var movClose);
            Mov0318CloseWidget = movClose;

            mAov0339Popup = createValvePopup(mCvcHeatingRoot, "Popup_0339", 392f, 690f, "06_CVC_AOV_0339", false,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_20_Aov0339PopupOpen, onClickAov0339Close, out var aovClose);
            Aov0339CloseWidget = aovClose;

            mCvcHeatingRoot.gameObject.SetActive(false);
        }

        // =====================================================================
        // 화면 5: Primary > (CVC) > Demins & Letdowns
        // =====================================================================
        private void buildCvcDeminsScreen(RectTransform host)
        {
            mCvcDeminsRoot = ControlRoomUIFactory.createPanel(host, "Screen_CvcDemins", ControlRoomTheme.BgPrimary);
            ControlRoomUIFactory.setTopLeftRect(mCvcDeminsRoot, 0f, 0f, 1590f, ControlRoomTheme.ContentAreaHeight);

            var pid = ControlRoomUIFactory.createPanel(mCvcDeminsRoot, "Pid", Color.white, mSprites.diagCvcDemins);
            ControlRoomUIFactory.setTopLeftRect(pid, 0f, 0f, 1590f, ControlRoomTheme.ContentAreaHeight);
            if (mSprites.diagCvcDemins == null) pid.GetComponent<Image>().color = ControlRoomTheme.BgPrimary;

            mDeminsFlowTxt = ControlRoomUIFactory.createLabel(mCvcDeminsRoot, "Fit2012", "0 gpm\n283 psia\n75 °F", 16f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(mDeminsFlowTxt.rectTransform, 166f, 668f, 140f, 78f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(mCvcDeminsRoot, "AutoLock", Color.white, mSprites.lockClosed), 270f, 696f, 28f, 28f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mCvcDeminsRoot, "AutoLbl", "Auto Letdown\nEnabled", 13.5f, ControlRoomTheme.TextPrimary).rectTransform, 304f, 692f, 120f, 38f);

            mDeminsTagOverlay = ControlRoomUIFactory.createPanel(mCvcDeminsRoot, "DeminsTags", Color.clear);
            addPidTagBadge(mDeminsTagOverlay, "FIT\n2012", 166f, 632f, 52f, 30f);
            addPidTagBadge(mDeminsTagOverlay, "PDIT\n2005", 516f, 444f, 52f, 30f);
            mDeminsTagOverlay.gameObject.SetActive(false);

            mCvcDeminsRoot.gameObject.SetActive(false);
        }

        // =====================================================================
        // 화면 6: Special > Process Library (Procedures -> Automations -> CVC Makeup)
        // =====================================================================
        private void buildProcessLibraryScreen(RectTransform host)
        {
            mProcLibRoot = ControlRoomUIFactory.createPanel(host, "Screen_ProcLibS2", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(mProcLibRoot, 0f, 0f, 1590f, ControlRoomTheme.ContentAreaHeight);

            // 상단 서브바 (Procedures / Automations)
            var sub = ControlRoomUIFactory.createPanel(mProcLibRoot, "SubBar", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(sub, 0f, 0f, 1590f, 46f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(sub, "SubLine", ControlRoomTheme.BorderSubtle), 0f, 44.5f, 1590f, 1.5f);

            mProcTabTxt = ControlRoomUIFactory.createLabel(sub, "TabProc", "Procedures", 23f, ControlRoomTheme.TabActiveGreen, TextAlignmentOptions.MidlineLeft);
            ControlRoomUIFactory.setTopLeftRect(mProcTabTxt.rectTransform, 18f, 4f, 150f, 38f);
            mAutoTabTxt = ControlRoomUIFactory.createLabel(sub, "TabAuto", "Automations", 23f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            ControlRoomUIFactory.setTopLeftRect(mAutoTabTxt.rectTransform, 184f, 4f, 168f, 38f);
            ProcLibAutoTabWidget = ControlRoomUIFactory.createE2Control(
                sub, "HitAuto", 178f, 2f, 174f, 42f, Color.clear, Color.clear, null,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_24_ProcLibOpened,
                onClickProcLibAutomationsTab);
            mAllWidgets.Add(ProcLibAutoTabWidget);

            // 좌측 트리 패널 (x=0..388)
            var leftPane = ControlRoomUIFactory.createPanel(mProcLibRoot, "LeftPane", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(leftPane, 0f, 46f, 388f, ControlRoomTheme.ContentAreaHeight - 46f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(mProcLibRoot, "DivL", ControlRoomTheme.BorderSubtle), 388f, 46f, 1.5f, ControlRoomTheme.ContentAreaHeight - 46f);

            // Procedures 좌측 목록 (S2_00 초기 화면: CE 비상절차서 선택 표시)
            mProcTreePane = ControlRoomUIFactory.createPanel(leftPane, "ProcTreePane", Color.clear);
            mProcSearchRowRt = ControlRoomUIFactory.createBorderedBox(mProcTreePane, "SearchBoxP", ControlRoomTheme.BgInput, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(mProcSearchRowRt, 10f, 10f, 250f, 32f);
            ControlRoomUIFactory.createE2Control(mProcTreePane, "BtnSearchP", 268f, 10f, 66f, 32f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "Search", 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true);
            ControlRoomUIFactory.createE2Control(mProcTreePane, "BtnClearXP", 340f, 10f, 34f, 32f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "X", 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mProcTreePane, "PHdr", "Procedure Title                                  # Steps", 12.5f, ControlRoomTheme.TextSecondary).rectTransform, 14f, 48f, 360f, 20f);
            string[] procCats = { "ADMN  -  Administrative Procedures", "EOP    -  Emergency Operating Procedures", "IAP     -  Integrated Abnormal Procedures", "BAS    -  Boron Addition System", "CE      -  Containment Evacuation System" };
            for (int i = 0; i < procCats.Length; i++)
            {
                float py = 74f + i * 44f;
                var row = ControlRoomUIFactory.createBorderedBox(mProcTreePane, $"ProcCat_{i}", ControlRoomTheme.BgListItem, ControlRoomTheme.BorderSubtle, 1f);
                ControlRoomUIFactory.setTopLeftRect(row, 10f, py, 368f, 40f);
                ControlRoomUIFactory.createLabel(row.Find("Inner"), "T", "  " + procCats[i], 13f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            }
            var ceProcRow = ControlRoomUIFactory.createBorderedBox(mProcTreePane, "ProcCeSelected", ControlRoomTheme.AccentBlue, ControlRoomTheme.HighlightBlue, 1f);
            ControlRoomUIFactory.setTopLeftRect(ceProcRow, 28f, 294f, 350f, 54f);
            ControlRoomUIFactory.createLabel(ceProcRow.Find("Inner"), "T", "  Respond to XX CE gaseous discharge\n  radioactivity HI                                 12", 12.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);

            // Automations 좌측 목록 (Search "cvc" -> CVC Makeup)
            mAutoLeftPane = ControlRoomUIFactory.createPanel(leftPane, "AutoLeftPane", Color.clear);
            var sb = ControlRoomUIFactory.createBorderedBox(mAutoLeftPane, "SearchBox", ControlRoomTheme.BgInput, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(sb, 10f, 10f, 250f, 32f);
            mAutoSearchInputTxt = ControlRoomUIFactory.createLabel(sb.Find("Inner"), "SearchTxt", "", 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            ControlRoomUIFactory.setOffsets(mAutoSearchInputTxt.rectTransform, 8f, 2f, -8f, -2f);

            ProcLibSearchWidget = ControlRoomUIFactory.createE2Control(
                mAutoLeftPane, "BtnSearch", 268f, 10f, 66f, 32f,
                ControlRoomTheme.BgButton, ControlRoomTheme.AccentBlue,
                null,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_25_AutomationsTabOpened,
                onClickProcLibSearch,
                "Search", 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);
            mAllWidgets.Add(ProcLibSearchWidget);
            ControlRoomUIFactory.createE2Control(mAutoLeftPane, "BtnClearX", 340f, 10f, 34f, 32f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "X", 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true);

            mAutoTreeRootPane = ControlRoomUIFactory.createPanel(mAutoLeftPane, "AutoTreeAll", Color.clear);
            ControlRoomUIFactory.setTopLeftRect(mAutoTreeRootPane, 0f, 52f, 388f, 440f);
            string[] allAutoItems = {
                "ADMN - Administrative Procedures",
                "BAS  - Boron Addition System",
                "CAR  - Condenser Air Removal System",
                "CE   - Containment Evacuation System",
                "CVC  - Chemical and Volume Control System",
                "RCCW - Reactor Component Cooling Water System"
            };
            for (int i = 0; i < allAutoItems.Length; i++)
            {
                var r = ControlRoomUIFactory.createBorderedBox(mAutoTreeRootPane, $"AA_{i}", ControlRoomTheme.BgListItem, ControlRoomTheme.BorderSubtle, 1f);
                ControlRoomUIFactory.setTopLeftRect(r, 10f, i * 44f, 368f, 40f);
                ControlRoomUIFactory.createLabel(r.Find("Inner"), "T", "  " + allAutoItems[i], 13f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            }

            mCvcSubTreePane = ControlRoomUIFactory.createPanel(mAutoLeftPane, "CvcSubTree", Color.clear);
            ControlRoomUIFactory.setTopLeftRect(mCvcSubTreePane, 0f, 58f, 388f, 220f);
            var cvcParentRow = ControlRoomUIFactory.createBorderedBox(mCvcSubTreePane, "RowCvcHdr", ControlRoomTheme.BgListItem, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(cvcParentRow, 10f, 0f, 368f, 42f);
            ControlRoomUIFactory.createLabel(cvcParentRow.Find("Inner"), "T", "  CVC - Chemical and Volume Control System", 13f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);

            ControlRoomUIFactory.createE2Control(mCvcSubTreePane, "RowLetdown", 34f, 48f, 344f, 42f, ControlRoomTheme.BgListItem, ControlRoomTheme.BgButton, null, () => false, null, "  CVC Letdown", 13.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, null, true, ControlRoomTheme.BorderSubtle);
            TreeCvcMakeupWidget = ControlRoomUIFactory.createE2Control(
                mCvcSubTreePane, "RowMakeup", 34f, 94f, 344f, 42f,
                ControlRoomTheme.BgListItem, ControlRoomTheme.AccentBlue,
                () => mModel.CurrentStep >= Scenario2SequenceStep.S2_27_CvcMakeupSetup,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_26_CvcAutoSearched,
                onClickTreeCvcMakeup,
                "  CVC Makeup", 13.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, null, true, ControlRoomTheme.BorderSubtle);
            mAllWidgets.Add(TreeCvcMakeupWidget);
            mCvcSubTreePane.gameObject.SetActive(false);

            // 중앙 본문 영역 (x=389.5..1590, y=46..974)
            var centerHost = ControlRoomUIFactory.createPanel(mProcLibRoot, "CenterHost", ControlRoomTheme.BgWorkspaceEmpty);
            ControlRoomUIFactory.setTopLeftRect(centerHost, 389.5f, 46f, 1200.5f, ControlRoomTheme.ContentAreaHeight - 46f);

            mMakeupEmptyPane = ControlRoomUIFactory.createPanel(centerHost, "Empty", ControlRoomTheme.BgWorkspaceEmpty);

            buildCvcMakeupSetupPane(centerHost);
            buildCvcMakeupRunPane(centerHost);

            mProcLibRoot.gameObject.SetActive(false);
        }

        private void buildCvcMakeupSetupPane(RectTransform centerHost)
        {
            mMakeupSetupPane = ControlRoomUIFactory.createPanel(centerHost, "MakeupSetupPane", ControlRoomTheme.BgCard);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mMakeupSetupPane, "Hdr", "Unit06  :  CVC Makeup", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, FontStyles.Italic).rectTransform, 14f, 4f, 400f, 28f);

            var top = ControlRoomUIFactory.createBorderedBox(mMakeupSetupPane, "ParamsBox", ControlRoomTheme.BgPrimary, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(top, 8f, 36f, 1184f, 706f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(top, "T", "CVC Makeup Parameters", 17f, ControlRoomTheme.TextPrimary).rectTransform, 14f, 10f, 320f, 26f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(top, "OptLbl", "Makeup Mixture Options:", 15f, ControlRoomTheme.TextPrimary).rectTransform, 14f, 48f, 220f, 26f);

            // 영상 f_183/f_187 실측: Boric Acid Only 기본 선택 유지! (Blend 미선택)
            var boricRing = ControlRoomUIFactory.createPanel(top, "RingBoric", ControlRoomTheme.HighlightBlue, mSprites.circleDot);
            ControlRoomUIFactory.setTopLeftRect(boricRing, 240f, 52f, 16f, 16f);
            var boricHole = ControlRoomUIFactory.createPanel(boricRing, "Hole", ControlRoomTheme.BgDarkest, mSprites.circleDot);
            ControlRoomUIFactory.setOffsets(boricHole, 2f, 2f, -2f, -2f);
            var boricDotRt = ControlRoomUIFactory.createPanel(boricHole, "Dot", ControlRoomTheme.HighlightBlue, mSprites.circleDot);
            ControlRoomUIFactory.setOffsets(boricDotRt, 2.5f, 2.5f, -2.5f, -2.5f);
            mMakeupBoricDot = boricDotRt.GetComponent<Image>();
            mMakeupBoricDot.gameObject.SetActive(true);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(top, "OptBA", "Boric Acid Only", 14.5f, ControlRoomTheme.TextPrimary).rectTransform, 264f, 48f, 150f, 26f);

            var blendRing = ControlRoomUIFactory.createPanel(top, "RingBlend", ControlRoomTheme.TextSecondary, mSprites.circleDot);
            ControlRoomUIFactory.setTopLeftRect(blendRing, 420f, 52f, 16f, 16f);
            var blendHole = ControlRoomUIFactory.createPanel(blendRing, "Hole", ControlRoomTheme.BgDarkest, mSprites.circleDot);
            ControlRoomUIFactory.setOffsets(blendHole, 2f, 2f, -2f, -2f);
            var blendDotRt = ControlRoomUIFactory.createPanel(blendHole, "Dot", ControlRoomTheme.HighlightBlue, mSprites.circleDot);
            ControlRoomUIFactory.setOffsets(blendDotRt, 2.5f, 2.5f, -2.5f, -2.5f);
            mMakeupBlendRadioDot = blendDotRt.GetComponent<Image>();
            mMakeupBlendRadioDot.gameObject.SetActive(false);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(top, "OptBlend", "Blend", 14.5f, ControlRoomTheme.TextSecondary).rectTransform, 444f, 48f, 100f, 26f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(top, "BasInfo", "Boron Addition System Lineup Status:  <color=#4CE896>Ready</color>    Tank Level: 100 %    RCS Concentration: 595 ppm", 14.5f, ControlRoomTheme.TextPrimary).rectTransform, 14f, 92f, 1100f, 26f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(top, "DwInfo", "Demineralized Water System Lineup Status:  <color=#4CE896>Ready</color>    BAS Supply Pump A/B & DW Pump A/B: Ready", 14.5f, ControlRoomTheme.TextPrimary).rectTransform, 14f, 128f, 1100f, 26f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(top, "CvcInfo", "Chemical & Volume Control System Lineup Status:  <color=#4CE896>Ready</color>    Filter/Pump A/B 312 psia -> 1795 psia", 14.5f, ControlRoomTheme.TextPrimary).rectTransform, 14f, 164f, 1100f, 26f);

            mMakeupSetupPzrTxt = ControlRoomUIFactory.createLabel(top, "PzrLvl", "Pressurizer Level:  57.6 %\nRecommended Target Level:  59.92 %", 17f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(mMakeupSetupPzrTxt.rectTransform, 40f, 520f, 520f, 70f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(top, "SumHdr", "Execution Summary", 16.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineRight).rectTransform, 800f, 500f, 360f, 26f);
            var sumBox = ControlRoomUIFactory.createBorderedBox(top, "SumBox", ControlRoomTheme.BgInput, ControlRoomTheme.TextPrimary, 1.5f);
            ControlRoomUIFactory.setTopLeftRect(sumBox, 790f, 534f, 376f, 130f);
            mMakeupSummaryTxt = ControlRoomUIFactory.createLabel(sumBox.Find("Inner"), "Val", "", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.TopLeft);
            ControlRoomUIFactory.setOffsets(mMakeupSummaryTxt.rectTransform, 10f, 10f, -10f, -10f);

            var footer = ControlRoomUIFactory.createPanel(mMakeupSetupPane, "Footer", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(footer, 0f, 876f, 1200f, 52f);
            mMakeupReadyTxt = ControlRoomUIFactory.createLabel(footer, "ReadyTxt", "Ready to start", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineRight, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(mMakeupReadyTxt.rectTransform, 796f, 8f, 160f, 36f);
            mMakeupReadyTxt.gameObject.SetActive(false);

            MakeupValidateWidget = ControlRoomUIFactory.createE2Control(
                footer, "BtnValidate", 966f, 8f, 102f, 36f,
                ControlRoomTheme.BgButton, ControlRoomTheme.BgButton,
                null,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_27_CvcMakeupSetup,
                onClickMakeupValidate,
                "Validate", 15.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);
            mAllWidgets.Add(MakeupValidateWidget);

            MakeupExecuteWidget = ControlRoomUIFactory.createE2Control(
                footer, "BtnExecute", 1076f, 8f, 102f, 36f,
                ControlRoomTheme.BgButton, ControlRoomTheme.BgButton,
                null,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_28_CvcMakeupValidated,
                onClickMakeupExecute,
                "Execute", 15.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);
            mAllWidgets.Add(MakeupExecuteWidget);

            mMakeupSetupPane.gameObject.SetActive(false);
        }

        private void buildCvcMakeupRunPane(RectTransform centerHost)
        {
            mMakeupRunPane = ControlRoomUIFactory.createPanel(centerHost, "MakeupRunPane", ControlRoomTheme.BgCard);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mMakeupRunPane, "Hdr", "Unit06  :  CVC Makeup", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, FontStyles.Italic).rectTransform, 14f, 4f, 400f, 28f);

            var body = ControlRoomUIFactory.createBorderedBox(mMakeupRunPane, "RunBody", ControlRoomTheme.BgPrimary, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(body, 8f, 36f, 1184f, 830f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(body, "Tanks", "Boric Acid Batch Tank: 100 %     Boric Acid Storage Tank: 100 %\nBAS Supply Pump A: Running (97.2 psia -> 92.1 psia, 19.7 gpm)", 16f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.TopLeft, FontStyles.Bold).rectTransform, 28f, 28f, 920f, 54f);
            mMakeupRunFlowTxt = ControlRoomUIFactory.createLabel(body, "RunFlow", "CVC Makeup Filter/Pump A: 0 % (Off)   |   CVC Makeup Filter/Pump B: Running (312 psia -> 1760 psia, 35% -> 19.5%)", 15.5f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(mMakeupRunFlowTxt.rectTransform, 28f, 120f, 1080f, 34f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(body, "MkPumpAIco", Color.white, mSprites.pumpOff), 420f, 210f, 52f, 52f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(body, "MkPumpBIco", Color.white, mSprites.pumpOn), 420f, 330f, 52f, 52f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(body, "MkPumpALbl", "CVC Makeup Pump A (0%)", 13f, ControlRoomTheme.TextSecondary).rectTransform, 484f, 224f, 220f, 24f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(body, "MkPumpBLbl", "CVC Makeup Pump B (Running)", 13f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.MidlineLeft, FontStyles.Bold).rectTransform, 484f, 344f, 240f, 24f);

            mMakeupRunCurLvlTxt = ControlRoomUIFactory.createLabel(body, "CurLvl", "Current Level: 57.6 %   ->   Target Level: 59.92 %", 18f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(mMakeupRunCurLvlTxt.rectTransform, 28f, 640f, 700f, 34f);
            mMakeupRunStatusBanner = ControlRoomUIFactory.createLabel(body, "Banner", "Aligning Boric Acid System", 18f, ControlRoomTheme.TabActiveGreen, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(mMakeupRunStatusBanner.rectTransform, 28f, 700f, 700f, 34f);

            var rFoot = ControlRoomUIFactory.createPanel(mMakeupRunPane, "RunFoot", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(rFoot, 0f, 876f, 1200f, 52f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(rFoot, "CanTxt", "Cancel Automation?", 15f, ControlRoomTheme.TextPrimary).rectTransform, 894f, 12f, 160f, 26f);
            ControlRoomUIFactory.createE2Control(rFoot, "BtnCancelAuto", 1068f, 8f, 110f, 36f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "Cancel", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true);

            mMakeupRunPane.gameObject.SetActive(false);
        }

        // =====================================================================
        // 화면 7: 0A : Primary > (RCCW) Reactor Component Cooling (Overview)
        // =====================================================================
        private void buildRccwScreen(RectTransform host)
        {
            mRccwRoot = ControlRoomUIFactory.createPanel(host, "Screen_RCCW", ControlRoomTheme.BgPrimary);
            ControlRoomUIFactory.setTopLeftRect(mRccwRoot, 0f, 0f, 1590f, ControlRoomTheme.ContentAreaHeight);

            var pid = ControlRoomUIFactory.createPanel(mRccwRoot, "Pid", Color.white, mSprites.diagRccw);
            ControlRoomUIFactory.setTopLeftRect(pid, 0f, 0f, 1590f, ControlRoomTheme.ContentAreaHeight);
            if (mSprites.diagRccw == null) pid.GetComponent<Image>().color = ControlRoomTheme.BgPrimary;

            RccwExpTankWidget = ControlRoomUIFactory.createE2Control(
                mRccwRoot, "BtnRccwExpTank", 112f, 224f, 146f, 194f,
                new Color(0f, 0f, 0f, 0.05f), ControlRoomTheme.AccentBlue,
                null,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_54_RccwOpened,
                onClickRccwExpTank,
                null, 14f, null, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);
            mAllWidgets.Add(RccwExpTankWidget);

            mRccwTankLvlTxt = ControlRoomUIFactory.createLabel(mRccwRoot, "RccwTankLvl", "21.7 %", 18.5f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(mRccwTankLvlTxt.rectTransform, 266f, 250f, 110f, 30f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(mRccwRoot, "RccwPumpA", Color.white, mSprites.pumpOn), 344f, 444f, 52f, 52f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(mRccwRoot, "RccwPumpB", Color.white, mSprites.pumpOff), 344f, 566f, 52f, 52f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mRccwRoot, "RccwMetrics", "72.1 psia\n74.9 °F     66.3 °F\n440 gpm", 16f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.TopLeft, FontStyles.Bold).rectTransform, 410f, 232f, 240f, 84f);

            mRccwRoot.gameObject.SetActive(false);
        }

        // =====================================================================
        // 우측 공용 패널 (Processes / Active+Completed + Details) 및 상단 3단계 드롭다운
        // =====================================================================
        private void buildSharedRightPane(RectTransform host)
        {
            mSharedRightPane = ControlRoomUIFactory.createPanel(host, "SharedRightPane", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(mSharedRightPane, 1592f, 0f, 328f, ControlRoomTheme.ContentAreaHeight);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(host, "DivRight", ControlRoomTheme.BorderSubtle), 1590.5f, 0f, 1.5f, ControlRoomTheme.ContentAreaHeight);

            mRightTopHdrTxt = ControlRoomUIFactory.createLabel(mSharedRightPane, "ProcHdr", "Processes", 16f, ControlRoomTheme.TextPrimary);
            ControlRoomUIFactory.setTopLeftRect(mRightTopHdrTxt.rectTransform, 12f, 6f, 300f, 28f);

            mMakeupActiveCard = ControlRoomUIFactory.createBorderedBox(mSharedRightPane, "Card_CvcMakeup", ControlRoomTheme.BgDarkPane, ControlRoomTheme.HighlightBlue, 1.5f);
            ControlRoomUIFactory.setTopLeftRect(mMakeupActiveCard, 6f, 40f, 316f, 114f);
            var cIn = mMakeupActiveCard.Find("Inner");
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(cIn, "Title", "Unit06 : CVC Makeup", 15f, ControlRoomTheme.TextPrimary).rectTransform, 10f, 6f, 290f, 24f);
            var bar = ControlRoomUIFactory.createBorderedBox(cIn, "Bar", ControlRoomTheme.BgDarkest, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(bar, 10f, 34f, 292f, 14f);
            mMakeupCardProgFill = ControlRoomUIFactory.createPanel(bar.Find("Inner"), "Fill", ControlRoomTheme.HighlightBlue);
            mMakeupCardProgFill.anchorMin = Vector2.zero; mMakeupCardProgFill.anchorMax = new Vector2(0.167f, 1f);
            mMakeupCardStatusTxt = ControlRoomUIFactory.createLabel(cIn, "StatusMsg", "Aligning Boric Acid System", 13.5f, ControlRoomTheme.TabActiveGreen);
            ControlRoomUIFactory.setTopLeftRect(mMakeupCardStatusTxt.rectTransform, 10f, 56f, 292f, 22f);
            mMakeupCardProgTxt = ControlRoomUIFactory.createLabel(cIn, "ProgTxt", "Status: <color=#3EE890>Active</color>  Progress: 16.7 %", 14f, ControlRoomTheme.HighlightBlue);
            ControlRoomUIFactory.setTopLeftRect(mMakeupCardProgTxt.rectTransform, 10f, 82f, 292f, 24f);
            mMakeupActiveCard.gameObject.SetActive(false);

            mDetDividerRt = ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(mSharedRightPane, "DetLine", ControlRoomTheme.BorderSubtle), 6f, 470f, 316f, 1.5f);
            mDetHdrTxt = ControlRoomUIFactory.createLabel(mSharedRightPane, "DetHdr", "Details", 16f, ControlRoomTheme.TextPrimary);
            ControlRoomUIFactory.setTopLeftRect(mDetHdrTxt.rectTransform, 12f, 478f, 180f, 26f);

            mDetailsStandardGroup = ControlRoomUIFactory.createPanel(mSharedRightPane, "DetStd", Color.clear);
            ControlRoomUIFactory.setTopLeftRect(mDetailsStandardGroup, 12f, 514f, 304f, 360f);
            mDetTagTxt = addDetailRow(mDetailsStandardGroup, "Tag:", 0f);
            mDetDescTxt = addDetailRow(mDetailsStandardGroup, "Description:", 44f, 76f);
            mDetValTxt = addDetailRow(mDetailsStandardGroup, "Value:", 130f);
            mDetPosTxt = addDetailRow(mDetailsStandardGroup, "Position:", 170f);
            mDetStateTxt = addDetailRow(mDetailsStandardGroup, "State:", 210f);

            mDetailsRecircControlGroup = ControlRoomUIFactory.createPanel(mSharedRightPane, "DetRecircCtrl", Color.clear);
            ControlRoomUIFactory.setTopLeftRect(mDetailsRecircControlGroup, 12f, 514f, 304f, 260f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mDetailsRecircControlGroup, "Hdr", "CVC Recirculation Control", 15.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center).rectTransform, 0f, 8f, 300f, 28f);
            mDetRecircLockImg = ControlRoomUIFactory.createPanel(mDetailsRecircControlGroup, "LockIco", Color.white, mSprites.lockClosed).GetComponent<Image>();
            ControlRoomUIFactory.setTopLeftRect(mDetRecircLockImg.rectTransform, 48f, 54f, 34f, 34f);
            mDetRecircLockImg.preserveAspect = true;
            mDetRecircModeTxt = ControlRoomUIFactory.createLabel(mDetailsRecircControlGroup, "Mode", "Recirculation", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            ControlRoomUIFactory.setTopLeftRect(mDetRecircModeTxt.rectTransform, 94f, 56f, 180f, 30f);

            RecircSwitchModesWidget = ControlRoomUIFactory.createE2Control(
                mDetailsRecircControlGroup, "BtnSwitchModes", 72f, 114f, 160f, 38f,
                ControlRoomTheme.BgButton, ControlRoomTheme.ClickPressBlue,
                null,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_37_RecircLockSelected,
                onClickRecircSwitchModes,
                "Switch Modes", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);
            mAllWidgets.Add(RecircSwitchModesWidget);
            mDetailsRecircControlGroup.gameObject.SetActive(false);

            ControlRoomUIFactory.createE2Control(mSharedRightPane, "BtnDetClear", 12f, 922f, 76f, 34f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "Clear", 14.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);
        }

        private TextMeshProUGUI addDetailRow(RectTransform parent, string label, float y, float h = 34f)
        {
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(parent, label + "_K", label, 13.5f, ControlRoomTheme.TextSecondary).rectTransform, 0f, y, 92f, 24f);
            var val = ControlRoomUIFactory.createLabel(parent, label + "_V", "--", 13.5f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.TopLeft);
            ControlRoomUIFactory.setTopLeftRect(val.rectTransform, 94f, y, 210f, h);
            return val;
        }

        private void buildTopDropdownOverlays(RectTransform root)
        {
            // 1. Unit 드롭다운 (영상 f_337 실측: 01~12, 00, 0A, 0B 전체 15개 호기)
            string[] allUnits = { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "00", "0A", "0B" };
            mUnitDropdown = ControlRoomUIFactory.createBorderedBox(root, "Dropdown_Unit", ControlRoomTheme.BgDarkest, ControlRoomTheme.HighlightBlue, 1.5f);
            ControlRoomUIFactory.setTopLeftRect(mUnitDropdown, 214f, 52f, 118f, allUnits.Length * 30f + 8f);
            var uIn = mUnitDropdown.Find("Inner");
            for (int i = 0; i < allUnits.Length; i++)
            {
                bool is0A = allUnits[i] == "0A";
                var uw = ControlRoomUIFactory.createE2Control(
                    uIn, "Opt_" + allUnits[i], 2f, 4f + i * 30f, 111f, 28f,
                    ControlRoomTheme.BgDarkPane, is0A ? ControlRoomTheme.ClickPressBlue : ControlRoomTheme.BgButton,
                    null,
                    is0A ? (Func<bool>)(() => mModel.CurrentStep == Scenario2SequenceStep.S2_50_UnitDropdownFor0A) : (() => false),
                    is0A ? onClickUnit0AOption : null,
                    "  " + allUnits[i], 15f, is0A ? ControlRoomTheme.TextPrimary : ControlRoomTheme.TextSecondary, TextAlignmentOptions.MidlineLeft);
                if (is0A) Unit0AWidget = uw;
                mAllWidgets.Add(uw);
            }
            mUnitDropdown.gameObject.SetActive(false);

            // 2. Category 드롭다운 (영상 f_338 실측: Electrical, I&C, Primary, Secondary, Special, Transient)
            string[] cats = { "Electrical", "I&C", "Primary", "Secondary", "Special", "Transient" };
            mCategoryDropdown = ControlRoomUIFactory.createBorderedBox(root, "Dropdown_Category", ControlRoomTheme.BgDarkest, ControlRoomTheme.HighlightBlue, 1.5f);
            ControlRoomUIFactory.setTopLeftRect(mCategoryDropdown, 350f, 52f, 152f, cats.Length * 32f + 8f);
            var cIn = mCategoryDropdown.Find("Inner");
            for (int i = 0; i < cats.Length; i++)
            {
                bool isPrimary = cats[i] == "Primary";
                var cw = ControlRoomUIFactory.createE2Control(
                    cIn, "Opt_" + cats[i], 2f, 4f + i * 32f, 145f, 30f,
                    ControlRoomTheme.BgDarkPane, isPrimary ? ControlRoomTheme.ClickPressBlue : ControlRoomTheme.BgButton,
                    null,
                    isPrimary ? (Func<bool>)(() =>
                        mModel.CurrentStep == Scenario2SequenceStep.S2_01_StartCatMenuOpen
                        || mModel.CurrentStep == Scenario2SequenceStep.S2_33_CatMenuForRecircStop
                        || mModel.CurrentStep == Scenario2SequenceStep.S2_45_CatMenuForCeRecheck
                        || mModel.CurrentStep == Scenario2SequenceStep.S2_52_Unit0ACatMenuOpen)
                    : (() => false),
                    isPrimary ? onClickCategoryPrimaryOption : null,
                    "  " + cats[i], 14.5f, isPrimary ? ControlRoomTheme.TextPrimary : ControlRoomTheme.TextSecondary, TextAlignmentOptions.MidlineLeft);
                if (isPrimary) CatPrimaryWidget = cw;
                mAllWidgets.Add(cw);
            }
            mCategoryDropdown.gameObject.SetActive(false);

            // 3. Primary Screen 드롭다운 (Unit 06 vs Unit 0A 전환 분기)
            mPrimaryScreenDropdown = ControlRoomUIFactory.createBorderedBox(root, "Dropdown_PrimaryScreen", ControlRoomTheme.BgDarkest, ControlRoomTheme.HighlightBlue, 1.5f);
            ControlRoomUIFactory.setTopLeftRect(mPrimaryScreenDropdown, 514f, 52f, 360f, 108f);
            var pIn = mPrimaryScreenDropdown.Find("Inner");

            mPrimary06Group = ControlRoomUIFactory.createPanel(pIn, "Group06", Color.clear);
            PrimCeWidget = ControlRoomUIFactory.createE2Control(
                mPrimary06Group, "Opt_CE", 2f, 4f, 353f, 30f,
                ControlRoomTheme.BgDarkPane, ControlRoomTheme.ClickPressBlue,
                null,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_02_StartPrimaryMenuOpen
                      || mModel.CurrentStep == Scenario2SequenceStep.S2_13_CvcToCeMenuOpen
                      || mModel.CurrentStep == Scenario2SequenceStep.S2_46_PrimaryMenuForCeRecheck,
                onClickPrimaryCeOption,
                "  (CE) Containment Evacuation", 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            mAllWidgets.Add(PrimCeWidget);

            PrimCvcWidget = ControlRoomUIFactory.createE2Control(
                mPrimary06Group, "Opt_CVC", 2f, 36f, 353f, 30f,
                ControlRoomTheme.BgDarkPane, ControlRoomTheme.ClickPressBlue,
                null,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_10_CeToCvcMenuOpen
                      || mModel.CurrentStep == Scenario2SequenceStep.S2_15_CeToCvcForHeatingMenu
                      || mModel.CurrentStep == Scenario2SequenceStep.S2_34_PrimaryMenuForRecircStop,
                onClickPrimaryCvcOption,
                "  (CVC) Chemical & Volume Control", 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            mAllWidgets.Add(PrimCvcWidget);
            mAllWidgets.Add(ControlRoomUIFactory.createE2Control(mPrimary06Group, "Opt_RCCW06", 2f, 68f, 353f, 30f, ControlRoomTheme.BgDarkPane, ControlRoomTheme.BgButton, null, () => false, null, "  (RCCW) Reactor Component Cooling (Unit)", 14f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.MidlineLeft));

            mPrimary0AGroup = ControlRoomUIFactory.createPanel(pIn, "Group0A", Color.clear);
            mAllWidgets.Add(ControlRoomUIFactory.createE2Control(mPrimary0AGroup, "Opt_CFD", 2f, 4f, 353f, 30f, ControlRoomTheme.BgDarkPane, ControlRoomTheme.BgButton, null, () => false, null, "  (CFD) Containment Flood & Drain", 14f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.MidlineLeft));
            mAllWidgets.Add(ControlRoomUIFactory.createE2Control(mPrimary0AGroup, "Opt_MHS", 2f, 36f, 353f, 30f, ControlRoomTheme.BgDarkPane, ControlRoomTheme.BgButton, null, () => false, null, "  (MHS) Module Heating", 14f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.MidlineLeft));
            Prim0ARccwWidget = ControlRoomUIFactory.createE2Control(
                mPrimary0AGroup, "Opt_RCCW0A", 2f, 68f, 353f, 30f,
                ControlRoomTheme.BgDarkPane, ControlRoomTheme.ClickPressBlue,
                null,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_53_Unit0APrimaryMenuOpen,
                onClickPrimary0ARccwOption,
                "  (RCCW) Reactor Component Cooling (Overview)", 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            mAllWidgets.Add(Prim0ARccwWidget);
            mPrimaryScreenDropdown.gameObject.SetActive(false);

            // 4. CVC SubScreen 드롭다운 (Demins & Letdowns / Heating & Spray / Recirc & Makeup)
            mCvcSubDropdown = ControlRoomUIFactory.createBorderedBox(root, "Dropdown_CvcSub", ControlRoomTheme.BgDarkest, ControlRoomTheme.HighlightBlue, 1.5f);
            ControlRoomUIFactory.setTopLeftRect(mCvcSubDropdown, 514f, 52f, 240f, 108f);
            var sIn = mCvcSubDropdown.Find("Inner");
            SubDeminsWidget = ControlRoomUIFactory.createE2Control(
                sIn, "Opt_Demins", 2f, 4f, 233f, 30f,
                ControlRoomTheme.BgDarkPane, ControlRoomTheme.ClickPressBlue,
                null,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_22_CvcSubMenuForDemins,
                onClickSubDeminsOption,
                "  Demins & Letdowns", 14.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            mAllWidgets.Add(SubDeminsWidget);
            SubHeatingWidget = ControlRoomUIFactory.createE2Control(
                sIn, "Opt_Heating", 2f, 36f, 233f, 30f,
                ControlRoomTheme.BgDarkPane, ControlRoomTheme.ClickPressBlue,
                null,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_16_CvcSubMenuForHeating,
                onClickSubHeatingOption,
                "  Heating & Spray", 14.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            mAllWidgets.Add(SubHeatingWidget);
            SubRecircWidget = ControlRoomUIFactory.createE2Control(
                sIn, "Opt_Recirc", 2f, 68f, 233f, 30f,
                ControlRoomTheme.BgDarkPane, ControlRoomTheme.ClickPressBlue,
                null,
                () => mModel.CurrentStep == Scenario2SequenceStep.S2_11_CvcSubMenuForRecirc
                      || mModel.CurrentStep == Scenario2SequenceStep.S2_35_CvcSubMenuForRecircStop,
                onClickSubRecircOption,
                "  Recirc & Makeup", 14.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            mAllWidgets.Add(SubRecircWidget);
            mCvcSubDropdown.gameObject.SetActive(false);

            // 5. 우측 상단 경보(Alarm) 드롭다운 (영상 f_192.jpg 실측: 3행, 닫기 버튼 없이 상단 배지 재클릭으로 닫힘)
            mAlarmDropdown = ControlRoomUIFactory.createBorderedBox(root, "Dropdown_Alarms", ControlRoomTheme.BgDarkest, ControlRoomTheme.HighlightBlue, 1.8f);
            ControlRoomUIFactory.setTopLeftRect(mAlarmDropdown, 1476f, 56f, 428f, 224f);
            var aIn = mAlarmDropdown.Find("Inner");
            mAlarmRow0Txt = addAlarmDropdownRow(aIn, 10f, ControlRoomTheme.StatusAlarm);
            mAlarmRow1Txt = addAlarmDropdownRow(aIn, 78f, ControlRoomTheme.StatusAlarm);
            mAlarmRow2Txt = addAlarmDropdownRow(aIn, 146f, ControlRoomTheme.StatusCaution);
            mAlarmDropdown.gameObject.SetActive(false);
        }

        private TextMeshProUGUI addAlarmDropdownRow(Transform parent, float y, Color badgeColor)
        {
            var card = ControlRoomUIFactory.createBorderedBox(parent, $"AlmRow_{y}", ControlRoomTheme.BgPanel, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(card, 8f, y, 408f, 62f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(card.Find("Inner"), "Bar", badgeColor), 0f, 0f, 6f, 60f);
            var lbl = ControlRoomUIFactory.createLabel(card.Find("Inner"), "T", "", 12.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            ControlRoomUIFactory.setTopLeftRect(lbl.rectTransform, 14f, 4f, 386f, 54f);
            return lbl;
        }

        // =====================================================================
        // 헬퍼: 태그 라벨 흰 박스, 밸브 버튼, 밸브 3줄 컨텍스트 팝업
        // =====================================================================
        private void addPidTagBadge(RectTransform parent, string text, float x, float y, float w, float h)
        {
            if (string.IsNullOrEmpty(text)) return;
            var box = ControlRoomUIFactory.createBorderedBox(parent, "Tag_" + text.Replace("\n", "_"), new Color(0.95f, 0.96f, 0.98f, 0.94f), ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(box, x, y, w, h);
            var lbl = ControlRoomUIFactory.createLabel(box.Find("Inner"), "L", text, 10f, new Color(0.08f, 0.1f, 0.14f, 1f), TextAlignmentOptions.Center, FontStyles.Bold);
            ControlRoomUIFactory.setOffsets(lbl.rectTransform, 1f, 1f, -1f, -1f);
        }

        private E2InteractiveWidget createValveControl(
            RectTransform parent, string name, float x, float y, float size,
            Func<bool> isIntended, Action onClick, out Image iconImg)
        {
            var widget = ControlRoomUIFactory.createE2Control(
                parent, name, x, y, size, size,
                Color.clear, ControlRoomTheme.AccentBlue,
                null, isIntended, () => onClick?.Invoke());
            mAllWidgets.Add(widget);
            iconImg = ControlRoomUIFactory.createPanel(widget.transform, "Icon", Color.white, mSprites.valveOpen).GetComponent<Image>();
            iconImg.preserveAspect = true;
            return widget;
        }

        private RectTransform createValvePopup(
            RectTransform parent, string name, float x, float y, string tagTitle, bool openAction,
            Func<bool> isIntended, Action onActionClick, out E2InteractiveWidget actionWidget)
        {
            var box = ControlRoomUIFactory.createBorderedBox(parent, name, ControlRoomTheme.BgDarkest, ControlRoomTheme.HighlightBlue, 1.5f);
            ControlRoomUIFactory.setTopLeftRect(box, x, y, 168f, 114f);
            var inner = box.Find("Inner");
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(inner, "Tag", tagTitle, 12.5f, ControlRoomTheme.TextSecondary).rectTransform, 8f, 4f, 152f, 20f);

            var openBtn = ControlRoomUIFactory.createE2Control(
                inner, "BtnOpen", 4f, 26f, 158f, 26f,
                ControlRoomTheme.BgDarkPane, ControlRoomTheme.ClickPressBlue,
                null,
                openAction ? isIntended : (() => false),
                openAction ? () => onActionClick?.Invoke() : null,
                "  Open", 14f, openAction ? ControlRoomTheme.TextPrimary : ControlRoomTheme.TextSecondary, TextAlignmentOptions.MidlineLeft);
            mAllWidgets.Add(openBtn);

            var closeBtn = ControlRoomUIFactory.createE2Control(
                inner, "BtnClose", 4f, 54f, 158f, 26f,
                ControlRoomTheme.BgDarkPane, ControlRoomTheme.ClickPressBlue,
                null,
                !openAction ? isIntended : (() => false),
                !openAction ? () => onActionClick?.Invoke() : null,
                "  Close", 14f, !openAction ? ControlRoomTheme.TextPrimary : ControlRoomTheme.TextSecondary, TextAlignmentOptions.MidlineLeft);
            mAllWidgets.Add(closeBtn);

            mAllWidgets.Add(ControlRoomUIFactory.createE2Control(
                inner, "BtnSetPos", 4f, 82f, 158f, 26f,
                ControlRoomTheme.BgDarkPane, ControlRoomTheme.BgButton,
                null, () => false, null,
                "  Set Position", 13.5f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.MidlineLeft));

            actionWidget = openAction ? openBtn : closeBtn;
            box.gameObject.SetActive(false);
            return box;
        }

        // =====================================================================
        // 의도된 단계 전이 핸들러 (S2_00 -> S2_55)
        // =====================================================================
        private void onClickCategoryBox()
        {
            if (mModel.CurrentStep == Scenario2SequenceStep.S2_00_ProcLibInitial) mModel.advanceStep(Scenario2SequenceStep.S2_01_StartCatMenuOpen);
            else if (mModel.CurrentStep == Scenario2SequenceStep.S2_32_ModOverviewBeforeRecirc) mModel.advanceStep(Scenario2SequenceStep.S2_33_CatMenuForRecircStop);
            else if (mModel.CurrentStep == Scenario2SequenceStep.S2_44_IsoValvesChecked) mModel.advanceStep(Scenario2SequenceStep.S2_45_CatMenuForCeRecheck);
            else if (mModel.CurrentStep == Scenario2SequenceStep.S2_51_Unit0ASelected) mModel.advanceStep(Scenario2SequenceStep.S2_52_Unit0ACatMenuOpen);
        }

        private void onClickCategoryPrimaryOption()
        {
            if (mModel.CurrentStep == Scenario2SequenceStep.S2_01_StartCatMenuOpen) mModel.advanceStep(Scenario2SequenceStep.S2_02_StartPrimaryMenuOpen);
            else if (mModel.CurrentStep == Scenario2SequenceStep.S2_33_CatMenuForRecircStop) mModel.advanceStep(Scenario2SequenceStep.S2_34_PrimaryMenuForRecircStop);
            else if (mModel.CurrentStep == Scenario2SequenceStep.S2_45_CatMenuForCeRecheck) mModel.advanceStep(Scenario2SequenceStep.S2_46_PrimaryMenuForCeRecheck);
            else if (mModel.CurrentStep == Scenario2SequenceStep.S2_52_Unit0ACatMenuOpen) mModel.advanceStep(Scenario2SequenceStep.S2_53_Unit0APrimaryMenuOpen);
        }

        private void onClickScreenBox()
        {
            if (mModel.CurrentStep == Scenario2SequenceStep.S2_09_CeRecheckBeforeCvc) mModel.advanceStep(Scenario2SequenceStep.S2_10_CeToCvcMenuOpen);
            else if (mModel.CurrentStep == Scenario2SequenceStep.S2_12_CvcRecircChecked) mModel.advanceStep(Scenario2SequenceStep.S2_13_CvcToCeMenuOpen);
            else if (mModel.CurrentStep == Scenario2SequenceStep.S2_14_CeCheckedForTrip) mModel.advanceStep(Scenario2SequenceStep.S2_15_CeToCvcForHeatingMenu);
            else if (mModel.CurrentStep == Scenario2SequenceStep.S2_21_CvcsValvesClosed) mModel.advanceStep(Scenario2SequenceStep.S2_22_CvcSubMenuForDemins);
        }

        private void onClickPrimaryCeOption()
        {
            if (mModel.CurrentStep == Scenario2SequenceStep.S2_02_StartPrimaryMenuOpen) mModel.advanceStep(Scenario2SequenceStep.S2_03_CeInitialAlarm);
            else if (mModel.CurrentStep == Scenario2SequenceStep.S2_13_CvcToCeMenuOpen) mModel.advanceStep(Scenario2SequenceStep.S2_14_CeCheckedForTrip);
            else if (mModel.CurrentStep == Scenario2SequenceStep.S2_46_PrimaryMenuForCeRecheck) mModel.advanceStep(Scenario2SequenceStep.S2_47_CeLeakRechecked);
        }

        private void onClickPrimaryCvcOption()
        {
            if (mModel.CurrentStep == Scenario2SequenceStep.S2_10_CeToCvcMenuOpen) mModel.advanceStep(Scenario2SequenceStep.S2_11_CvcSubMenuForRecirc);
            else if (mModel.CurrentStep == Scenario2SequenceStep.S2_15_CeToCvcForHeatingMenu) mModel.advanceStep(Scenario2SequenceStep.S2_16_CvcSubMenuForHeating);
            else if (mModel.CurrentStep == Scenario2SequenceStep.S2_34_PrimaryMenuForRecircStop) mModel.advanceStep(Scenario2SequenceStep.S2_35_CvcSubMenuForRecircStop);
        }

        private void onClickPrimary0ARccwOption()
        {
            if (mModel.CurrentStep == Scenario2SequenceStep.S2_53_Unit0APrimaryMenuOpen) mModel.advanceStep(Scenario2SequenceStep.S2_54_RccwOpened);
        }

        private void onClickSubRecircOption()
        {
            if (mModel.CurrentStep == Scenario2SequenceStep.S2_11_CvcSubMenuForRecirc) mModel.advanceStep(Scenario2SequenceStep.S2_12_CvcRecircChecked);
            else if (mModel.CurrentStep == Scenario2SequenceStep.S2_35_CvcSubMenuForRecircStop) mModel.advanceStep(Scenario2SequenceStep.S2_36_RecircForPumpStop);
        }

        private void onClickSubHeatingOption() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_16_CvcSubMenuForHeating) mModel.advanceStep(Scenario2SequenceStep.S2_17_HeatingSprayOpened); }
        private void onClickSubDeminsOption() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_22_CvcSubMenuForDemins) mModel.advanceStep(Scenario2SequenceStep.S2_23_DeminsChecked); }

        private void onClickCeAov0130() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_03_CeInitialAlarm) mModel.advanceStep(Scenario2SequenceStep.S2_04_Ce0130PopupOpen); }
        private void onClickCe0130Open() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_04_Ce0130PopupOpen) mModel.advanceStep(Scenario2SequenceStep.S2_05_Ce0130Opened); }
        private void onClickCeAov0128() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_05_Ce0130Opened) mModel.advanceStep(Scenario2SequenceStep.S2_06_Ce0128PopupOpen); }
        private void onClickCe0128Close() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_06_Ce0128PopupOpen) mModel.advanceStep(Scenario2SequenceStep.S2_07_CeFlowRerouted); }
        private void onClickCeSampleVessel() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_47_CeLeakRechecked) mModel.advanceStep(Scenario2SequenceStep.S2_48_CeSampleChecked); }

        private void onClickBackNav() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_08_ModOverviewChecked) mModel.advanceStep(Scenario2SequenceStep.S2_09_CeRecheckBeforeCvc); }

        private void onClickBottomUnitOverview()
        {
            if (mModel.CurrentStep == Scenario2SequenceStep.S2_07_CeFlowRerouted) mModel.advanceStep(Scenario2SequenceStep.S2_08_ModOverviewChecked);
            else if (mModel.CurrentStep == Scenario2SequenceStep.S2_31_AlarmChecked) mModel.advanceStep(Scenario2SequenceStep.S2_32_ModOverviewBeforeRecirc);
            else if (mModel.CurrentStep == Scenario2SequenceStep.S2_40_RecircPumpsStopped) mModel.advanceStep(Scenario2SequenceStep.S2_41_ModOverviewForIso);
            else if (mModel.CurrentStep == Scenario2SequenceStep.S2_48_CeSampleChecked) mModel.advanceStep(Scenario2SequenceStep.S2_49_ModOverviewRechecked);
        }

        private void onClickMov0318() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_17_HeatingSprayOpened) mModel.advanceStep(Scenario2SequenceStep.S2_18_Mov0318PopupOpen); }
        private void onClickMov0318Close() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_18_Mov0318PopupOpen) mModel.advanceStep(Scenario2SequenceStep.S2_19_Mov0318Closed); }
        private void onClickAov0339() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_19_Mov0318Closed) mModel.advanceStep(Scenario2SequenceStep.S2_20_Aov0339PopupOpen); }
        private void onClickAov0339Close() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_20_Aov0339PopupOpen) mModel.advanceStep(Scenario2SequenceStep.S2_21_CvcsValvesClosed); }

        private void onClickBottomProcessLibrary() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_23_DeminsChecked) mModel.advanceStep(Scenario2SequenceStep.S2_24_ProcLibOpened); }
        private void onClickProcLibAutomationsTab() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_24_ProcLibOpened) mModel.advanceStep(Scenario2SequenceStep.S2_25_AutomationsTabOpened); }
        private void onClickProcLibSearch() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_25_AutomationsTabOpened) mModel.advanceStep(Scenario2SequenceStep.S2_26_CvcAutoSearched); }
        private void onClickTreeCvcMakeup() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_26_CvcAutoSearched) mModel.advanceStep(Scenario2SequenceStep.S2_27_CvcMakeupSetup); }
        private void onClickMakeupValidate() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_27_CvcMakeupSetup) mModel.advanceStep(Scenario2SequenceStep.S2_28_CvcMakeupValidated); }
        private void onClickMakeupExecute() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_28_CvcMakeupValidated) mModel.advanceStep(Scenario2SequenceStep.S2_29_CvcMakeupRunning); }

        private void onClickAlarmBadge()
        {
            if (mModel.CurrentStep == Scenario2SequenceStep.S2_29_CvcMakeupRunning) mModel.advanceStep(Scenario2SequenceStep.S2_30_AlarmDropdownOpen);
            else if (mModel.CurrentStep == Scenario2SequenceStep.S2_30_AlarmDropdownOpen) mModel.advanceStep(Scenario2SequenceStep.S2_31_AlarmChecked);
        }

        private void onClickRecircLock() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_36_RecircForPumpStop) mModel.advanceStep(Scenario2SequenceStep.S2_37_RecircLockSelected); }
        private void onClickRecircSwitchModes() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_37_RecircLockSelected) mModel.advanceStep(Scenario2SequenceStep.S2_38_RecircManualMode); }
        private void onClickRecircPumpA() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_38_RecircManualMode) mModel.advanceStep(Scenario2SequenceStep.S2_39_PumpAPopupOpen); }
        private void onClickPumpAStop() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_39_PumpAPopupOpen) mModel.advanceStep(Scenario2SequenceStep.S2_40_RecircPumpsStopped); }

        private void onClickModIsoTab() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_41_ModOverviewForIso) mModel.advanceStep(Scenario2SequenceStep.S2_42_IsoTabOpened); }
        private void onClickIsoShowAll() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_42_IsoTabOpened) mModel.advanceStep(Scenario2SequenceStep.S2_43_IsoShowAllActive); }
        private void onClickIsoCvcCheck() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_43_IsoShowAllActive) mModel.advanceStep(Scenario2SequenceStep.S2_44_IsoValvesChecked); }

        private void onClickUnitBox() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_49_ModOverviewRechecked) mModel.advanceStep(Scenario2SequenceStep.S2_50_UnitDropdownFor0A); }
        private void onClickUnit0AOption() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_50_UnitDropdownFor0A) mModel.advanceStep(Scenario2SequenceStep.S2_51_Unit0ASelected); }
        private void onClickRccwExpTank() { if (mModel.CurrentStep == Scenario2SequenceStep.S2_54_RccwOpened) mModel.advanceStep(Scenario2SequenceStep.S2_55_Scenario2Completed); }

        // =====================================================================
        // 3D 가이드 화살표 타겟 조회 (S2_00 ~ S2_55 전수 대응)
        // =====================================================================
        public RectTransform getCurrentScenarioGuideTargetRect()
        {
            try
            {
                switch (mModel.CurrentStep)
                {
                    case Scenario2SequenceStep.S2_00_ProcLibInitial: return mChrome.CategoryBoxWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_01_StartCatMenuOpen: return CatPrimaryWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_02_StartPrimaryMenuOpen: return PrimCeWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_03_CeInitialAlarm: return CeAov0130Widget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_04_Ce0130PopupOpen: return Ce0130OpenWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_05_Ce0130Opened: return CeAov0128Widget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_06_Ce0128PopupOpen: return Ce0128CloseWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_07_CeFlowRerouted: return mChrome.UnitOverviewNavWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_08_ModOverviewChecked: return mChrome.BackNavWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_09_CeRecheckBeforeCvc: return mChrome.ScreenBoxWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_10_CeToCvcMenuOpen: return PrimCvcWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_11_CvcSubMenuForRecirc: return SubRecircWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_12_CvcRecircChecked: return mChrome.ScreenBoxWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_13_CvcToCeMenuOpen: return PrimCeWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_14_CeCheckedForTrip: return mChrome.ScreenBoxWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_15_CeToCvcForHeatingMenu: return PrimCvcWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_16_CvcSubMenuForHeating: return SubHeatingWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_17_HeatingSprayOpened: return Mov0318Widget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_18_Mov0318PopupOpen: return Mov0318CloseWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_19_Mov0318Closed: return Aov0339Widget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_20_Aov0339PopupOpen: return Aov0339CloseWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_21_CvcsValvesClosed: return mChrome.ScreenBoxWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_22_CvcSubMenuForDemins: return SubDeminsWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_23_DeminsChecked: return mChrome.ProcessLibraryNavWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_24_ProcLibOpened: return ProcLibAutoTabWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_25_AutomationsTabOpened: return ProcLibSearchWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_26_CvcAutoSearched: return TreeCvcMakeupWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_27_CvcMakeupSetup: return MakeupValidateWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_28_CvcMakeupValidated: return MakeupExecuteWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_29_CvcMakeupRunning: return mChrome.AlarmBadgeWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_30_AlarmDropdownOpen: return mChrome.AlarmBadgeWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_31_AlarmChecked: return mChrome.UnitOverviewNavWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_32_ModOverviewBeforeRecirc: return mChrome.CategoryBoxWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_33_CatMenuForRecircStop: return CatPrimaryWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_34_PrimaryMenuForRecircStop: return PrimCvcWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_35_CvcSubMenuForRecircStop: return SubRecircWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_36_RecircForPumpStop: return RecircLockWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_37_RecircLockSelected: return RecircSwitchModesWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_38_RecircManualMode: return RecircPumpAWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_39_PumpAPopupOpen: return PumpAStopWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_40_RecircPumpsStopped: return mChrome.UnitOverviewNavWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_41_ModOverviewForIso: return ModIsoTabWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_42_IsoTabOpened: return IsoShowAllWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_43_IsoShowAllActive: return IsoCvcCheckWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_44_IsoValvesChecked: return mChrome.CategoryBoxWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_45_CatMenuForCeRecheck: return CatPrimaryWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_46_PrimaryMenuForCeRecheck: return PrimCeWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_47_CeLeakRechecked: return CeSampleLevelWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_48_CeSampleChecked: return mChrome.UnitOverviewNavWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_49_ModOverviewRechecked: return mChrome.UnitBoxWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_50_UnitDropdownFor0A: return Unit0AWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_51_Unit0ASelected: return mChrome.CategoryBoxWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_52_Unit0ACatMenuOpen: return CatPrimaryWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_53_Unit0APrimaryMenuOpen: return Prim0ARccwWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_54_RccwOpened: return RccwExpTankWidget?.GetComponent<RectTransform>();
                    case Scenario2SequenceStep.S2_55_Scenario2Completed:
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Scenario2MainMonitorController] getCurrentScenarioGuideTargetRect 오류: {ex}");
                return null;
            }
        }

        // =====================================================================
        // 전체 UI 동기화
        // =====================================================================
        public void refreshUI()
        {
            try
            {
                var p = mModel.PlantModel;
                var step = mModel.CurrentStep;

                mChrome.updateHeaderAndClock(
                    mModel.CenterHeaderUnitCode,
                    mModel.CenterHeaderCategory,
                    mModel.CenterHeaderScreenTitle,
                    p.ActiveAlarmCount,
                    p.ActiveCautionCount,
                    p.NoticeBadgeCount,
                    null,
                    mSprites);

                var scr = mModel.CurrentMainScreen;
                mCeRoot.gameObject.SetActive(scr == MonitorScreenId.PrimaryCE);
                mModOverviewRoot.gameObject.SetActive(scr == MonitorScreenId.ModuleOverview);
                mCvcRecircRoot.gameObject.SetActive(scr == MonitorScreenId.PrimaryCvcRecirMakeup);
                mCvcHeatingRoot.gameObject.SetActive(scr == MonitorScreenId.PrimaryCvcHeatingSpray);
                mCvcDeminsRoot.gameObject.SetActive(scr == MonitorScreenId.PrimaryCvcDeminsLetdowns);
                mProcLibRoot.gameObject.SetActive(scr == MonitorScreenId.ProcessLibrary);
                mRccwRoot.gameObject.SetActive(scr == MonitorScreenId.PrimaryRccw);

                mSharedRightPane.gameObject.SetActive(true);

                // CE 화면 갱신
                mCe0130Img.sprite = p.getValveOpen("CE-AOV-0130") ? mSprites.valveOpen : mSprites.valveClosed;
                mCe0128Img.sprite = p.getValveOpen("CE-AOV-0128") ? mSprites.valveOpen : mSprites.valveClosed;
                mCe0130Popup.gameObject.SetActive(step == Scenario2SequenceStep.S2_04_Ce0130PopupOpen);
                mCe0128Popup.gameObject.SetActive(step == Scenario2SequenceStep.S2_06_Ce0128PopupOpen);
                mCeTagOverlay.gameObject.SetActive(mModel.IsTagLabelsVisible);
                mCeRt1011Txt.text = $"{mModel.CeRt1011Cps:F1} cps\n0.014 lb/s";
                mCeCnvPressTxt.text = $"{mModel.CnvPressurePsia:F2} psia";
                mCeLeakRateTxt.text = $"{mModel.RcsLeakRateGpm:F2} gpm";
                mCeSampleLevelTxt.text = $"{mModel.CeSampleVesselLevelFt:F3} ft";
                mCeSampleCpsTxt.text = $"{mModel.CeRt1007Cps:F1} cps";
                mCeFilterDpTxt.text = $"{mModel.FilterDpPsi:F3} psi   {mModel.FilterDpPsi:F3} psi   {mModel.FilterDpPsi:F3} psi";

                // Module Overview & Isolation 갱신
                mModCnvPressTxt.text = $"{mModel.CnvPressurePsia:F2} psia";
                mModPzrLevelTxt.text = $"{mModel.PzrLevelPct:F1} %";
                mModPowerTxt.text = "99.7 %";
                mModTaveTxt.text = "544 °F";
                mModFwmsPanel.gameObject.SetActive(!mModel.IsModuleIsoTabSelected);
                mModIsoPanel.gameObject.SetActive(mModel.IsModuleIsoTabSelected);
                mIsoTagOverlay.gameObject.SetActive(mModel.IsIsoShowAllActive);
                if (mIsoCvcISV329Img != null) mIsoCvcISV329Img.sprite = p.getValveOpen("CVC-ISV-0329") ? mSprites.valveOpen : mSprites.valveClosed;
                if (mIsoCvcISV331Img != null) mIsoCvcISV331Img.sprite = p.getValveOpen("CVC-ISV-0331") ? mSprites.valveOpen : mSprites.valveClosed;

                // CVC Recirc & Makeup 갱신
                mRecircPumpAImg.sprite = mModel.IsRecircPumpARunning ? mSprites.pumpOn : mSprites.pumpOff;
                mRecircPumpBImg.sprite = mModel.IsRecircPumpBRunning ? mSprites.pumpOn : mSprites.pumpOff;
                mRecircLockImg.sprite = mModel.IsRecircManualMode ? mSprites.lockOpen : mSprites.lockClosed;
                mRecircModeLbl.text = mModel.IsRecircManualMode ? "Manual" : "Recirculation";
                mPumpAPopup.gameObject.SetActive(step == Scenario2SequenceStep.S2_39_PumpAPopupOpen);
                mRecircTagOverlay.gameObject.SetActive(mModel.IsTagLabelsVisible);

                // CVC Heating & Spray 갱신
                mMov0318Img.sprite = p.getValveOpen("CVC-MOV-0318") ? mSprites.valveOpen : mSprites.valveClosed;
                mAov0339Img.sprite = p.getValveOpen("CVC-AOV-0339") ? mSprites.valveOpen : mSprites.valveClosed;
                mMov0318Popup.gameObject.SetActive(step == Scenario2SequenceStep.S2_18_Mov0318PopupOpen);
                mAov0339Popup.gameObject.SetActive(step == Scenario2SequenceStep.S2_20_Aov0339PopupOpen);
                mHeatingTagOverlay.gameObject.SetActive(mModel.IsTagLabelsVisible);
                mHeatSprayFlowTxt.text = $"{mModel.CvcSprayFlowGpm:F2} gpm";
                mHeatInjFlowTxt.text = $"{mModel.CvcInjectionFlowGpm:F2} gpm";
                mHeatDischFlowTxt.text = $"{mModel.CvcDischargeFlowGpm:F2} gpm";

                // CVC Demins & Letdowns 갱신
                mDeminsFlowTxt.text = "0 gpm\n283 psia\n75 °F";
                mDeminsTagOverlay.gameObject.SetActive(mModel.IsTagLabelsVisible);

                // Process Library (Procedures <-> Automations) 갱신
                bool inAuto = step >= Scenario2SequenceStep.S2_25_AutomationsTabOpened;
                mProcTabTxt.color = inAuto ? ControlRoomTheme.TextPrimary : ControlRoomTheme.TabActiveGreen;
                mAutoTabTxt.color = inAuto ? ControlRoomTheme.TabActiveGreen : ControlRoomTheme.TextPrimary;
                mProcTreePane.gameObject.SetActive(!inAuto);
                mAutoLeftPane.gameObject.SetActive(inAuto);

                mAutoSearchInputTxt.text = mModel.IsCvcSearchApplied ? "cvc" : "";
                mAutoTreeRootPane.gameObject.SetActive(inAuto && !mModel.IsCvcSearchApplied);
                mCvcSubTreePane.gameObject.SetActive(inAuto && mModel.IsCvcSearchApplied);

                bool showMakeupSetup = inAuto && (step >= Scenario2SequenceStep.S2_27_CvcMakeupSetup && step <= Scenario2SequenceStep.S2_28_CvcMakeupValidated);
                bool showMakeupRun = inAuto && step >= Scenario2SequenceStep.S2_29_CvcMakeupRunning;
                mMakeupEmptyPane.gameObject.SetActive(!showMakeupSetup && !showMakeupRun);
                mMakeupSetupPane.gameObject.SetActive(showMakeupSetup);
                mMakeupRunPane.gameObject.SetActive(showMakeupRun);

                mMakeupBoricDot.gameObject.SetActive(!mModel.IsCvcMakeupBlendSelected);
                mMakeupBlendRadioDot.gameObject.SetActive(mModel.IsCvcMakeupBlendSelected);
                mMakeupSetupPzrTxt.text = $"Pressurizer Level:  {mModel.PzrLevelPct:F1} %\nRecommended Target Level:  59.92 %";
                mMakeupSummaryTxt.text = $"Boric Acid Only Makeup\nRecommended Pressurizer Level of 59.92%\nTarget Level: 59.92 %\nStatus: {(mModel.IsCvcMakeupValidated ? "Validated" : "Pending")}";
                mMakeupReadyTxt.gameObject.SetActive(mModel.IsCvcMakeupValidated);

                mMakeupRunCurLvlTxt.text = $"Current Level: {mModel.PzrLevelPct:F1} %   ->   Target Level: 59.92 %";
                mMakeupRunStatusBanner.text = string.IsNullOrEmpty(mModel.CvcMakeupStatusText) ? "Aligning Boric Acid System" : mModel.CvcMakeupStatusText;

                // RCCW 갱신
                mRccwTankLvlTxt.text = $"{mModel.RccwExpansionTankLevelPct:F1} %";

                // 공용 우측 패널 (Process Library일 때는 Active / Completed 탭 + 0A에서는 Unit06 카드 숨김!)
                bool isProcLibScreen = scr == MonitorScreenId.ProcessLibrary;
                mRightTopHdrTxt.text = isProcLibScreen ? "<color=#4CE896>Active</color>      Completed" : "Processes";
                bool showCard = mModel.IsCvcMakeupRunning && mModel.CenterHeaderUnitCode == "06";
                mMakeupActiveCard.gameObject.SetActive(showCard);
                if (showCard)
                {
                    mMakeupCardProgFill.anchorMax = new Vector2(Mathf.Clamp01(mModel.CvcMakeupProgressPct / 100f), 1f);
                    mMakeupCardStatusTxt.text = mModel.CvcMakeupStatusText;
                    mMakeupCardProgTxt.text = $"Status: <color=#3EE890>Active</color>  Progress: {mModel.CvcMakeupProgressPct:F1} %";
                }

                bool showDetails = !isProcLibScreen;
                mDetDividerRt.gameObject.SetActive(showDetails);
                mDetHdrTxt.gameObject.SetActive(showDetails);
                mDetailsStandardGroup.gameObject.SetActive(showDetails && !mModel.ShowRecircControlInDetails);
                mDetailsRecircControlGroup.gameObject.SetActive(showDetails && mModel.ShowRecircControlInDetails);

                mDetTagTxt.text = mModel.SelectedTag;
                mDetDescTxt.text = mModel.SelectedDescription;
                mDetValTxt.text = mModel.SelectedValueText;
                mDetPosTxt.text = mModel.SelectedPositionText;
                mDetStateTxt.text = mModel.SelectedStateText;
                mDetRecircLockImg.sprite = mModel.IsRecircManualMode ? mSprites.lockOpen : mSprites.lockClosed;
                mDetRecircModeTxt.text = mModel.IsRecircManualMode ? "Manual" : "Recirculation";

                // 상단 3단계 드롭다운 & 경보창 갱신
                mUnitDropdown.gameObject.SetActive(step == Scenario2SequenceStep.S2_50_UnitDropdownFor0A);
                mCategoryDropdown.gameObject.SetActive(
                    step == Scenario2SequenceStep.S2_01_StartCatMenuOpen
                    || step == Scenario2SequenceStep.S2_33_CatMenuForRecircStop
                    || step == Scenario2SequenceStep.S2_45_CatMenuForCeRecheck
                    || step == Scenario2SequenceStep.S2_52_Unit0ACatMenuOpen);

                bool showPrim06 =
                    step == Scenario2SequenceStep.S2_02_StartPrimaryMenuOpen
                    || step == Scenario2SequenceStep.S2_10_CeToCvcMenuOpen
                    || step == Scenario2SequenceStep.S2_13_CvcToCeMenuOpen
                    || step == Scenario2SequenceStep.S2_15_CeToCvcForHeatingMenu
                    || step == Scenario2SequenceStep.S2_34_PrimaryMenuForRecircStop
                    || step == Scenario2SequenceStep.S2_46_PrimaryMenuForCeRecheck;
                bool showPrim0A = step == Scenario2SequenceStep.S2_53_Unit0APrimaryMenuOpen;
                mPrimaryScreenDropdown.gameObject.SetActive(showPrim06 || showPrim0A);
                mPrimary06Group.gameObject.SetActive(showPrim06);
                mPrimary0AGroup.gameObject.SetActive(showPrim0A);

                mCvcSubDropdown.gameObject.SetActive(
                    step == Scenario2SequenceStep.S2_11_CvcSubMenuForRecirc
                    || step == Scenario2SequenceStep.S2_16_CvcSubMenuForHeating
                    || step == Scenario2SequenceStep.S2_22_CvcSubMenuForDemins
                    || step == Scenario2SequenceStep.S2_35_CvcSubMenuForRecircStop);

                mAlarmDropdown.gameObject.SetActive(step == Scenario2SequenceStep.S2_30_AlarmDropdownOpen);

                string ceAck = mModel.IsCeAlarmAcknowledged ? "ACKNOWLEDGED" : "ACTIVE";
                mAlarmRow0Txt.text = "<b>06 - CVC RECIRCULATION PUMP A FLOW LOW LOW</b>\nACTIVE";
                mAlarmRow1Txt.text = $"<b>06 - CE GASEOUS DISCHARGE RADIOACTIVITY HI ALARM</b>\n{ceAck}";
                mAlarmRow2Txt.text = "<b>06 - CE VACUUM PUMP SUCTION PRESSURE HI HI</b>\nACTIVE";

                mChrome.refreshHoverWidgetColors();
                foreach (var w in mAllWidgets)
                {
                    if (w != null) w.refreshVisualColor();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Scenario2MainMonitorController] refreshUI 오류: {ex}");
            }
        }
    }
}
