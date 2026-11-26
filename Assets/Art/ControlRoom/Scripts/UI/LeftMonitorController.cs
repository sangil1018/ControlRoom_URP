using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ControlRoom.UI
{
    /// <summary>
    /// 좌측 모니터 (06 : Special &gt; Unit Overview) 컨트롤러입니다.
    /// 교육자료 p.70(76) 원본 1920x1080 시트를 기반으로 동적 계측치·트렌드·FW/MS 유로만 실시간 연동하며,
    /// 모든 버튼/탭은 호버 시 살짝 진해지고 클릭 시 아무 동작도 하지 않습니다.
    /// </summary>
    public class LeftMonitorController
    {
        private readonly PlantSimulationModel mModel;
        private readonly MonitorCommonChrome mChrome = new MonitorCommonChrome();
        private ControlRoomSpritePack mSprites;

        public MonitorCommonChrome Chrome => mChrome;

        private TextMeshProUGUI mM6Lbl;
        private TextMeshProUGUI mNiPwrTxt, mCalPwrTxt, mTaveTxt, mPzrPressTxt, mPzrLevelTxt, mRpvLevelTxt, mCnvTempTxt, mCnvPressTxt, mCnvLevelTxt;
        private TextMeshProUGUI mLegNiTxt, mLegThTxt, mLegTurbTxt, mLegMsPTxt;
        private TextMeshProUGUI mHotwellTxt, mCondATxt, mCondBTxt, mFwFlowATxt, mFwFlowBTxt, mFwPctATxt, mFwPctBTxt;
        private TextMeshProUGUI mFwTotalTxt, mMismatchTxt, mSg1Txt, mSg2Txt, mMsTotalTxt, mMsPressTxt, mGovPctTxt, mTurbPwrTxt, mCndPressTxt;
        private TrendChartWidget mTrendChart;
        private readonly List<TextMeshProUGUI> mChartTimeLabels = new List<TextMeshProUGUI>();

        /// <summary>단위 테스트 및 외부 검증용 읽기 전용 계측 라벨 접근자입니다.</summary>
        public string NiPwrText => mNiPwrTxt != null ? mNiPwrTxt.text : string.Empty;
        public string TurbinePowerText => mTurbPwrTxt != null ? mTurbPwrTxt.text : string.Empty;
        public string Sg1Text => mSg1Txt != null ? mSg1Txt.text : string.Empty;
        public string Sg2Text => mSg2Txt != null ? mSg2Txt.text : string.Empty;
        public string MismatchText => mMismatchTxt != null ? mMismatchTxt.text : string.Empty;
        public string CndPressText => mCndPressTxt != null ? mCndPressTxt.text : string.Empty;
        public string GovText => mGovPctTxt != null ? mGovPctTxt.text : string.Empty;
        public string FwTotalText => mFwTotalTxt != null ? mFwTotalTxt.text : string.Empty;
        public string FwPctAText => mFwPctATxt != null ? mFwPctATxt.text : string.Empty;
        public string MsPressText => mMsPressTxt != null ? mMsPressTxt.text : string.Empty;

        public LeftMonitorController(PlantSimulationModel model)
        {
            mModel = model ?? throw new ArgumentNullException(nameof(model));
        }

        public void buildUI(Transform canvasRoot, ControlRoomSpritePack sprites)
        {
            try
            {
                mSprites = sprites ?? new ControlRoomSpritePack();
                mChrome.buildChrome(canvasRoot, false, mSprites, () => false, null, "net.tcp://localhost:9080/service/tcp");

                var host = mChrome.ContentArea;
                // 원본 추출 시트(e2_left_unit_overview 1920x1080 중 y=56..1030 본문 영역)를 Full로 표시
                // 전체 1920x1080을 Root 아래 TopBar/BottomBar 뒤에 배치해 완벽히 결합
                var fullBackdrop = ControlRoomUIFactory.createPanel(mChrome.Root, "LeftE2Backdrop", Color.white, mSprites.e2LeftUnitOverview);
                ControlRoomUIFactory.setTopLeftRect(fullBackdrop, 0f, 0f, 1920f, 1080f);
                fullBackdrop.SetSiblingIndex(0);

                // 06호기 실시간 동적 텍스트 오버레이 (extract_e2_ui_assets.py 실측 좌표)
                mM6Lbl = ControlRoomUIFactory.createLabel(fullBackdrop, "M6", "M6", 30f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                mM6Lbl.enableWordWrapping = false;
                mM6Lbl.overflowMode = TextOverflowModes.Overflow;
                ControlRoomUIFactory.setTopLeftRect(mM6Lbl.rectTransform, 6f, 175f, 68f, 40f);

                mNiPwrTxt = addLeftVal(fullBackdrop, "V_NI", 500.7f, 96f, 29f);
                mCalPwrTxt = addLeftVal(fullBackdrop, "V_CAL", 554.1f, 96f, 29f);
                mTaveTxt = addLeftVal(fullBackdrop, "V_TAVE", 607.5f, 92f, 29f);
                mPzrPressTxt = addLeftVal(fullBackdrop, "V_PZRP", 660.9f, 112f, 31f);
                mPzrLevelTxt = addLeftVal(fullBackdrop, "V_PZRL", 714.2f, 96f, 29f);
                mRpvLevelTxt = addLeftVal(fullBackdrop, "V_RPVL", 767.6f, 92f, 29f);
                mCnvTempTxt = addLeftVal(fullBackdrop, "V_CNVT", 866.6f, 92f, 28f);
                mCnvPressTxt = addLeftVal(fullBackdrop, "V_CNVP", 919.9f, 108f, 30f);
                mCnvLevelTxt = addLeftVal(fullBackdrop, "V_CNVL", 973.3f, 80f, 28f);

                // 상단 차트 PlotArea 투명 오버레이 (원본 격자 유지 + 실시간 곡선만 그리기)
                mTrendChart = ControlRoomUIFactory.createTrendChart(fullBackdrop, "LeftLivePlot", 609.2f, 90.5f, 822.6f, 400.3f, 0f, 100f, 8, 5, drawGrid: false);

                mChartTimeLabels.Clear();
                for (int i = 0; i < 8; i++)
                {
                    var tl = ControlRoomUIFactory.createLabel(fullBackdrop, $"T_{i}", "08:00:00", 10.5f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.Center);
                    ControlRoomUIFactory.setTopLeftRect(tl.rectTransform, 588f + i * 116f, 498f, 82f, 22f);
                    mChartTimeLabels.Add(tl);
                }

                // 범례 테이블 실시간 수치 & 06 태그 접두사
                mLegNiTxt = addLegVal(fullBackdrop, "LV_0", 558.6f);
                mLegThTxt = addLegVal(fullBackdrop, "LV_1", 584.1f);
                mLegTurbTxt = addLegVal(fullBackdrop, "LV_2", 609.7f);
                mLegMsPTxt = addLegVal(fullBackdrop, "LV_3", 635.3f);
                for (int r = 0; r < 4; r++)
                {
                    var u = ControlRoomUIFactory.createLabel(fullBackdrop, $"LU_{r}", "06", 12f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineRight);
                    ControlRoomUIFactory.setTopLeftRect(u.rectTransform, 1000f, 558.6f + r * 25.5f, 22f, 20f);
                }

                // 하단 FW/MS P&ID 실시간 녹색 수치 오버레이
                mHotwellTxt = addMimicVal(fullBackdrop, "HotwellV", 467.7f, 947.7f, 96.4f, 60f, TextAlignmentOptions.TopLeft, 15.5f);
                mCondATxt = addMimicVal(fullBackdrop, "CondA", 596.9f, 772.0f, 98.5f, 31.1f, TextAlignmentOptions.MidlineLeft, 16f);
                mCondBTxt = addMimicVal(fullBackdrop, "CondB", 596.9f, 872.1f, 98.5f, 31.1f, TextAlignmentOptions.MidlineLeft, 16f);
                mFwFlowATxt = addMimicVal(fullBackdrop, "FwFA", 740.5f, 772.0f, 100.5f, 31.1f, TextAlignmentOptions.MidlineLeft, 16f);
                mFwFlowBTxt = addMimicVal(fullBackdrop, "FwFB", 740.5f, 872.1f, 100.5f, 31.1f, TextAlignmentOptions.MidlineLeft, 16f);
                mFwPctATxt = addMimicVal(fullBackdrop, "FwPA", 882.1f, 740.9f, 80.0f, 26.7f, TextAlignmentOptions.MidlineLeft, 16f);
                mFwPctBTxt = addMimicVal(fullBackdrop, "FwPB", 882.1f, 841.0f, 80.0f, 26.7f, TextAlignmentOptions.MidlineLeft, 16f);
                mFwTotalTxt = addMimicVal(fullBackdrop, "FwTot", 1007.2f, 892.1f, 92.3f, 31.1f, TextAlignmentOptions.Center, 16.5f);
                mMismatchTxt = addMimicVal(fullBackdrop, "Mism", 1054.4f, 974.4f, 106.7f, 24.5f, TextAlignmentOptions.MidlineLeft, 14.5f);
                mSg1Txt = addMimicVal(fullBackdrop, "Sg1", 1221.5f, 925.5f, 68.7f, 28.9f, TextAlignmentOptions.MidlineLeft, 15f);
                mSg2Txt = addMimicVal(fullBackdrop, "Sg2", 1221.5f, 963.3f, 68.7f, 28.9f, TextAlignmentOptions.MidlineLeft, 15f);
                mMsTotalTxt = addMimicVal(fullBackdrop, "MsTot", 1263.6f, 876.6f, 88.2f, 26.7f, TextAlignmentOptions.MidlineLeft, 16f);
                mMsPressTxt = addMimicVal(fullBackdrop, "MsP", 1314.9f, 727.6f, 98.5f, 31.1f, TextAlignmentOptions.MidlineLeft, 16.5f);
                mGovPctTxt = addMimicVal(fullBackdrop, "GovP", 1401.0f, 825.4f, 57.4f, 28.9f, TextAlignmentOptions.Center, 16f);
                mTurbPwrTxt = addMimicVal(fullBackdrop, "TurbP", 1493.3f, 729.8f, 90.3f, 26.7f, TextAlignmentOptions.MidlineLeft, 16f);

                // 하단 우측 CND 압력 고정 텍스트(1.02 psia) 마스크 및 실시간 라벨 오버레이
                ControlRoomUIFactory.setTopLeftRect(
                    ControlRoomUIFactory.createPanel(fullBackdrop, "CndPressMask", new Color(62f / 255f, 62f / 255f, 62f / 255f, 1f)),
                    1474f, 924f, 108f, 30f);
                mCndPressTxt = addMimicVal(fullBackdrop, "CndPress", 1474f, 924f, 108f, 30f, TextAlignmentOptions.Center, 15.5f);

                // Details 영역 Valve Name 및 Description 전체를 단일 TMP 폰트로 깔끔 렌더링
                var dValve = ControlRoomUIFactory.createLabel(fullBackdrop, "DetValveVal", "06_CVC_ISV_0331", 12.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
                ControlRoomUIFactory.setTopLeftRect(dValve.rectTransform, 1746f, 584f, 160f, 22f);
                var dDesc = ControlRoomUIFactory.createLabel(fullBackdrop, "DetDescVal", "CNT Module 06 RCS\nInjection Inboard\nContainment Isolation\nValve", 12.2f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.TopLeft);
                ControlRoomUIFactory.setTopLeftRect(dDesc.rectTransform, 1746f, 610f, 164f, 84f);

                // 호버 전용 UI 버튼 히트박스 (상/하단 서브탭, Setpoints/Transmitters, Clear — 클릭 시 무동작)
                for (int i = 0; i < 7; i++) ControlRoomUIFactory.createHoverOnlyZone(host, $"H_SubL_{i}", 6f + i * 62f, 6f, 58f, 28f);
                for (int i = 0; i < 9; i++) ControlRoomUIFactory.createHoverOnlyZone(host, $"H_ChartTab_{i}", 464f + i * 74f, 4f, 70f, 32f);
                for (int i = 0; i < 9; i++) ControlRoomUIFactory.createHoverOnlyZone(host, $"H_MimicTab_{i}", 464f + i * 74f, 594f, 70f, 32f);
                ControlRoomUIFactory.createHoverOnlyZone(host, "H_Setpoints", 6f, 42f, 92f, 34f);
                ControlRoomUIFactory.createHoverOnlyZone(host, "H_Transmitters", 6f, 82f, 92f, 34f);
                ControlRoomUIFactory.createHoverOnlyZone(host, "H_Clear", 1608f, 916f, 62f, 38f);

                refreshUI();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LeftMonitorController] buildUI 오류: {ex}");
            }
        }

        private TextMeshProUGUI addLeftVal(Transform p, string n, float y, float w, float h)
        {
            var t = ControlRoomUIFactory.createLabel(p, n, "--", 16.5f, ControlRoomTheme.TelemetryGreen, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(t.rectTransform, 4.1f, y, w, h);
            return t;
        }

        private TextMeshProUGUI addLegVal(Transform p, string n, float y)
        {
            var t = ControlRoomUIFactory.createLabel(p, n, "--", 12.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
            ControlRoomUIFactory.setTopLeftRect(t.rectTransform, 841f, y, 58f, 20f);
            return t;
        }

        private TextMeshProUGUI addMimicVal(Transform p, string n, float x, float y, float w, float h, TextAlignmentOptions align, float fs)
        {
            var t = ControlRoomUIFactory.createLabel(p, n, "--", fs, ControlRoomTheme.TelemetryGreen, align, FontStyles.Bold);
            ControlRoomUIFactory.setTopLeftRect(t.rectTransform, x, y, w, h);
            return t;
        }

        public void tickChrome(int heartbeatIndex)
        {
            mChrome.updateHeartbeat(heartbeatIndex);
            mChrome.refreshDeviceClock();
        }

        public void refreshUI()
        {
            try
            {
                mChrome.updateHeaderAndClock(
                    mModel.ActiveUnitCode,
                    "Special",
                    "Unit Overview",
                    mModel.ActiveAlarmCount,
                    mModel.ActiveCautionCount,
                    mModel.NoticeBadgeCount,
                    null,
                    mSprites);

                mNiPwrTxt.text = $"{mModel.CoreNiPowerPct:F1} %";
                mCalPwrTxt.text = $"{mModel.CoreThermalPowerPct:F1} %";
                mTaveTxt.text = $"{mModel.RcsTAveF:F0} °F";
                mPzrPressTxt.text = $"{mModel.PzrPressurePsia:F0} psia";
                mPzrLevelTxt.text = $"{mModel.PzrLevelPct:F1} %";
                mRpvLevelTxt.text = $"{mModel.RpvLevelFt:F1} ft";
                mCnvTempTxt.text = $"{mModel.CnvTempF:F0} °F";
                mCnvPressTxt.text = $"{mModel.CnvPressurePsia:F2} psia";
                mCnvLevelTxt.text = $"{mModel.CnvLevelFt:F0} ft";

                mLegNiTxt.text = $"{mModel.CoreNiPowerPct:F1}";
                mLegThTxt.text = $"{mModel.CoreThermalPowerPct:F1}";
                mLegTurbTxt.text = $"{mModel.TurbineElectricMw:F1}";
                mLegMsPTxt.text = $"{mModel.MsHeaderPressurePsia:F0}";

                mHotwellTxt.text = $"3.03 ft\n{mModel.CondenserPressurePsia:F2} psia";
                mCondATxt.text = $"{mModel.FwPumpAFlowLbS:F1} lb/s";
                mCondBTxt.text = $"{mModel.FwPumpBFlowLbS:F1} lb/s";
                mFwFlowATxt.text = $"{mModel.FwPumpAFlowLbS:F1} lb/s";
                mFwFlowBTxt.text = $"{mModel.FwPumpBFlowLbS:F1} lb/s";
                mFwPctATxt.text = $"{mModel.FwPumpASpeedPct:F1} %";
                mFwPctBTxt.text = $"{mModel.FwPumpBSpeedPct:F1} %";
                mFwTotalTxt.text = $"{mModel.FwFlowLbS:F0} lb/s";
                mMismatchTxt.text = mModel.FwMsMismatchLbS >= 0.05f
                    ? $"{mModel.FwMsMismatchLbS:F3} lb/s"
                    : $"{mModel.FwMsMismatchLbS:F2} lb/s";
                mSg1Txt.text = $"{mModel.Sg1SuperheatF:F0} °F";
                mSg2Txt.text = $"{mModel.Sg2SuperheatF:F0} °F";
                mMsTotalTxt.text = $"{mModel.MsFlowLbS:F0} lb/s";
                mMsPressTxt.text = $"{mModel.MsHeaderPressurePsia:F0} psia";
                mGovPctTxt.text = $"{mModel.GovernorPct:F0} %";
                mTurbPwrTxt.text = $"{mModel.TurbineElectricMw:F1} MW";
                if (mCndPressTxt != null) mCndPressTxt.text = $"{mModel.CondenserPressurePsia:F2} psia";

                // 시계 기반 트렌드 X축 시간 갱신
                float baseSec = mModel.SimClockSeconds - 300f;
                for (int i = 0; i < mChartTimeLabels.Count; i++)
                {
                    int sec = Mathf.Max(0, Mathf.FloorToInt(baseSec + (300f / Mathf.Max(1, mChartTimeLabels.Count - 1)) * i)) % 86400;
                    int hh = sec / 3600; int mm = (sec % 3600) / 60; int ss = sec % 60;
                    mChartTimeLabels[i].text = $"{hh:00}:{mm:00}:{ss:00}";
                }

                // 상단 5개 시계열 트레이스 실시간 갱신 (NI%, Thermal%, 0..60MW 핑크, 500..600°F 그린, 470..600psia 라이트블루)
                int sampleCount = mModel.PowerHistory.Count;
                var turbPct = new List<float>(sampleCount);
                var tavePct = new List<float>(sampleCount);
                var msPct = new List<float>(sampleCount);
                for (int i = 0; i < sampleCount; i++)
                {
                    float mw = i < mModel.TurbineMwHistory.Count ? mModel.TurbineMwHistory[i] : mModel.TurbineElectricMw;
                    float tave = i < mModel.TAveHistory.Count ? mModel.TAveHistory[i] : mModel.RcsTAveF;
                    float msP = i < mModel.MsPressHistory.Count ? mModel.MsPressHistory[i] : mModel.MsHeaderPressurePsia;

                    turbPct.Add(Mathf.InverseLerp(0f, 60f, mw) * 100f);
                    tavePct.Add(Mathf.InverseLerp(500f, 600f, tave) * 100f);
                    msPct.Add(Mathf.InverseLerp(470f, 600f, msP) * 100f);
                }
                mTrendChart.updateSeries(0, mModel.NiPowerHistory, ControlRoomTheme.StatusCaution, 2.4f);
                mTrendChart.updateSeries(1, mModel.PowerHistory, ControlRoomTheme.hex("#2E80E8"), 2.0f);
                mTrendChart.updateSeries(2, turbPct, ControlRoomTheme.hex("#E068B8"), 2.8f);
                mTrendChart.updateSeries(3, tavePct, ControlRoomTheme.hex("#20E820"), 2.6f);
                mTrendChart.updateSeries(4, msPct, ControlRoomTheme.hex("#76BFFF"), 2.8f);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LeftMonitorController] refreshUI 오류: {ex}");
            }
        }
    }
}
