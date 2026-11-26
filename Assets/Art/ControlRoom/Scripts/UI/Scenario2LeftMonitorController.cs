using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ControlRoom.UI
{
    /// <summary>
    /// 시나리오 2 좌측 모니터 (06 : Special &gt; Instructor Station — Malfunctions / Primary Breaks) 컨트롤러입니다.
    /// 참조 캡처(왼쪽 모니터 화면.png, 왼쪽 모니터 primary breaks 내용 참고.png)와 1:1 동일한 레이아웃으로 구성하며,
    /// 상단 경보 배지 및 Run Time이 3대 모니터 간 실시간 동기화됩니다.
    /// </summary>
    public class Scenario2LeftMonitorController
    {
        private readonly Scenario2ControllerModel mModel;
        private readonly MonitorCommonChrome mChrome = new MonitorCommonChrome();
        private ControlRoomSpritePack mSprites;

        public MonitorCommonChrome Chrome => mChrome;

        private TextMeshProUGUI mRunTimeTxt;
        private TextMeshProUGUI mSelectedLocationTxt;
        private TextMeshProUGUI mSelectedSizeTxt;

        // 단위 테스트 검증용 읽기 전용 접근자
        public string RunTimeText => mRunTimeTxt != null ? mRunTimeTxt.text : string.Empty;
        public string SelectedLocationText => mSelectedLocationTxt != null ? mSelectedLocationTxt.text : string.Empty;
        public string SelectedSizeText => mSelectedSizeTxt != null ? mSelectedSizeTxt.text : string.Empty;
        public int HeaderAlarmCount => mChrome.CurrentAlarmCount;
        public int HeaderCautionCount => mChrome.CurrentCautionCount;
        public int HeaderNoticeCount => mChrome.CurrentNoticeCount;

        public Scenario2LeftMonitorController(Scenario2ControllerModel model)
        {
            mModel = model ?? throw new ArgumentNullException(nameof(model));
        }

        /// <summary>좌측 모니터 Instructor Station(Primary Breaks) UI를 1920x1080 캔버스에 구축합니다.</summary>
        public void buildUI(Transform canvasRoot, ControlRoomSpritePack sprites)
        {
            try
            {
                mSprites = sprites ?? new ControlRoomSpritePack();
                mChrome.buildChrome(canvasRoot, false, mSprites, () => false, null, "net.tcp://SIMLABHS01:9080/service/tcp");

                var host = mChrome.ContentArea;
                var bg = ControlRoomUIFactory.createPanel(host, "InstPrimaryBreaksBg", ControlRoomTheme.BgWorkspaceEmpty);

                buildLeftSidebar(bg);
                buildPrimaryBreaksMainArea(bg);
                buildRightSimControlsAndDetails(bg);

                refreshUI();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Scenario2LeftMonitorController] buildUI 오류: {ex}");
            }
        }

        private void buildLeftSidebar(RectTransform host)
        {
            var left = ControlRoomUIFactory.createPanel(host, "InstLeft", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(left, 0f, 0f, 342f, ControlRoomTheme.ContentAreaHeight);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(host, "DivL", ControlRoomTheme.BorderSubtle), 342f, 0f, 1.5f, ControlRoomTheme.ContentAreaHeight);

            var searchBox = ControlRoomUIFactory.createBorderedBox(left, "SearchBox", ControlRoomTheme.BgInput, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(searchBox, 10f, 12f, 294f, 34f);
            ControlRoomUIFactory.createLabel(searchBox.Find("Inner"), "PH", "  Search Transients...", 16f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.MidlineLeft, FontStyles.Italic);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(left, "Q", "Q", 16f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.Center).rectTransform, 308f, 12f, 26f, 34f);
            ControlRoomUIFactory.createHoverOnlyZone(left, "HitSearch", 10f, 12f, 322f, 34f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(left, "TabM", "Malfunctions", 26f, ControlRoomTheme.TabActiveGreen, TextAlignmentOptions.Center).rectTransform, 10f, 52f, 162f, 44f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(left, "TabS", "Systems", 26f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center).rectTransform, 174f, 52f, 158f, 44f);
            ControlRoomUIFactory.createHoverOnlyZone(left, "HitTabM", 10f, 52f, 162f, 44f);
            ControlRoomUIFactory.createHoverOnlyZone(left, "HitTabS", 174f, 52f, 158f, 44f);

            string[] malItems = { "Display", "Primary Breaks", "Secondary Breaks" };
            for (int i = 0; i < malItems.Length; i++)
            {
                bool isPrimaryBreaks = i == 1;
                float ry = 104f + i * 54f;
                var row = ControlRoomUIFactory.createBorderedBox(
                    left, $"Mal_{i}",
                    isPrimaryBreaks ? ControlRoomTheme.AccentBlue : ControlRoomTheme.BgDarkPane,
                    ControlRoomTheme.BorderSubtle, 1f);
                ControlRoomUIFactory.setTopLeftRect(row, 10f, ry, 322f, 54f);
                ControlRoomUIFactory.createLabel(row.Find("Inner"), "L", "   " + malItems[i], 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
                ControlRoomUIFactory.createHoverOnlyZone(row.Find("Inner"), "Hit", 0f, 0f, 320f, 52f);
            }

            var logo = ControlRoomUIFactory.createPanel(left, "E2Logo", Color.white, mSprites.e2LogoInstructor);
            ControlRoomUIFactory.setTopLeftRect(logo, 16f, 706f, 306f, 178f);
        }

        private void buildPrimaryBreaksMainArea(RectTransform host)
        {
            var center = ControlRoomUIFactory.createPanel(host, "PrimaryBreaksPane", ControlRoomTheme.BgPrimary);
            ControlRoomUIFactory.setTopLeftRect(center, 344f, 0f, 1246f, ControlRoomTheme.ContentAreaHeight);

            ControlRoomUIFactory.setTopLeftRect(
                ControlRoomUIFactory.createLabel(center, "Hdr", "Primary Breaks", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center).rectTransform,
                0f, 6f, 1246f, 26f);
            ControlRoomUIFactory.setTopLeftRect(
                ControlRoomUIFactory.createLabel(center, "SubHdr", "A system break that affects reactor coolant inventory", 15.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, FontStyles.Italic).rectTransform,
                16f, 44f, 900f, 26f);

            // 카드 1: Containment Vessel Flange Leak
            var c1 = ControlRoomUIFactory.createBorderedBox(center, "Card_Flange", ControlRoomTheme.BgCard, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(c1, 14f, 84f, 1218f, 46f);
            ControlRoomUIFactory.setTopLeftRect(
                ControlRoomUIFactory.createLabel(c1.Find("Inner"), "T", "Containment Vessel Flange Leak to the reactor pool", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform,
                12f, 8f, 800f, 28f);

            // 카드 2: Leak from CVC to RCCW
            var c2 = ControlRoomUIFactory.createBorderedBox(center, "Card_CvcRccw", ControlRoomTheme.BgCard, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(c2, 14f, 140f, 1218f, 184f);
            var c2In = c2.Find("Inner");
            ControlRoomUIFactory.setTopLeftRect(
                ControlRoomUIFactory.createLabel(c2In, "T", "Leak from CVC to RCCW through the Non-Regenerative Heat Exchanger:", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform,
                12f, 8f, 900f, 28f);
            ControlRoomUIFactory.setTopLeftRect(
                ControlRoomUIFactory.createLabel(c2In, "SzK", "Select a transient\nsize:", 13.5f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.TopLeft).rectTransform,
                100f, 44f, 140f, 40f);
            string[] sizes = { "None", "Small", "Medium", "Large", "Full" };
            for (int i = 0; i < sizes.Length; i++)
            {
                addRadioOption(c2In, $"R_C2_{i}", 260f, 44f + i * 26f, sizes[i], i == 0);
            }

            // 카드 3 (파란 테두리 활성 선택): Initiate a Loss of Coolant Accident (CVC Charging line / Small)
            var c3 = ControlRoomUIFactory.createBorderedBox(center, "Card_LOCA", ControlRoomTheme.BgCard, ControlRoomTheme.HighlightBlue, 1.8f);
            ControlRoomUIFactory.setTopLeftRect(c3, 14f, 336f, 1218f, 188f);
            var c3In = c3.Find("Inner");
            ControlRoomUIFactory.setTopLeftRect(
                ControlRoomUIFactory.createLabel(c3In, "T", "Initiate a Loss of Coolant Accident:", 15.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform,
                12f, 8f, 700f, 28f);

            ControlRoomUIFactory.setTopLeftRect(
                ControlRoomUIFactory.createLabel(c3In, "LocK", "Select a LOCA\nlocation:", 13.5f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.TopLeft).rectTransform,
                100f, 52f, 130f, 40f);
            string[] locs = { "CVC Degas line", "CVC Charging line", "CVC Letdown line" };
            for (int i = 0; i < locs.Length; i++)
            {
                var lbl = addRadioOption(c3In, $"R_Loc_{i}", 260f, 52f + i * 28f, locs[i], i == 1);
                if (i == 1) mSelectedLocationTxt = lbl;
            }

            ControlRoomUIFactory.setTopLeftRect(
                ControlRoomUIFactory.createLabel(c3In, "SzK", "Select a transient\nsize:", 13.5f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.TopLeft).rectTransform,
                530f, 46f, 140f, 40f);
            for (int i = 0; i < sizes.Length; i++)
            {
                var lbl = addRadioOption(c3In, $"R_Sz_{i}", 690f, 44f + i * 26f, sizes[i], i == 1);
                if (i == 1) mSelectedSizeTxt = lbl;
            }

            // 카드 4: SRV fails Fully Open
            var c4 = ControlRoomUIFactory.createBorderedBox(center, "Card_SRV", ControlRoomTheme.BgCard, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(c4, 14f, 536f, 1218f, 118f);
            var c4In = c4.Find("Inner");
            ControlRoomUIFactory.setTopLeftRect(
                ControlRoomUIFactory.createLabel(c4In, "T", "Reactor Coolant System Safety Valve fails Fully Open:", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform,
                12f, 8f, 700f, 28f);
            ControlRoomUIFactory.setTopLeftRect(
                ControlRoomUIFactory.createLabel(c4In, "SrvK", "Select a Safety\nRelief Valve (SRV):", 13.5f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.TopLeft).rectTransform,
                100f, 44f, 140f, 40f);
            addRadioOption(c4In, "R_SrvA", 260f, 44f, "Safety Relief Valve A", true);
            addRadioOption(c4In, "R_SrvB", 260f, 72f, "Safety Relief Valve B", false);

            // 카드 5: SGTF
            var c5 = ControlRoomUIFactory.createBorderedBox(center, "Card_SGTF", ControlRoomTheme.BgCard, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(c5, 14f, 666f, 1218f, 184f);
            var c5In = c5.Find("Inner");
            ControlRoomUIFactory.setTopLeftRect(
                ControlRoomUIFactory.createLabel(c5In, "T", "Steam Generator Tube Failure (SGTF):", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform,
                12f, 8f, 700f, 28f);
            ControlRoomUIFactory.setTopLeftRect(
                ControlRoomUIFactory.createLabel(c5In, "SgK", "Select a Steam\nGenerator division:", 13.5f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.TopLeft).rectTransform,
                100f, 48f, 140f, 40f);
            addRadioOption(c5In, "R_Sg1", 260f, 48f, "Steam Generator Division I", true);
            addRadioOption(c5In, "R_Sg2", 260f, 76f, "Steam Generator Division II", false);
            ControlRoomUIFactory.setTopLeftRect(
                ControlRoomUIFactory.createLabel(c5In, "SzK", "Select a transient\nsize:", 13.5f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.TopLeft).rectTransform,
                530f, 46f, 140f, 40f);
            for (int i = 0; i < sizes.Length; i++)
            {
                addRadioOption(c5In, $"R_SgSz_{i}", 690f, 44f + i * 26f, sizes[i], i == 0);
            }
        }

        private TextMeshProUGUI addRadioOption(Transform parent, string name, float x, float y, string text, bool selected)
        {
            var outer = ControlRoomUIFactory.createPanel(parent, name, selected ? ControlRoomTheme.HighlightBlue : ControlRoomTheme.TextSecondary, mSprites.circleDot);
            ControlRoomUIFactory.setTopLeftRect(outer, x, y + 3f, 16f, 16f);
            var innerBg = ControlRoomUIFactory.createPanel(outer, "Bg", ControlRoomTheme.BgCard, mSprites.circleDot);
            ControlRoomUIFactory.setOffsets(innerBg, 2f, 2f, -2f, -2f);
            if (selected)
            {
                var dot = ControlRoomUIFactory.createPanel(innerBg, "Dot", ControlRoomTheme.HighlightBlue, mSprites.circleDot);
                ControlRoomUIFactory.setOffsets(dot, 2.5f, 2.5f, -2.5f, -2.5f);
            }
            var lbl = ControlRoomUIFactory.createLabel(parent, name + "_L", text, 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            ControlRoomUIFactory.setTopLeftRect(lbl.rectTransform, x + 24f, y, 220f, 22f);
            return lbl;
        }

        private void buildRightSimControlsAndDetails(RectTransform host)
        {
            var right = ControlRoomUIFactory.createPanel(host, "InstRight", ControlRoomTheme.BgDarkPane);
            ControlRoomUIFactory.setTopLeftRect(right, 1592f, 0f, 328f, ControlRoomTheme.ContentAreaHeight);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(host, "LineR", ControlRoomTheme.BorderSubtle), 1590.5f, 0f, 1.5f, ControlRoomTheme.ContentAreaHeight);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(right, "Hdr", "Sim Controls", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform, 14f, 10f, 200f, 28f);
            addHairline(right, 44f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(right, "Stat", "Simulator Status:  Running", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform, 14f, 64f, 210f, 34f);
            ControlRoomUIFactory.createE2Control(right, "BtnPlaySim", 226f, 58f, 42f, 42f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "▶", 18f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);
            ControlRoomUIFactory.createE2Control(right, "BtnPauseSim", 274f, 58f, 42f, 42f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "||", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);
            addHairline(right, 114f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(right, "CliK", "Clients Connected:", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform, 14f, 130f, 160f, 28f);
            var cli17 = ControlRoomUIFactory.createBorderedBox(right, "CliBox", ControlRoomTheme.BgInput, ControlRoomTheme.BorderSubtle, 1f);
            ControlRoomUIFactory.setTopLeftRect(cli17, 152f, 128f, 36f, 30f);
            ControlRoomUIFactory.createLabel(cli17.Find("Inner"), "V", "17", 17f, ControlRoomTheme.StatusAlarm, TextAlignmentOptions.Center, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(right, "CliMax", "/ 18", 16.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform, 194f, 130f, 60f, 28f);
            addHairline(right, 168f);

            mRunTimeTxt = ControlRoomUIFactory.createLabel(right, "RunT", "Run Time:   00:01:07", 16.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            ControlRoomUIFactory.setTopLeftRect(mRunTimeTxt.rectTransform, 14f, 182f, 280f, 28f);
            addHairline(right, 224f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(right, "IcLbl", "IC:  Full Power Steady State", 15.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform, 14f, 240f, 200f, 28f);
            ControlRoomUIFactory.createE2Control(right, "BtnReset", 220f, 236f, 70f, 36f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "Reset", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true);
            ControlRoomUIFactory.createE2Control(right, "BtnResetDrop", 292f, 236f, 24f, 36f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "▼", 10f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.Center, null, true);
            addHairline(right, 282f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(right, "AudK", "Audio:", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform, 14f, 300f, 72f, 28f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(right, "AudDot", ControlRoomTheme.StatusGreen, mSprites.circleDot), 84f, 304f, 18f, 18f);
            addHairline(right, 340f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(right, "RstK", "Restart Simulator:", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform, 14f, 358f, 180f, 30f);
            ControlRoomUIFactory.createE2Control(right, "BtnRestartSim", 274f, 352f, 42f, 42f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "①", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true);
            addHairline(right, 456f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(right, "SubSC", "Sim Controls", 15.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center).rectTransform, 14f, 466f, 108f, 26f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(right, "SubLine", ControlRoomTheme.HighlightBlue), 14f, 494f, 108f, 2.5f);
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(right, "SubLog", "Log", 15.5f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.Center).rectTransform, 128f, 466f, 54f, 26f);
            ControlRoomUIFactory.createHoverOnlyZone(right, "HitLog", 128f, 466f, 54f, 28f);

            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(right, "DetHdr", "Details", 16f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft).rectTransform, 14f, 508f, 120f, 28f);
            string detailBody =
                "<b>LOCA</b>\n" +
                "DCA Section: 15.6.5. Initiates a pipe break of the CVC system inside containment. Reactor Coolant System (RCS) depressurizes and coolant enters containment. Containment will isolate. ECCS will actuate when CNT level rises significantly.\n\n" +
                "<b>CVC Charging line</b>\n" +
                "A pipe break in the CVC Charging line\n\n" +
                "<b>Small</b>\n" +
                "A small break resulting in 0.1-5% of the flow escaping. Typically causes one or more cautions and eventually an alarm, but not a safety system actuation.";
            ControlRoomUIFactory.setTopLeftRect(
                ControlRoomUIFactory.createLabel(right, "DetBody", detailBody, 13.5f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.TopLeft).rectTransform,
                14f, 542f, 300f, 350f);

            ControlRoomUIFactory.createE2Control(right, "BtnVal", 136f, 922f, 84f, 34f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "Validate", 14f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);
            ControlRoomUIFactory.createE2Control(right, "BtnExec", 228f, 922f, 84f, 34f, ControlRoomTheme.BgButton, ControlRoomTheme.BgButton, null, () => false, null, "Execute", 14f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);
        }

        private void addHairline(Transform parent, float y)
        {
            ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(parent, $"HL_{y}", new Color(0.42f, 0.44f, 0.47f, 0.6f)), 10f, y, 308f, 1.2f);
        }

        public void tickChrome(int heartbeatIndex)
        {
            mChrome.updateHeartbeat(heartbeatIndex);
            mChrome.refreshDeviceClock();
        }

        /// <summary>상단 크롬 호기/경보 배지 및 실시간 Run Time을 갱신합니다.</summary>
        public void refreshUI()
        {
            try
            {
                var p = mModel.PlantModel;
                mChrome.updateHeaderAndClock(
                    "06",
                    "Special",
                    "Instructor Station",
                    p.ActiveAlarmCount,
                    p.ActiveCautionCount,
                    p.NoticeBadgeCount,
                    null,
                    mSprites);

                if (mRunTimeTxt != null)
                {
                    mRunTimeTxt.text = $"Run Time:   {mModel.getFormattedRunTime()}";
                }
                mChrome.refreshHoverWidgetColors();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Scenario2LeftMonitorController] refreshUI 오류: {ex}");
            }
        }
    }
}
