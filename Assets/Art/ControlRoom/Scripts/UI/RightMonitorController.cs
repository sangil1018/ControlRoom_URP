using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ControlRoom.UI
{
    /// <summary>우측 모니터 표시 모드 구분 (시나리오 1: 12-Unit Overview / 추후 재사용: Safety Function Monitor)</summary>
    public enum RightMonitorMode
    {
        TwelveUnitOverview,
        SafetyFunctionMonitor
    }

    /// <summary>
    /// 우측 모니터 (기본: 06 : Special &gt; 12-Unit Overview, 보존: Safety Function Monitor) 컨트롤러입니다.
    /// 시나리오 1 정상 상태에서는 12개 호기를 붉은 경보·이상 표시 없이 깔끔하게 모니터링하며,
    /// 기존 Safety Function Monitor UI는 비활성 루트(mSfmRoot)로 그대로 보존하여 필요 시 즉시 전환할 수 있습니다.
    /// </summary>
    public class RightMonitorController
    {
        private sealed class UnitRowWidgets
        {
            public Image borderBox;
            public readonly Image[] sfmCells = new Image[6];
            public Image alarmImg;
            public Image cautionImg;
            public Image noticeImg;
            public TextMeshProUGUI alarmTxt;
            public TextMeshProUGUI cautionTxt;
            public TextMeshProUGUI noticeTxt;
            public GameObject radUpBadge;
            public GameObject carUpBadge;
            public TextMeshProUGUI thermalPwrTxt;
            public TextMeshProUGUI tAveTxt;
            public TextMeshProUGUI pzrPressTxt;
            public TextMeshProUGUI pzrLevelTxt;
            public TextMeshProUGUI rcsFlowTxt;
            public TextMeshProUGUI cntPressTxt;
            public TextMeshProUGUI ceRadTxt;
            public TextMeshProUGUI carOffGasTxt;
        }

        private readonly PlantSimulationModel mModel;
        private readonly MonitorCommonChrome mChrome = new MonitorCommonChrome();
        private ControlRoomSpritePack mSprites;
        private string mHeaderUnitCodeOverride;

        public MonitorCommonChrome Chrome => mChrome;
        public RightMonitorMode CurrentMode { get; private set; } = RightMonitorMode.TwelveUnitOverview;
        public bool IsSfmPreserved => mSfmRoot != null;
        public int TwelveUnitRowCount => mUnitRows.Count;

        // 12-Unit Overview 루트 및 12호기 행 위젯
        private RectTransform mTwelveUnitRoot;
        private readonly List<UnitRowWidgets> mUnitRows = new List<UnitRowWidgets>();

        // 기존 Safety Function Monitor 보존 루트 및 위젯
        private RectTransform mSfmRoot;
        private TextMeshProUGUI mUnitLargeTxt;
        private TextMeshProUGUI mNiPwrTxt, mTColdTxt, mTHotTxt, mPzrPressTxt;
        private TextMeshProUGUI mCondATxt, mCondBTxt, mFwATxt, mFwBTxt;
        private TextMeshProUGUI mSg1PTxt, mSg2PTxt, mSg1TTxt, mSg2TTxt;
        private readonly List<(Image bg, TextMeshProUGUI txt)> mRodCrossCells = new List<(Image, TextMeshProUGUI)>();

        public RightMonitorController(PlantSimulationModel model)
        {
            mModel = model ?? throw new ArgumentNullException(nameof(model));
        }

        public void buildUI(Transform canvasRoot, ControlRoomSpritePack sprites)
        {
            try
            {
                mSprites = sprites ?? new ControlRoomSpritePack();
                mChrome.buildChrome(canvasRoot, false, mSprites, () => false, null, "net.tcp://SIMLABHS01:9080/service/tcp");

                buildTwelveUnitOverviewView(mChrome.ContentArea);
                buildPreservedSafetyFunctionMonitorView();

                setDisplayMode(RightMonitorMode.TwelveUnitOverview);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RightMonitorController] buildUI 오류: {ex}");
            }
        }

        /// <summary>우측 모니터 상단 호기 번호 표시를 강제로 지정합니다(시나리오 2에서는 "00" 표시, null이면 기본 ActiveUnitCode).</summary>
        public void setHeaderUnitCodeOverride(string unitCode)
        {
            try
            {
                mHeaderUnitCodeOverride = unitCode;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RightMonitorController] setHeaderUnitCodeOverride 오류: {ex}");
            }
        }

        /// <summary>12-Unit Overview 화면과 보존된 Safety Function Monitor 화면을 전환합니다.</summary>
        public void setDisplayMode(RightMonitorMode mode)
        {
            CurrentMode = mode;
            if (mTwelveUnitRoot != null) mTwelveUnitRoot.gameObject.SetActive(mode == RightMonitorMode.TwelveUnitOverview);
            if (mSfmRoot != null) mSfmRoot.gameObject.SetActive(mode == RightMonitorMode.SafetyFunctionMonitor);
            refreshUI();
        }

        public void tickChrome(int heartbeatIndex)
        {
            mChrome.updateHeartbeat(heartbeatIndex);
            mChrome.refreshDeviceClock();
        }

        /// <summary>12-Unit Overview에 붉은 경보 아이콘이나 이상 화살표 배지가 하나라도 표시 중인지 확인합니다(정상 상태 검증용).</summary>
        public bool hasAnyAlertIndicatorVisible()
        {
            for (int i = 0; i < mUnitRows.Count; i++)
            {
                var row = mUnitRows[i];
                if (row.radUpBadge != null && row.radUpBadge.activeSelf) return true;
                if (row.carUpBadge != null && row.carUpBadge.activeSelf) return true;
                if (row.alarmImg != null && mSprites != null && mSprites.alarmOn != null && row.alarmImg.sprite == mSprites.alarmOn) return true;
                if (row.cautionImg != null && mSprites != null && mSprites.cautionOn != null && row.cautionImg.sprite == mSprites.cautionOn) return true;
            }
            return false;
        }

        // =====================================================================
        // 신규 12-Unit Overview 구성 (참조 이미지 기반, 정상 운전 시 붉은 표시 제거)
        // =====================================================================
        private void buildTwelveUnitOverviewView(RectTransform host)
        {
            mTwelveUnitRoot = ControlRoomUIFactory.createPanel(host, "TwelveUnitOverviewRoot", ControlRoomTheme.hex("#1E2023"));
            mUnitRows.Clear();

            // 상단 계측 컬럼 헤더
            string[] headers =
            {
                "Thermal Power", "T-Average", "PZR Pressure", "PZR Level",
                "RCS Flow", "CNT Pressure", "CE Rad Mon", "CAR Off gas"
            };
            float[] colXs = { 488f, 642f, 782f, 944f, 1084f, 1244f, 1404f, 1554f };
            float[] colWs = { 144f, 132f, 150f, 130f, 148f, 148f, 136f, 144f };

            for (int c = 0; c < headers.Length; c++)
            {
                var hLbl = ControlRoomUIFactory.createLabel(mTwelveUnitRoot, $"ColHdr_{c}", headers[c], 14.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                hLbl.enableWordWrapping = false;
                ControlRoomUIFactory.setTopLeftRect(hLbl.rectTransform, colXs[c], 6f, colWs[c], 26f);
            }

            // 12개 호기 행 (01 ~ 12)
            for (int i = 0; i < 12; i++)
            {
                float rowY = 38f + i * 77f;
                Color rowBg = (i % 2 == 0) ? ControlRoomTheme.hex("#25282C") : ControlRoomTheme.hex("#2D3035");
                var rowRt = ControlRoomUIFactory.createPanel(mTwelveUnitRoot, $"UnitRow_{i + 1:00}", rowBg);
                ControlRoomUIFactory.setTopLeftRect(rowRt, 14f, rowY, 1756f, 71f);

                var w = new UnitRowWidgets();

                // 좌측 호기 박스 + 6개 SFM 상태 타일 + SFM 배지
                var unitFrame = ControlRoomUIFactory.createBorderedBox(rowRt, "UnitBox", ControlRoomTheme.hex("#1A1C1F"), ControlRoomTheme.StatusGreen, 1.8f);
                ControlRoomUIFactory.setTopLeftRect(unitFrame, 2f, 2f, 254f, 67f);
                w.borderBox = unitFrame.GetComponent<Image>();

                var unitNum = ControlRoomUIFactory.createLabel(unitFrame.Find("Inner"), "UnitNo", $"{i + 1:00}", 24f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                ControlRoomUIFactory.setTopLeftRect(unitNum.rectTransform, 12f, 6f, 48f, 52f);

                for (int r = 0; r < 2; r++)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        int cellIdx = r * 3 + c;
                        var cell = ControlRoomUIFactory.createBorderedBox(unitFrame.Find("Inner"), $"SfmCell_{cellIdx}", ControlRoomTheme.StatusGreen, ControlRoomTheme.BgDarkest, 1.0f);
                        ControlRoomUIFactory.setTopLeftRect(cell, 102f + c * 28f, 9f + r * 24f, 24f, 20f);
                        w.sfmCells[cellIdx] = cell.Find("Inner").GetComponent<Image>();
                    }
                }

                var sfmBadge = ControlRoomUIFactory.createBorderedBox(unitFrame.Find("Inner"), "SfmTag", ControlRoomTheme.hex("#2B2F34"), ControlRoomTheme.TextSecondary, 1.2f);
                ControlRoomUIFactory.setTopLeftRect(sfmBadge, 194f, 19f, 48f, 25f);
                ControlRoomUIFactory.createLabel(sfmBadge.Find("Inner"), "T", "SFM", 12f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);

                // 상태 아이콘 3종 (Alarm / Caution / Notice) — 일반 상황에서는 모두 OFF 아이콘과 0 표시
                var almRt = ControlRoomUIFactory.createPanel(rowRt, "AlmIco", Color.white, mSprites.alarmOff);
                ControlRoomUIFactory.setTopLeftRect(almRt, 272f, 18f, 34f, 34f);
                w.alarmImg = almRt.GetComponent<Image>();
                w.alarmImg.preserveAspect = true;
                w.alarmTxt = ControlRoomUIFactory.createLabel(rowRt, "AlmCnt", "0\n0", 10.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center);
                w.alarmTxt.lineSpacing = 36f;
                ControlRoomUIFactory.setTopLeftRect(w.alarmTxt.rectTransform, 308f, 10f, 18f, 50f);

                var cauRt = ControlRoomUIFactory.createPanel(rowRt, "CauIco", Color.white, mSprites.cautionOff);
                ControlRoomUIFactory.setTopLeftRect(cauRt, 342f, 18f, 34f, 34f);
                w.cautionImg = cauRt.GetComponent<Image>();
                w.cautionImg.preserveAspect = true;
                w.cautionTxt = ControlRoomUIFactory.createLabel(rowRt, "CauCnt", "0\n0", 10.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center);
                w.cautionTxt.lineSpacing = 36f;
                ControlRoomUIFactory.setTopLeftRect(w.cautionTxt.rectTransform, 378f, 10f, 18f, 50f);

                var ntcRt = ControlRoomUIFactory.createPanel(rowRt, "NtcIco", Color.white, mSprites.noticeOff);
                ControlRoomUIFactory.setTopLeftRect(ntcRt, 410f, 18f, 34f, 34f);
                w.noticeImg = ntcRt.GetComponent<Image>();
                w.noticeImg.preserveAspect = true;
                w.noticeTxt = ControlRoomUIFactory.createLabel(rowRt, "NtcCnt", "0", 11f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center);
                ControlRoomUIFactory.setTopLeftRect(w.noticeTxt.rectTransform, 444f, 36f, 18f, 20f);

                // 상승 이상 화살표 배지 (정상 시 완전히 숨김, 시나리오 2 LOCA 시에만 활성)
                var radUp = ControlRoomUIFactory.createPanel(rowRt, "RadUpBadge", Color.white, mSprites.upBadge);
                ControlRoomUIFactory.setTopLeftRect(radUp, 1364f, 18f, 26f, 34f);
                radUp.GetComponent<Image>().preserveAspect = true;
                radUp.gameObject.SetActive(false);
                w.radUpBadge = radUp.gameObject;

                var carUp = ControlRoomUIFactory.createPanel(rowRt, "CarUpBadge", Color.white, mSprites.upBadge);
                ControlRoomUIFactory.setTopLeftRect(carUp, 1514f, 18f, 26f, 34f);
                carUp.GetComponent<Image>().preserveAspect = true;
                carUp.gameObject.SetActive(false);
                w.carUpBadge = carUp.gameObject;

                // 8개 시안 계측치 라벨 (row 기준 X = colXs[c] - 14f)
                w.thermalPwrTxt = addRowVal(rowRt, "V_Pwr", colXs[0] - 14f, colWs[0]);
                w.tAveTxt = addRowVal(rowRt, "V_Tave", colXs[1] - 14f, colWs[1]);
                w.pzrPressTxt = addRowVal(rowRt, "V_Pzrp", colXs[2] - 14f, colWs[2]);
                w.pzrLevelTxt = addRowVal(rowRt, "V_Pzrl", colXs[3] - 14f, colWs[3]);
                w.rcsFlowTxt = addRowVal(rowRt, "V_Rcs", colXs[4] - 14f, colWs[4]);
                w.cntPressTxt = addRowVal(rowRt, "V_Cnt", colXs[5] - 14f, colWs[5]);
                w.ceRadTxt = addRowVal(rowRt, "V_Rad", colXs[6] - 14f, colWs[6]);
                w.carOffGasTxt = addRowVal(rowRt, "V_Car", colXs[7] - 14f, colWs[7]);

                // 행 호버 전용 (클릭 시 무동작)
                ControlRoomUIFactory.createHoverOnlyZone(rowRt, "RowHit", 0f, 0f, 1756f, 71f);
                mUnitRows.Add(w);
            }

            // 우측 사이드 액션 버튼 4개 (호버 전용 — 클릭 시 무동작)
            ControlRoomUIFactory.createE2Control(
                mTwelveUnitRoot, "BtnRight_ImpParams",
                1784f, 40f, 124f, 112f,
                ControlRoomTheme.AccentBlue, ControlRoomTheme.AccentBlue,
                () => true, () => false, null,
                "123\n<size=14>Important\nParameters</size>", 26f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);

            ControlRoomUIFactory.createE2Control(
                mTwelveUnitRoot, "BtnRight_Processes",
                1784f, 162f, 124f, 112f,
                ControlRoomTheme.BgPanel, ControlRoomTheme.BgButton,
                null, () => false, null,
                "Processes", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);

            ControlRoomUIFactory.createE2Control(
                mTwelveUnitRoot, "BtnRight_EventEntry",
                1784f, 284f, 124f, 112f,
                ControlRoomTheme.BgPanel, ControlRoomTheme.BgButton,
                null, () => false, null,
                "Event\nEntry", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);

            ControlRoomUIFactory.createE2Control(
                mTwelveUnitRoot, "BtnRight_CommonProcEvents",
                1784f, 406f, 124f, 126f,
                ControlRoomTheme.BgPanel, ControlRoomTheme.BgButton,
                null, () => false, null,
                "Common\nProcesses\nand Events", 14.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, null, true, ControlRoomTheme.BorderSubtle);
        }

        private TextMeshProUGUI addRowVal(Transform parent, string name, float x, float w)
        {
            var lbl = ControlRoomUIFactory.createLabel(parent, name, "--", 17.5f, ControlRoomTheme.TelemetryCyan, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            lbl.enableWordWrapping = false;
            ControlRoomUIFactory.setTopLeftRect(lbl.rectTransform, x, 12f, w, 46f);
            return lbl;
        }

        // =====================================================================
        // 기존 Safety Function Monitor 보존 (추후 시나리오 재사용 가능)
        // =====================================================================
        private void buildPreservedSafetyFunctionMonitorView()
        {
            mSfmRoot = ControlRoomUIFactory.createPanel(mChrome.Root, "RightSfmPreservedRoot", Color.clear);
            ControlRoomUIFactory.setTopLeftRect(mSfmRoot, 0f, 0f, ControlRoomTheme.CanvasWidth, ControlRoomTheme.CanvasHeight);
            // 상·하단 크롬 바로 뒤에 위치하도록 형제 순서 조정
            mSfmRoot.SetSiblingIndex(0);

            var fullBackdrop = ControlRoomUIFactory.createPanel(mSfmRoot, "RightSfmBackdrop", Color.white, mSprites.e2RightSfm);
            ControlRoomUIFactory.setTopLeftRect(fullBackdrop, 0f, 0f, 1920f, 1080f);

            mUnitLargeTxt = ControlRoomUIFactory.createLabel(fullBackdrop, "UnitLarge", "06", 58f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
            mUnitLargeTxt.enableWordWrapping = false;
            mUnitLargeTxt.overflowMode = TextOverflowModes.Overflow;
            ControlRoomUIFactory.setTopLeftRect(mUnitLargeTxt.rectTransform, 1720f, 118f, 150f, 102f);

            mNiPwrTxt = addSfmVal(fullBackdrop, "Sfm_NI", 378f, 595.5f, 86f, 28.4f, 17.5f);
            mTColdTxt = addSfmVal(fullBackdrop, "Sfm_TC", 845f, 436.5f, 93f, 30.3f, 17f);
            mTHotTxt = addSfmVal(fullBackdrop, "Sfm_TH", 845f, 468.7f, 93f, 30.3f, 17f);
            mPzrPressTxt = addSfmVal(fullBackdrop, "Sfm_PZRP", 845f, 642.9f, 111f, 26.5f, 17f);

            mCondATxt = addSfmVal(fullBackdrop, "Sfm_CA", 816f, 856.8f, 79f, 34.1f, 17f);
            mCondBTxt = addSfmVal(fullBackdrop, "Sfm_CB", 816f, 906.0f, 79f, 34.1f, 17f);
            mFwATxt = addSfmVal(fullBackdrop, "Sfm_FA", 1008f, 856.8f, 80f, 34.1f, 17f);
            mFwBTxt = addSfmVal(fullBackdrop, "Sfm_FB", 1008f, 906.0f, 80f, 34.1f, 17f);

            mSg1PTxt = addSfmVal(fullBackdrop, "Sfm_SG1P", 1450f, 866.2f, 95f, 32.2f, 17f);
            mSg2PTxt = addSfmVal(fullBackdrop, "Sfm_SG2P", 1560f, 866.2f, 96f, 32.2f, 17f);
            mSg1TTxt = addSfmVal(fullBackdrop, "Sfm_SG1T", 1450f, 900.3f, 95f, 32.2f, 17f);
            mSg2TTxt = addSfmVal(fullBackdrop, "Sfm_SG2T", 1560f, 900.3f, 96f, 32.2f, 17f);

            mRodCrossCells.Clear();
            var rodCoords = new (float x, float y, float w, float h)[]
            {
                (507f, 735.6f, 33f, 24.6f), (507f, 809.5f, 33f, 24.6f),
                (507f, 882.3f, 33f, 24.6f), (507f, 956.2f, 33f, 24.6f),
                (386f, 845.4f, 33f, 24.6f), (467f, 845.4f, 33f, 24.6f),
                (547f, 845.4f, 33f, 24.6f), (628f, 845.4f, 33f, 24.6f),
            };
            for (int i = 0; i < rodCoords.Length; i++)
            {
                var rc = rodCoords[i];
                var cell = ControlRoomUIFactory.createPanel(fullBackdrop, $"SfmRod_{i}", ControlRoomTheme.RodCyan);
                ControlRoomUIFactory.setTopLeftRect(cell, rc.x + 2f, rc.y + 2f, rc.w - 4f, rc.h - 4f);
                var lbl = ControlRoomUIFactory.createLabel(cell, "N", "201", 11.5f, ControlRoomTheme.BgDarkest, TextAlignmentOptions.Center, FontStyles.Bold);
                mRodCrossCells.Add((cell.GetComponent<Image>(), lbl));
            }

            for (int i = 0; i < 3; i++) ControlRoomUIFactory.createHoverOnlyZone(mSfmRoot, $"H_SfmCircle_{i}", 4f, 112f + i * 50f, 44f, 44f);
            for (int i = 0; i < 4; i++) ControlRoomUIFactory.createHoverOnlyZone(mSfmRoot, $"H_RightMet_{i}", 1696f, 270f + i * 206f, 200f, 110f);

            mSfmRoot.gameObject.SetActive(false);
        }

        private TextMeshProUGUI addSfmVal(Transform p, string n, float x, float y, float w, float h, float fs)
        {
            var t = ControlRoomUIFactory.createLabel(p, n, "--", fs, ControlRoomTheme.TelemetryCyan, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(t.rectTransform, x, y, w, h);
            return t;
        }

        public void refreshUI()
        {
            try
            {
                string title = CurrentMode == RightMonitorMode.TwelveUnitOverview ? "12-Unit Overview" : "Safety Function Monitor";
                string headerUnit = string.IsNullOrEmpty(mHeaderUnitCodeOverride) ? mModel.ActiveUnitCode : mHeaderUnitCodeOverride;
                mChrome.updateHeaderAndClock(
                    headerUnit,
                    "Special",
                    title,
                    mModel.ActiveAlarmCount,
                    mModel.ActiveCautionCount,
                    mModel.NoticeBadgeCount,
                    null,
                    mSprites);

                if (CurrentMode == RightMonitorMode.TwelveUnitOverview)
                {
                    refreshTwelveUnitRows();
                }
                else
                {
                    refreshPreservedSfm();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RightMonitorController] refreshUI 오류: {ex}");
            }
        }

        private void refreshTwelveUnitRows()
        {
            for (int i = 0; i < mUnitRows.Count && i < mModel.AllUnits.Length; i++)
            {
                var u = mModel.AllUnits[i];
                var row = mUnitRows[i];

                // 일반/시나리오 1 정상 상태에서는 붉은 표시나 추가 경고 UI를 전혀 표시하지 않음
                bool hasAlertState = mModel.IsLocaActive && (u.alarmCount > 0 || u.cautionCount > 0 || u.isTripped);
                row.borderBox.color = hasAlertState && u.isTripped ? ControlRoomTheme.StatusAlarm : ControlRoomTheme.StatusGreen;

                for (int c = 0; c < row.sfmCells.Length; c++)
                {
                    bool ok = !hasAlertState || (u.sfmStatus != null && c < u.sfmStatus.Length && u.sfmStatus[c]);
                    row.sfmCells[c].color = ok ? ControlRoomTheme.StatusGreen : ControlRoomTheme.StatusAlarm;
                }

                if (mSprites != null)
                {
                    row.alarmImg.sprite = hasAlertState && u.alarmCount > 0 ? mSprites.alarmOn : mSprites.alarmOff;
                    row.cautionImg.sprite = hasAlertState && u.cautionCount > 0 ? mSprites.cautionOn : mSprites.cautionOff;
                    row.noticeImg.sprite = (hasAlertState || mModel.IsLocaActive) && u.noticeCount > 0 ? mSprites.noticeOn : mSprites.noticeOff;
                }

                row.alarmTxt.text = hasAlertState ? $"{u.alarmCount}\n{u.alarmCount}" : "0\n0";
                row.cautionTxt.text = hasAlertState ? $"{u.cautionCount}\n{u.cautionCount}" : "0\n0";
                row.noticeTxt.text = (hasAlertState || mModel.IsLocaActive) ? u.noticeCount.ToString() : "0";

                bool showUpBadges = mModel.IsLocaActive && i == 5;
                if (row.radUpBadge != null) row.radUpBadge.SetActive(showUpBadges);
                if (row.carUpBadge != null) row.carUpBadge.SetActive(showUpBadges);

                row.thermalPwrTxt.text = $"{u.thermalPowerPct:F1} %";
                row.tAveTxt.text = $"{u.tAverageF:F1} °F";
                row.pzrPressTxt.text = $"{u.pzrPressurePsia:F0} psia";
                row.pzrLevelTxt.text = $"{u.pzrLevelPct:F1} %";
                row.rcsFlowTxt.text = $"{u.rcsFlowGpm:F0} gpm";
                row.cntPressTxt.text = $"{u.cntPressurePsia:F2} psia";
                row.ceRadTxt.text = $"{u.ceRadCps:F0} cps";
                row.carOffGasTxt.text = $"{u.carOffGasCpm:F0} cpm";
            }
        }

        private void refreshPreservedSfm()
        {
            if (mUnitLargeTxt == null) return;
            mUnitLargeTxt.text = mModel.ActiveUnitCode;
            mNiPwrTxt.text = $"{mModel.CoreThermalPowerPct:F1} %";
            mTColdTxt.text = $"{mModel.RcsTColdF:F0} °F";
            mTHotTxt.text = $"{mModel.RcsTHotF:F0} °F";
            mPzrPressTxt.text = $"{mModel.PzrPressurePsia:F0} psia";

            mCondATxt.text = "100 %";
            mCondBTxt.text = "100 %";
            mFwATxt.text = $"{mModel.FwPumpASpeedPct:F1} %";
            mFwBTxt.text = $"{mModel.FwPumpBSpeedPct:F1} %";

            string msP = $"{Mathf.Lerp(508f, 520f, Mathf.InverseLerp(99.8f, 80f, mModel.CoreThermalPowerPct)):F0} psia";
            mSg1PTxt.text = msP;
            mSg2PTxt.text = msP;
            mSg1TTxt.text = "584 °F";
            mSg2TTxt.text = "584 °F";

            Color crossCol = mModel.ControlRodSteps < 200 ? ControlRoomTheme.RodYellow : ControlRoomTheme.RodCyan;
            for (int i = 0; i < mRodCrossCells.Count; i++)
            {
                mRodCrossCells[i].bg.color = crossCol;
                mRodCrossCells[i].txt.text = mModel.ControlRodSteps.ToString();
            }
        }
    }
}
