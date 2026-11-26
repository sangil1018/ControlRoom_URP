using System;
using System.Collections.Generic;
using UnityEngine;

namespace ControlRoom.UI
{
    /// <summary>교육 시나리오 모드 구분</summary>
    public enum TrainingScenarioType
    {
        Standby,
        Scenario1PowerAdjustment,
        Scenario2LocaEmergency
    }

    /// <summary>Capture[00..49] 및 시나리오 1 전체(100%→80%→100%) 메인 모니터 진행 시퀀스 단계</summary>
    public enum CaptureSequenceStep
    {
        Step00_InstructorPaused = 0,
        Step01_InstructorRunning = 1,
        Step02_ProcessLibraryEmpty = 2,
        Step03_IopExpanded = 3,
        Step04_PowerOpExpanded = 4,
        Step05_PowerReductionPreview = 5,
        Step06_PowerReductionActive = 6,
        Step07_Step1Checked = 7,
        Step08_Step3MenuOpen = 8,
        Step09_Step3NaCollapsed = 9,
        Step10_PowerManeuveringSetup = 10,
        Step11_PmParamsEntered = 11,
        Step12_PmValidated = 12,
        Step13_PmRunningCardView = 13,
        Step14_ReturnedToProcCard = 14,
        Step15_Step6Checked = 15,
        Step16_Step8Checked = 16,
        Step17_ReturnedToPmRamp80 = 17,
        Step18_Ramp80Completed = 18,
        Step19_ProceduresTabForAscension = 19,
        Step20_PowerAscensionPreview = 20,
        Step21_PowerAscensionActive = 21,
        Step22_AscPmSetup = 22,
        Step23_AscPmParamsEntered = 23,
        Step24_AscPmValidated = 24,
        Step25_AscPmRamp100 = 25,
        Step26_Scenario1Completed = 26
    }

    /// <summary>메인 모니터에 표시되는 화면 ID</summary>
    public enum MonitorScreenId
    {
        ProcessLibrary,
        UnitOverview,
        ModuleOverview,
        PrimaryCE,
        TripMatrix,
        PrimaryCvcHeatingSpray,
        PrimaryCvcDeminsLetdowns,
        PrimaryCvcRecirMakeup,
        PrimaryRccw,
        AnnunciatorStatus,
        InstructorStation,
        TwelveUnitOverview
    }

    /// <summary>우측 12-Unit Overview의 호기별 요약 데이터</summary>
    [Serializable]
    public class UnitSummaryData
    {
        public int unitNumber;
        public float thermalPowerPct = 99.8f;
        public float tAverageF = 543.3f;
        public float pzrPressurePsia = 1849f;
        public float pzrLevelPct = 58.5f;
        public float rcsFlowGpm = 9425f;
        public float cntPressurePsia = 0.21f;
        public float ceRadCps = 10f;
        public float carOffGasCpm = 30f;
        public int alarmCount;
        public int cautionCount;
        public int noticeCount = 1;
        public bool isTripped;
        public bool[] sfmStatus = new bool[6] { true, true, true, true, true, true };
    }

    /// <summary>경보/주의/알림 항목 데이터</summary>
    [Serializable]
    public class AlarmNotificationEntry
    {
        public string id;
        public string severity;
        public string unitText;
        public string state;
        public string shortTitle;
        public string fullDescription;
        public string plantSystem;
        public string deviceTag;
        public string setpoint;
        public string timeText;
        public MonitorScreenId targetScreen;
    }

    /// <summary>실행 중 또는 완료된 절차/자동화 카드 데이터</summary>
    [Serializable]
    public class ActiveProcessEntry
    {
        public string id;
        public string title;
        public string statusText;
        public float progressPct;
        public bool isCompleted;
    }

    /// <summary>
    /// 3개 관리자 모니터가 공유하는 NuScale E2 발전소 상태 시뮬레이션 모델 및 Capture[00..49] 단계 제어 상태 머신입니다.
    /// </summary>
    public class PlantSimulationModel
    {
        public event Action OnStateChanged;

        /// <summary>중앙 시나리오 매니저가 비활성(스킵) 단계를 건너뛸 때 사용하는 선택적 리졸버입니다.</summary>
        public Func<CaptureSequenceStep, CaptureSequenceStep> StepSkipResolver { get; set; }
        private bool mIsResolvingStep;

        // =====================================================================
        // Capture[00..49] 메인 모니터 시퀀스 및 시계 상태
        // =====================================================================
        public CaptureSequenceStep CaptureStep { get; private set; } = CaptureSequenceStep.Step00_InstructorPaused;
        public bool IsSimulatorPaused { get; private set; } = true;
        public float SimRunTimeSeconds { get; private set; }
        public int NoticeBadgeCount { get; private set; } = 1;
        public string ActiveUnitCode { get; private set; } = "06";

        /// <summary>3대 모니터 상단에 공유되는 활성 Alarm 개수입니다.</summary>
        public int ActiveAlarmCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Alarms.Count; i++)
                {
                    if (string.Equals(Alarms[i].severity, "Alarm", StringComparison.OrdinalIgnoreCase)) count++;
                }
                return count;
            }
        }

        /// <summary>3대 모니터 상단에 공유되는 활성 Caution 개수입니다.</summary>
        public int ActiveCautionCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Alarms.Count; i++)
                {
                    if (string.Equals(Alarms[i].severity, "Caution", StringComparison.OrdinalIgnoreCase)) count++;
                }
                return count;
            }
        }

        public TrainingScenarioType CurrentScenario { get; private set; } = TrainingScenarioType.Standby;
        public float TimeScaleMultiplier { get; private set; } = 1f;
        public float SimClockSeconds { get; private set; } = 8f * 3600f;

        // =====================================================================
        // 06호기 핵심 운전 변수
        // =====================================================================
        public float CoreThermalPowerPct { get; private set; } = 99.8f;
        public float CoreNiPowerPct { get; private set; } = 99.8f;
        public float RequestedPowerPct { get; private set; } = 100f;
        public float RcsTHotF { get; private set; } = 589f;
        public float RcsTColdF { get; private set; } = 498f;
        public float RcsTAveF { get; private set; } = 543.3f;
        public float PzrPressurePsia { get; private set; } = 1849f;
        public float PzrLevelPct { get; private set; } = 58.5f;
        public float RpvLevelFt { get; private set; } = 51.3f;
        public float RcsFlowGpm { get; private set; } = 9425f;
        public float CnvTempF { get; private set; } = 238f;
        public float CnvPressurePsia { get; private set; } = 0.21f;
        public float CnvLevelFt { get; private set; } = 0f;
        public float MsHeaderPressurePsia { get; private set; } = 480f;
        public float FwFlowLbS { get; private set; } = 151f;
        public float MsFlowLbS { get; private set; } = 151f;
        public float FwPumpASpeedPct { get; private set; } = 71.3f;
        public float FwPumpBSpeedPct { get; private set; } = 71.3f;
        public float FwPumpCSpeedPct { get; private set; } = 0f;
        public float FwPumpAFlowLbS { get; private set; } = 75.6f;
        public float FwPumpBFlowLbS { get; private set; } = 75.6f;
        public float GovernorPct { get; private set; } = 83.85f;
        /// <summary>좌측 모니터(Unit Overview) 터빈 전기 출력(MW, 0..60 MW 축: 100% 시 50.0 MW, 80% 시 39.5 MW)</summary>
        public float TurbineElectricMw { get; private set; } = 50.0f;
        /// <summary>중앙 모니터(Power Maneuver Running) 터빈 출력 백분율(%, 100% 시 99.8%, 80% 시 78.7%)</summary>
        public float TurbinePowerPct { get; private set; } = 99.8f;
        /// <summary>증기발생기 1 과열도(°F, 100% 시 111 °F, 80% 시 101 °F)</summary>
        public float Sg1SuperheatF { get; private set; } = 111f;
        /// <summary>증기발생기 2 과열도(°F, 100% 시 111 °F, 80% 시 101 °F)</summary>
        public float Sg2SuperheatF { get; private set; } = 111f;
        /// <summary>복수기(CND/Hotwell) 압력(psia, 100% 시 1.02 psia, 80% 시 0.89 psia)</summary>
        public float CondenserPressurePsia { get; private set; } = 1.02f;
        /// <summary>급수-주증기 유량 편차(lb/s, 100% 시 0.01 lb/s, 80% 시 0.096 lb/s)</summary>
        public float FwMsMismatchLbS { get; private set; } = 0.01f;
        public int ControlRodSteps { get; private set; } = 201;
        public int ControlRodOuterSteps { get; private set; } = 201;

        // =====================================================================
        // CE / CVC / RCCW 계통 변수 (시나리오 2 호환)
        // =====================================================================
        public float CeRt1011Cps { get; private set; } = 10.1f;
        public float CeRt1018Cps { get; private set; } = 10.0f;
        public float CeRt1007Cps { get; private set; } = 9.96f;
        public float RcsLeakRateGpm { get; private set; } = 0.38f;
        public float CeSampleVesselLevelFt { get; private set; } = 0f;
        public float CvcLetdownFlowGpm { get; private set; } = 0f;
        public float CvcMakeupFlowGpm { get; private set; } = 0f;
        public float RccwExpansionTankLevelPct { get; private set; } = 45.2f;
        public bool IsLocaActive { get; private set; }
        public bool IsAutoLetdownEnabled { get; private set; } = true;
        public bool IsLetdownLockUnlocked { get; private set; }
        public bool IsRecircLockUnlocked { get; private set; }
        public bool IsCvcRecircPumpARunning { get; private set; } = true;
        public bool IsCvcRecircPumpBRunning { get; private set; }
        public bool IsCvcMakeupBlendSelected { get; private set; }
        public bool IsCvcMakeupValidated { get; private set; }
        public bool IsCvcMakeupExecuted { get; private set; }
        public bool HasRequestedHotwellSample { get; private set; }
        public bool HasRequestedRccwIsolation { get; private set; }

        // =====================================================================
        // Process Library & Power Maneuvering 상태
        // =====================================================================
        public string SelectedProcedureId { get; private set; } = string.Empty;
        public float PendingTargetPowerPct { get; private set; } = 100f;
        public float PendingRampRatePctHr { get; private set; } = 20f;
        public bool PendingUseDilution { get; private set; }
        public bool IsPowerManeuverValidated { get; private set; }
        public bool IsPowerManeuverRunning { get; private set; }
        public float PowerManeuverInitialPct { get; private set; } = 99.8f;
        public float PowerManeuverProgressPct { get; private set; }
        public string PowerManeuverDurationText { get; private set; } = "0h 0m 33s";
        public string PowerManeuverStatusText { get; private set; } = string.Empty;
        public bool HasReached80Percent { get; private set; }
        public bool HasReached100PercentAfterReduction { get; private set; }

        // Power Maneuvering 실시간 변화 타임라인 상태 (감소/상승 각각 기본 60초, 인스펙터에서 조정 가능)
        public float PowerManeuverDurationSeconds { get; private set; } = 60f;
        public float PowerAscensionDurationSeconds { get; private set; } = 60f;
        public float PowerManeuverElapsedSeconds { get; private set; }
        public float PowerManeuverNormalizedProgress { get; private set; }
        public bool IsRunChartAxesVisible { get; private set; }
        public float RunChartVisibleRatio { get; private set; }
        public int ControlRodInnerSteps { get; private set; } = 201;
        public int ControlRodOuterCrossSteps { get; private set; } = 201;
        public float RunChartDisplayClockSeconds { get; private set; } = 8f * 3600f + 59f;

        // =====================================================================
        // Trip Matrix 스위치/트립 상태
        // =====================================================================
        public bool IsTripSwitchUnlocked { get; private set; }
        public bool IsRts1Active { get; private set; }
        public bool IsRts2Active { get; private set; }
        public bool IsNse1Active { get; private set; }
        public bool IsNse2Active { get; private set; }
        public bool IsRxTripActuated { get; private set; }
        public bool IsCisActuated { get; private set; }
        public bool IsCvcsiActuated { get; private set; }
        public bool HasCompletedTripSwitchReturn { get; private set; }

        public string SelectedObjectTag { get; private set; } = "06_CVC_ISV_0331";
        public string SelectedObjectDesc { get; private set; } = "CNT Module 06 RCS Injection Inboard Containment Isolation Valve";
        public string SelectedObjectValueText { get; private set; } = "100.0 %";
        public string SelectedObjectStateText { get; private set; } = "Opened";
        public bool SelectedObjectIsValve { get; private set; } = true;

        public UnitSummaryData[] AllUnits { get; } = new UnitSummaryData[12];
        public List<AlarmNotificationEntry> Alarms { get; } = new List<AlarmNotificationEntry>();
        public List<ActiveProcessEntry> Processes { get; } = new List<ActiveProcessEntry>();

        public List<float> PowerHistory { get; } = new List<float>();
        public List<float> NiPowerHistory { get; } = new List<float>();
        public List<float> TurbineMwHistory { get; } = new List<float>();
        public List<float> TAveHistory { get; } = new List<float>();
        public List<float> THotHistory { get; } = new List<float>();
        public List<float> TColdHistory { get; } = new List<float>();
        public List<float> PzrPressHistory { get; } = new List<float>();
        public List<float> MsPressHistory { get; } = new List<float>();

        private readonly Dictionary<string, bool> mValveStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> mValveDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, bool> mProcedureStepChecked = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, bool> mProcedureStepNa = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private float mHistorySampleTimer;

        public PlantSimulationModel()
        {
            for (int i = 0; i < 12; i++)
            {
                AllUnits[i] = new UnitSummaryData { unitNumber = i + 1 };
            }
            seedValveMetadata();
            resetToStandby();
        }

        private void seedValveMetadata()
        {
            mValveDescriptions["CE-AOV-0130"] = "CE Module Gaseous Discharge Flow to GRW Isolation Valve";
            mValveDescriptions["CE-AOV-0128"] = "CE Module Gaseous Discharge Flow to RBV Isolation Valve";
            mValveDescriptions["CVC-MOV-0318"] = "CVC Module Injection Isolation Valve";
            mValveDescriptions["CVC-AOV-0339"] = "CVC Spray Header Isolation Valve";
            mValveDescriptions["CVC-ISV-0334"] = "CVC Letdown Outboard Containment Isolation Valve";
            mValveDescriptions["CVC-ISV-0336"] = "CVC Letdown Inboard Containment Isolation Valve";
            mValveDescriptions["CVC-ISV-0323"] = "CVC Letdown Isolation Valve 0323";
            mValveDescriptions["CVC-ISV-0325"] = "CVC PZR Spray Containment Isolation Valve";
            mValveDescriptions["CVC-ISV-0329"] = "CVC RCS Injection Containment Isolation Valve (Outboard)";
            mValveDescriptions["CVC-ISV-0331"] = "CVC RCS Injection Containment Isolation Valve (Inboard)";
            mValveDescriptions["FW-AOV-0117"] = "Feedwater Isolation Valve A";
            mValveDescriptions["MS-AOV-0102"] = "Main Steam Isolation Valve A";
        }

        /// <summary>Capture[00] 초기 스탠바이 상태(Instructor Station Paused, 8:00:00 AM, Notice=1)로 초기화합니다.</summary>
        public void resetToStandby()
        {
            try
            {
                CurrentScenario = TrainingScenarioType.Standby;
                CaptureStep = CaptureSequenceStep.Step00_InstructorPaused;
                IsSimulatorPaused = true;
                SimRunTimeSeconds = 0f;
                SimClockSeconds = 8f * 3600f;
                ActiveUnitCode = "06";
                NoticeBadgeCount = 1;
                TimeScaleMultiplier = 1f;

                applyNormal100PercentValues();
                resetProcedureAndProcessLists();
                // 정상 상태(Standby / Scenario 1)에서 12-Unit Overview 전 호기가 이상 표시 없이 깔끔하도록 초기화
                for (int i = 0; i < AllUnits.Length; i++)
                {
                    var u = AllUnits[i];
                    u.unitNumber = i + 1;
                    u.thermalPowerPct = 99.8f;
                    u.tAverageF = 543.3f;
                    u.pzrPressurePsia = 1849f;
                    u.pzrLevelPct = 58.5f;
                    u.rcsFlowGpm = 9425f;
                    u.cntPressurePsia = 0.21f;
                    u.ceRadCps = 10f;
                    u.carOffGasCpm = 30f;
                    u.alarmCount = 0;
                    u.cautionCount = 0;
                    u.noticeCount = 0;
                    u.isTripped = false;
                    for (int k = 0; k < u.sfmStatus.Length; k++) u.sfmStatus[k] = true;
                }
                PendingTargetPowerPct = 100f;
                PendingRampRatePctHr = 20f;
                PendingUseDilution = false;
                PowerManeuverDurationText = "0h 0m 33s";
                PowerManeuverStatusText = string.Empty;
                PowerManeuverProgressPct = 0f;
                SelectedProcedureId = string.Empty;

                Alarms.Clear();
                seedHistoryBuffers();
                selectComponent("06_CVC_ISV_0331", "CNT Module 06 RCS Injection Inboard Containment Isolation Valve", "100.0 %", "Opened", false);
                syncUnit06Summary();
                notifyChanged();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlantSimulationModel] resetToStandby 오류: {ex}");
            }
        }

        /// <summary>3대 모니터 상단 바에 표시되는 선택 호기 번호("01".."12")를 변경하고 즉시 동기화합니다.</summary>
        public void setActiveUnitCode(string unitCode)
        {
            if (string.IsNullOrEmpty(unitCode)) return;
            ActiveUnitCode = unitCode;
            notifyChanged();
        }

        /// <summary>Capture[00 -> 01]: Instructor Station에서 Play(▶) 버튼을 눌러 시뮬레이터를 실행합니다.</summary>
        public bool startSimulatorRun()
        {
            try
            {
                IsSimulatorPaused = false;
                if (CaptureStep == CaptureSequenceStep.Step00_InstructorPaused)
                {
                    SimRunTimeSeconds = 7f;
                    SimClockSeconds = 8f * 3600f + 7f;
                    setCaptureStep(CaptureSequenceStep.Step01_InstructorRunning);
                    return true;
                }
                CurrentScenario = TrainingScenarioType.Scenario1PowerAdjustment;
                notifyChanged();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlantSimulationModel] startSimulatorRun 오류: {ex}");
                return false;
            }
        }

        /// <summary>Capture[02..44] 각 의도된 클릭 단계에 맞춰 상태 머신을 정확히 전진시킵니다. StepSkipResolver가 설정되면 비활성 단계를 자동 건너뜁니다.</summary>
        public void setCaptureStep(CaptureSequenceStep nextStep)
        {
            try
            {
                if (!mIsResolvingStep && StepSkipResolver != null)
                {
                    mIsResolvingStep = true;
                    try
                    {
                        CaptureSequenceStep resolved = StepSkipResolver(nextStep);
                        if ((int)resolved > (int)nextStep && (int)resolved - (int)nextStep <= 10)
                        {
                            for (int s = (int)nextStep; s <= (int)resolved; s++)
                            {
                                applyCaptureStepSingle((CaptureSequenceStep)s);
                            }
                        }
                        else
                        {
                            applyCaptureStepSingle(resolved);
                        }
                        notifyChanged();
                        return;
                    }
                    finally
                    {
                        mIsResolvingStep = false;
                    }
                }

                applyCaptureStepSingle(nextStep);
                notifyChanged();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlantSimulationModel] setCaptureStep({nextStep}) 오류: {ex}");
            }
        }

        private void applyCaptureStepSingle(CaptureSequenceStep nextStep)
        {
            CaptureStep = nextStep;
            if (nextStep >= CaptureSequenceStep.Step01_InstructorRunning)
            {
                IsSimulatorPaused = false;
                CurrentScenario = TrainingScenarioType.Scenario1PowerAdjustment;
            }

            switch (nextStep)
                {
                    case CaptureSequenceStep.Step05_PowerReductionPreview:
                        SelectedProcedureId = "C_PowerReduction";
                        break;
                    case CaptureSequenceStep.Step06_PowerReductionActive:
                        SelectedProcedureId = "C_PowerReduction";
                        updateProcessProgress("PROC_C_PWR_RED", "Unit06 : C. Power Reduction", 0.0f, false);
                        break;
                    case CaptureSequenceStep.Step07_Step1Checked:
                        mProcedureStepChecked["1"] = true;
                        updateProcessProgress("PROC_C_PWR_RED", "Unit06 : C. Power Reduction", 14.3f, false);
                        break;
                    case CaptureSequenceStep.Step09_Step3NaCollapsed:
                        mProcedureStepNa["3"] = true;
                        updateProcessProgress("PROC_C_PWR_RED", "Unit06 : C. Power Reduction", 28.6f, false);
                        break;
                    case CaptureSequenceStep.Step10_PowerManeuveringSetup:
                        SelectedProcedureId = "PowerManeuvering";
                        PendingTargetPowerPct = 100f;
                        PendingRampRatePctHr = 20f;
                        PendingUseDilution = false;
                        PowerManeuverDurationText = "0h 0m 33s";
                        IsPowerManeuverValidated = false;
                        PowerManeuverStatusText = string.Empty;
                        break;
                    case CaptureSequenceStep.Step11_PmParamsEntered:
                        SelectedProcedureId = "PowerManeuvering";
                        PendingTargetPowerPct = 80f;
                        PendingRampRatePctHr = 300f;
                        PendingUseDilution = false;
                        PowerManeuverDurationText = "0h 3m 57s";
                        IsPowerManeuverValidated = false;
                        PowerManeuverStatusText = string.Empty;
                        break;
                    case CaptureSequenceStep.Step12_PmValidated:
                        validatePowerManeuver(80f, 300f, false);
                        break;
                    case CaptureSequenceStep.Step13_PmRunningCardView:
                        if (!IsPowerManeuverValidated) validatePowerManeuver(80f, 300f, false);
                        executePowerManeuver();
                        break;
                    case CaptureSequenceStep.Step14_ReturnedToProcCard:
                        SelectedProcedureId = "C_PowerReduction";
                        break;
                    case CaptureSequenceStep.Step15_Step6Checked:
                        mProcedureStepChecked["6"] = true;
                        updateProcessProgress("PROC_C_PWR_RED", "Unit06 : C. Power Reduction", 42.9f, false);
                        break;
                    case CaptureSequenceStep.Step16_Step8Checked:
                        mProcedureStepChecked["8"] = true;
                        updateProcessProgress("PROC_C_PWR_RED", "Unit06 : C. Power Reduction", 57.1f, false);
                        break;
                    case CaptureSequenceStep.Step17_ReturnedToPmRamp80:
                        SelectedProcedureId = "PowerManeuvering";
                        if (!IsPowerManeuverRunning && !HasReached80Percent)
                        {
                            PendingTargetPowerPct = 80f;
                            PendingRampRatePctHr = 300f;
                            IsPowerManeuverValidated = true;
                            RequestedPowerPct = 80f;
                            PowerManeuverInitialPct = 99.8f;
                            IsPowerManeuverRunning = true;
                        }
                        if (!HasReached80Percent)
                        {
                            PowerManeuverInitialPct = 99.8f;
                            RequestedPowerPct = 80f;
                            IsPowerManeuverRunning = true;
                            resetPowerReductionTimelineState();
                            applyPowerReductionTimeline(0f);
                            seedHistoryBuffers();
                        }
                        break;
                    case CaptureSequenceStep.Step18_Ramp80Completed:
                        SelectedProcedureId = "PowerManeuvering";
                        HasReached80Percent = true;
                        IsPowerManeuverRunning = false;
                        break;
                    case CaptureSequenceStep.Step19_ProceduresTabForAscension:
                        SelectedProcedureId = string.Empty;
                        break;
                    case CaptureSequenceStep.Step20_PowerAscensionPreview:
                        SelectedProcedureId = "B_PowerAscension";
                        break;
                    case CaptureSequenceStep.Step21_PowerAscensionActive:
                        SelectedProcedureId = "B_PowerAscension";
                        updateProcessProgress("PROC_B_PWR_ASC", "Unit06 : B. Power Ascension", 0.0f, false);
                        break;
                    case CaptureSequenceStep.Step22_AscPmSetup:
                        SelectedProcedureId = "PowerManeuvering";
                        PendingTargetPowerPct = 80f;
                        PendingRampRatePctHr = 300f;
                        PendingUseDilution = false;
                        IsPowerManeuverValidated = false;
                        PowerManeuverStatusText = string.Empty;
                        PowerManeuverDurationText = "0h 0m 0s";
                        break;
                    case CaptureSequenceStep.Step23_AscPmParamsEntered:
                        SelectedProcedureId = "PowerManeuvering";
                        PendingTargetPowerPct = 100f;
                        PendingRampRatePctHr = 20f;
                        PendingUseDilution = false;
                        IsPowerManeuverValidated = false;
                        PowerManeuverStatusText = string.Empty;
                        PowerManeuverDurationText = formatDurationText(Mathf.Abs(CoreThermalPowerPct - 100f) / 20f);
                        break;
                    case CaptureSequenceStep.Step24_AscPmValidated:
                        validatePowerManeuver(100f, 20f, false);
                        break;
                    case CaptureSequenceStep.Step25_AscPmRamp100:
                        SelectedProcedureId = "PowerManeuvering";
                        if (!IsPowerManeuverValidated || Mathf.Abs(PendingTargetPowerPct - 100f) > 0.5f)
                        {
                            validatePowerManeuver(100f, 20f, false);
                        }
                        executePowerManeuver();
                        break;
                    case CaptureSequenceStep.Step26_Scenario1Completed:
                        SelectedProcedureId = "PowerManeuvering";
                        HasReached80Percent = true;
                        HasReached100PercentAfterReduction = true;
                        IsPowerManeuverRunning = false;
                        applyPowerAscensionTimeline(1f);
                        updateProcessProgress("AUTO_PWR_MAN", PowerManeuverStatusText, 100.0f, true);
                        updateProcessProgress("PROC_B_PWR_ASC", "Unit06 : B. Power Ascension", 100.0f, true);
                        break;
                }
        }

        public void startScenario1PowerAdjustment()
        {
            try
            {
                CurrentScenario = TrainingScenarioType.Scenario1PowerAdjustment;
                IsSimulatorPaused = false;
                TimeScaleMultiplier = 1f;
                applyNormal100PercentValues();
                resetProcedureAndProcessLists();
                Alarms.Clear();

                SelectedProcedureId = "C_PowerReduction";
                PendingTargetPowerPct = 80f;
                PendingRampRatePctHr = 300f;
                PendingUseDilution = false;
                IsPowerManeuverValidated = false;
                IsPowerManeuverRunning = false;
                HasReached80Percent = false;
                HasReached100PercentAfterReduction = false;
                PowerManeuverStatusText = string.Empty;
                PowerManeuverDurationText = "0h 3m 58s";

                seedHistoryBuffers();
                syncUnit06Summary();
                notifyChanged();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlantSimulationModel] startScenario1PowerAdjustment 오류: {ex}");
            }
        }

        public void startScenario2LocaEmergency()
        {
            try
            {
                CurrentScenario = TrainingScenarioType.Scenario2LocaEmergency;
                IsSimulatorPaused = false;
                TimeScaleMultiplier = 1f;
                applyNormal100PercentValues();
                resetProcedureAndProcessLists();

                IsLocaActive = true;
                CeRt1011Cps = 54.2f;
                CeRt1018Cps = 54.1f;
                CeRt1007Cps = 54.2f;
                RcsLeakRateGpm = 22.4f;
                CeSampleVesselLevelFt = 0.08f;
                PzrLevelPct = 56.8f;
                CnvPressurePsia = 1.08f;
                CnvLevelFt = 0.12f;
                CvcMakeupFlowGpm = 8.33f;
                CvcLetdownFlowGpm = 7.09f;
                RccwExpansionTankLevelPct = 21.7f;

                mValveStates["CE-AOV-0130"] = false;
                mValveStates["CE-AOV-0128"] = true;
                mValveStates["CVC-MOV-0318"] = true;
                mValveStates["CVC-AOV-0339"] = true;
                mValveStates["CVC-ISV-0334"] = true;
                mValveStates["CVC-ISV-0336"] = true;

                Alarms.Clear();
                Alarms.Add(new AlarmNotificationEntry
                {
                    id = "ALM_CE_RAD_HI",
                    severity = "Alarm",
                    unitText = "06",
                    state = "Active",
                    shortTitle = "CE gaseous discharge radioactivity Hi",
                    fullDescription = "Containment Evacuation gaseous discharge radiation monitor exceeded High setpoint (50 cps).",
                    plantSystem = "CE",
                    deviceTag = "06_CE_RT_1011",
                    setpoint = "> 50 cps",
                    timeText = getFormattedClock(),
                    targetScreen = MonitorScreenId.PrimaryCE
                });
                Alarms.Add(new AlarmNotificationEntry
                {
                    id = "CAU_RCS_LEAK",
                    severity = "Caution",
                    unitText = "06",
                    state = "Active",
                    shortTitle = "Small Charging Line Break Suspected",
                    fullDescription = "RCS inventory loss with rising CNV pressure and CE sample vessel level.",
                    plantSystem = "CVC",
                    deviceTag = "06_RCS_LEAK_CALC",
                    setpoint = "> 1.0 gpm",
                    timeText = getFormattedClock(),
                    targetScreen = MonitorScreenId.ModuleOverview
                });

                seedHistoryBuffers();
                syncUnit06Summary();
                notifyChanged();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlantSimulationModel] startScenario2LocaEmergency 오류: {ex}");
            }
        }

        private void applyNormal100PercentValues()
        {
            CoreThermalPowerPct = 99.8f;
            CoreNiPowerPct = 99.8f;
            RequestedPowerPct = 100f;
            RcsTHotF = 589f;
            RcsTColdF = 498f;
            RcsTAveF = 543.3f;
            PzrPressurePsia = 1849f;
            PzrLevelPct = 58.5f;
            RpvLevelFt = 51.3f;
            RcsFlowGpm = 9425f;
            CnvTempF = 238f;
            CnvPressurePsia = 0.21f;
            CnvLevelFt = 0f;
            MsHeaderPressurePsia = 480f;
            FwFlowLbS = 151.2f;
            MsFlowLbS = 151.2f;
            FwPumpAFlowLbS = 75.6f;
            FwPumpBFlowLbS = 75.6f;
            FwPumpASpeedPct = 71.30f;
            FwPumpBSpeedPct = 71.30f;
            FwPumpCSpeedPct = 0f;
            GovernorPct = 83.85f;
            TurbineElectricMw = 50.0f;
            TurbinePowerPct = 99.8f;
            Sg1SuperheatF = 111f;
            Sg2SuperheatF = 111f;
            CondenserPressurePsia = 1.02f;
            FwMsMismatchLbS = 0.01f;
            ControlRodSteps = 201;
            ControlRodOuterSteps = 201;
            ControlRodInnerSteps = 201;
            ControlRodOuterCrossSteps = 201;
            resetPowerReductionTimelineState();

            CeRt1011Cps = 10.1f;
            CeRt1018Cps = 10.0f;
            CeRt1007Cps = 9.96f;
            RcsLeakRateGpm = 0.38f;
            CeSampleVesselLevelFt = 0f;
            CvcLetdownFlowGpm = 0f;
            CvcMakeupFlowGpm = 0f;
            RccwExpansionTankLevelPct = 45.2f;
            IsLocaActive = false;
            IsAutoLetdownEnabled = true;
            IsLetdownLockUnlocked = false;
            IsRecircLockUnlocked = false;
            IsCvcRecircPumpARunning = true;
            IsCvcRecircPumpBRunning = false;
            IsCvcMakeupBlendSelected = false;
            IsCvcMakeupValidated = false;
            IsCvcMakeupExecuted = false;
            HasRequestedHotwellSample = false;
            HasRequestedRccwIsolation = false;

            IsPowerManeuverValidated = false;
            IsPowerManeuverRunning = false;
            HasReached80Percent = false;
            HasReached100PercentAfterReduction = false;

            IsTripSwitchUnlocked = false;
            IsRts1Active = false;
            IsRts2Active = false;
            IsNse1Active = false;
            IsNse2Active = false;
            IsRxTripActuated = false;
            IsCisActuated = false;
            IsCvcsiActuated = false;
            HasCompletedTripSwitchReturn = false;

            mValveStates["CE-AOV-0130"] = false;
            mValveStates["CE-AOV-0128"] = true;
            mValveStates["CVC-MOV-0318"] = true;
            mValveStates["CVC-AOV-0339"] = true;
            mValveStates["CVC-ISV-0334"] = true;
            mValveStates["CVC-ISV-0336"] = true;
            mValveStates["CVC-ISV-0323"] = false;
            mValveStates["CVC-ISV-0325"] = false;
            mValveStates["CVC-ISV-0329"] = false;
            mValveStates["CVC-ISV-0331"] = false;
            mValveStates["FW-AOV-0117"] = true;
            mValveStates["MS-AOV-0102"] = true;

            for (int i = 0; i < 12; i++)
            {
                AllUnits[i].thermalPowerPct = 99.8f;
                AllUnits[i].tAverageF = 543.3f;
                AllUnits[i].pzrPressurePsia = 1849f;
                AllUnits[i].pzrLevelPct = 58.5f;
                AllUnits[i].rcsFlowGpm = 9425f;
                AllUnits[i].cntPressurePsia = 0.21f;
                AllUnits[i].ceRadCps = 10f;
                AllUnits[i].carOffGasCpm = 30f;
                AllUnits[i].alarmCount = 0;
                AllUnits[i].cautionCount = 0;
                AllUnits[i].noticeCount = 1;
                AllUnits[i].isTripped = false;
                for (int s = 0; s < 6; s++) AllUnits[i].sfmStatus[s] = true;
            }
        }

        private void resetProcedureAndProcessLists()
        {
            mProcedureStepChecked.Clear();
            mProcedureStepNa.Clear();
            Processes.Clear();
        }

        private void seedHistoryBuffers()
        {
            PowerHistory.Clear();
            NiPowerHistory.Clear();
            TurbineMwHistory.Clear();
            TAveHistory.Clear();
            THotHistory.Clear();
            TColdHistory.Clear();
            PzrPressHistory.Clear();
            MsPressHistory.Clear();
            for (int i = 0; i < 24; i++)
            {
                PowerHistory.Add(CoreThermalPowerPct);
                NiPowerHistory.Add(CoreNiPowerPct);
                TurbineMwHistory.Add(TurbineElectricMw);
                TAveHistory.Add(RcsTAveF);
                THotHistory.Add(RcsTHotF);
                TColdHistory.Add(RcsTColdF);
                PzrPressHistory.Add(PzrPressurePsia);
                MsPressHistory.Add(MsHeaderPressurePsia);
            }
        }

        /// <summary>현재 시뮬레이션 계측치 스냅샷을 좌·중 모니터 공유 히스토리 버퍼에 동시에 추가합니다.</summary>
        private void pushAllHistorySnapshots()
        {
            pushHistoryPoint(PowerHistory, CoreThermalPowerPct);
            pushHistoryPoint(NiPowerHistory, CoreNiPowerPct);
            pushHistoryPoint(TurbineMwHistory, TurbineElectricMw);
            pushHistoryPoint(TAveHistory, RcsTAveF);
            pushHistoryPoint(THotHistory, RcsTHotF);
            pushHistoryPoint(TColdHistory, RcsTColdF);
            pushHistoryPoint(PzrPressHistory, PzrPressurePsia);
            pushHistoryPoint(MsPressHistory, MsHeaderPressurePsia);
        }

        /// <summary>Power Maneuvering 출력 감소 전체 소요 시간(초)을 설정합니다(최소 1초).</summary>
        public void setPowerManeuverDurationSeconds(float seconds)
        {
            try
            {
                PowerManeuverDurationSeconds = Mathf.Max(1f, seconds);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlantSimulationModel] setPowerManeuverDurationSeconds 오류: {ex}");
            }
        }

        /// <summary>Power Maneuvering 출력 상승(80%→100%) 전체 소요 시간(초)을 설정합니다(최소 1초).</summary>
        public void setPowerAscensionDurationSeconds(float seconds)
        {
            try
            {
                PowerAscensionDurationSeconds = Mathf.Max(1f, seconds);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlantSimulationModel] setPowerAscensionDurationSeconds 오류: {ex}");
            }
        }

        private void resetPowerReductionTimelineState()
        {
            PowerManeuverElapsedSeconds = 0f;
            PowerManeuverNormalizedProgress = 0f;
            IsRunChartAxesVisible = false;
            RunChartVisibleRatio = 0f;
            RunChartDisplayClockSeconds = 8f * 3600f + 59f;
            mHistorySampleTimer = 0f;
        }

        public void setTimeScaleMultiplier(float multiplier)
        {
            TimeScaleMultiplier = Mathf.Clamp(multiplier, 1f, 120f);
            notifyChanged();
        }

        public void tickSimulation(float deltaTime)
        {
            try
            {
                if (deltaTime <= 0f || IsSimulatorPaused) return;
                float scaledDt = deltaTime * TimeScaleMultiplier;
                SimClockSeconds += scaledDt;
                SimRunTimeSeconds += scaledDt;

                bool changed = true;
                bool handledTimelineHistory = false;

                // Capture[35..49] / 시나리오 1: Power Maneuvering 자동 출력 램프
                if (IsPowerManeuverRunning && !IsRxTripActuated)
                {
                    bool holdingForProcedureCheck = CaptureStep >= CaptureSequenceStep.Step13_PmRunningCardView
                                                    && CaptureStep <= CaptureSequenceStep.Step16_Step8Checked;

                    if (holdingForProcedureCheck)
                    {
                        // Execute 후 절차서 확인 중에는 이미지 2 시작 상태를 유지하고, Power Maneuvering 복귀 시 1분 변화를 처음부터 진행
                        handledTimelineHistory = true;
                    }
                    else
                    {
                        bool isTimelineReduction = RequestedPowerPct < PowerManeuverInitialPct
                                                   && Mathf.Abs(RequestedPowerPct - 80f) < 1f;
                        bool isTimelineAscension = RequestedPowerPct > PowerManeuverInitialPct
                                                   && Mathf.Abs(RequestedPowerPct - 100f) < 1f;

                        float duration = isTimelineAscension
                            ? Mathf.Max(1f, PowerAscensionDurationSeconds)
                            : Mathf.Max(1f, PowerManeuverDurationSeconds);

                        PowerManeuverElapsedSeconds = Mathf.Min(duration, PowerManeuverElapsedSeconds + scaledDt);
                        float u = Mathf.Clamp01(PowerManeuverElapsedSeconds / duration);
                        PowerManeuverNormalizedProgress = u;

                        if (isTimelineReduction)
                        {
                            applyPowerReductionTimeline(u);
                            handledTimelineHistory = true;

                            if (u < 0.05f)
                            {
                                mHistorySampleTimer = 0f;
                            }
                            else
                            {
                                float sampleInterval = u < 0.78f
                                    ? Mathf.Max(0.02f, (0.73f * duration) / 23f)
                                    : Mathf.Max(0.02f, (0.22f * duration) / 24f);

                                mHistorySampleTimer += scaledDt;
                                while (mHistorySampleTimer >= sampleInterval)
                                {
                                    mHistorySampleTimer -= sampleInterval;
                                    pushAllHistorySnapshots();
                                }
                            }

                            updateProcessProgress("AUTO_PWR_MAN", PowerManeuverStatusText, PowerManeuverProgressPct, false);

                            if (u >= 1f)
                            {
                                applyPowerReductionTimeline(1f);
                                IsPowerManeuverRunning = false;
                                HasReached80Percent = true;
                                updateProcessProgress("AUTO_PWR_MAN", PowerManeuverStatusText, 85.7f, false);
                                if (CaptureStep == CaptureSequenceStep.Step17_ReturnedToPmRamp80)
                                {
                                    CaptureStep = CaptureSequenceStep.Step18_Ramp80Completed;
                                }
                            }
                        }
                        else if (isTimelineAscension)
                        {
                            applyPowerAscensionTimeline(u);
                            handledTimelineHistory = true;

                            float sampleInterval = Mathf.Max(0.02f, duration / 24f);
                            mHistorySampleTimer += scaledDt;
                            while (mHistorySampleTimer >= sampleInterval)
                            {
                                mHistorySampleTimer -= sampleInterval;
                                pushAllHistorySnapshots();
                            }

                            updateProcessProgress("AUTO_PWR_MAN", PowerManeuverStatusText, PowerManeuverProgressPct, false);

                            if (u >= 1f)
                            {
                                applyPowerAscensionTimeline(1f);
                                pushAllHistorySnapshots();
                                IsPowerManeuverRunning = false;
                                HasReached100PercentAfterReduction = true;
                                updateProcessProgress("AUTO_PWR_MAN", PowerManeuverStatusText, 100.0f, true);
                                updateProcessProgress("PROC_B_PWR_ASC", "Unit06 : B. Power Ascension", 100.0f, true);
                                if (CaptureStep == CaptureSequenceStep.Step25_AscPmRamp100)
                                {
                                    CaptureStep = CaptureSequenceStep.Step26_Scenario1Completed;
                                }
                            }
                        }
                        else
                        {
                            float nextPower = Mathf.Lerp(PowerManeuverInitialPct, RequestedPowerPct, u);
                            applyPowerCoupledPhysics(nextPower);

                            float progress = u >= 0.99f ? 85.7f : Mathf.Max(7.1f, u * 85.7f);
                            PowerManeuverProgressPct = progress;
                            IsRunChartAxesVisible = true;
                            RunChartVisibleRatio = 1f;

                            string statusLine = u < 0.04f
                                ? "Begin Ramp"
                                : (u < 0.96f
                                    ? $"Ramping Feedwater Pumps to {RequestedPowerPct:F1}% Thermal Power"
                                    : "Stabilizing Maneuver (15+ minutes)");
                            PowerManeuverStatusText = statusLine;
                            updateProcessProgress("AUTO_PWR_MAN", statusLine, progress, false);

                            if (u >= 1f)
                            {
                                CoreThermalPowerPct = RequestedPowerPct;
                                applyPowerCoupledPhysics(CoreThermalPowerPct);
                                IsPowerManeuverRunning = false;
                                PowerManeuverProgressPct = 85.7f;
                                PowerManeuverStatusText = "Stabilizing Maneuver (15+ minutes)";
                                updateProcessProgress("AUTO_PWR_MAN", PowerManeuverStatusText, 85.7f, false);

                                if (Mathf.Abs(RequestedPowerPct - 80f) < 1f) HasReached80Percent = true;
                                if (HasReached80Percent && Mathf.Abs(RequestedPowerPct - 100f) < 1f) HasReached100PercentAfterReduction = true;
                            }
                        }
                    }
                }

                if (IsLocaActive && !IsRxTripActuated)
                {
                    bool ceRoutedToGrw = getValveOpen("CE-AOV-0130") && !getValveOpen("CE-AOV-0128");
                    if (!ceRoutedToGrw) CeRt1011Cps = Mathf.Min(98f, CeRt1011Cps + 0.08f * scaledDt);
                    if (getValveOpen("CVC-MOV-0318") || getValveOpen("CVC-AOV-0339"))
                    {
                        CnvPressurePsia = Mathf.Min(7.8f, CnvPressurePsia + 0.008f * scaledDt);
                        PzrLevelPct = Mathf.Max(18f, PzrLevelPct - 0.015f * scaledDt);
                    }
                }

                if (!handledTimelineHistory)
                {
                    mHistorySampleTimer += scaledDt;
                    if (mHistorySampleTimer >= 1.2f)
                    {
                        mHistorySampleTimer = 0f;
                        pushAllHistorySnapshots();
                    }
                }

                if (changed)
                {
                    syncUnit06Summary();
                    notifyChanged();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlantSimulationModel] tickSimulation 오류: {ex}");
            }
        }

        /// <summary>정규화된 진행률 u(0..1)에 따라 100%→80% 감발 단계의 계측치·제어봉·차트 상태를 갱신합니다.</summary>
        private void applyPowerReductionTimeline(float u)
        {
            u = Mathf.Clamp01(u);

            if (u < 0.05f)
            {
                // 구간 1 (이미지 2 → 이미지 3): 빈 차트에서 Y축/초기 라인 등장
                IsRunChartAxesVisible = u >= 0.035f;
                RunChartVisibleRatio = u < 0.035f ? 0f : Mathf.Lerp(0.18f, 0.22f, Mathf.InverseLerp(0.035f, 0.05f, u));
                RunChartDisplayClockSeconds = Mathf.Lerp(8f * 3600f + 59f, 8f * 3600f + 60f, u / 0.05f);

                CoreThermalPowerPct = 99.8f;
                GovernorPct = 82.87f;
                FwPumpASpeedPct = 70.66f;
                FwPumpBSpeedPct = 70.66f;
                FwFlowLbS = 150f;
                MsFlowLbS = 151f;
                TurbinePowerPct = 99.8f;
                PzrPressurePsia = 1849f;
                RcsTAveF = 544f;
                ControlRodInnerSteps = 201;
                ControlRodOuterCrossSteps = 201;
                ControlRodSteps = 201;
                PowerManeuverProgressPct = 7.1f;
                PowerManeuverStatusText = "Ramping Feedwater Pumps to 97.8% Thermal Power";
            }
            else if (u < 0.30f)
            {
                // 구간 2 (이미지 3 → 이미지 6): 출력 95.6%, 십자 제어봉 196/197 황색 전환, 그래프 우측 하강 시작
                float p = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.05f, 0.30f, u));
                IsRunChartAxesVisible = true;
                RunChartVisibleRatio = Mathf.Lerp(0.22f, 0.55f, p);
                RunChartDisplayClockSeconds = Mathf.Lerp(8f * 3600f + 60f, 8f * 3600f + 149f, p);

                CoreThermalPowerPct = Mathf.Lerp(99.8f, 95.6f, p);
                GovernorPct = Mathf.Lerp(82.87f, 68.18f, p);
                FwPumpASpeedPct = Mathf.Lerp(70.66f, 67.41f, p);
                FwPumpBSpeedPct = FwPumpASpeedPct;
                FwFlowLbS = Mathf.Lerp(150f, 142f, p);
                MsFlowLbS = Mathf.Lerp(151f, 142f, p);
                TurbinePowerPct = Mathf.Lerp(99.8f, 94.2f, p);
                PzrPressurePsia = Mathf.Lerp(1849f, 1856f, p);
                RcsTAveF = Mathf.Lerp(544f, 545f, p);
                ControlRodInnerSteps = Mathf.RoundToInt(Mathf.Lerp(201f, 196f, p));
                ControlRodOuterCrossSteps = Mathf.RoundToInt(Mathf.Lerp(201f, 197f, p));
                ControlRodSteps = ControlRodInnerSteps;
                PowerManeuverProgressPct = Mathf.Lerp(7.1f, 21.4f, p);
                PowerManeuverStatusText = $"Ramping Feedwater Pumps to {Mathf.Max(RequestedPowerPct, CoreThermalPowerPct - 1.7f):F1}% Thermal Power";
            }
            else if (u < 0.78f)
            {
                // 구간 3 (이미지 6 → 이미지 4): 출력 79.1% 도달, 십자 제어봉 177/178, 전체 폭 하향 곡선
                float p = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.30f, 0.78f, u));
                IsRunChartAxesVisible = true;
                RunChartVisibleRatio = Mathf.Lerp(0.55f, 1.0f, Mathf.Clamp01(p * 1.4f));
                RunChartDisplayClockSeconds = Mathf.Lerp(8f * 3600f + 149f, 8f * 3600f + 963f, p);

                CoreThermalPowerPct = Mathf.Lerp(95.6f, 79.1f, p);
                GovernorPct = Mathf.Lerp(68.18f, 50.55f, p);
                FwPumpASpeedPct = Mathf.Lerp(67.41f, 60.63f, p);
                FwPumpBSpeedPct = FwPumpASpeedPct;
                FwFlowLbS = Mathf.Lerp(142f, 121f, p);
                MsFlowLbS = FwFlowLbS;
                TurbinePowerPct = Mathf.Lerp(94.2f, 79.6f, p);
                PzrPressurePsia = Mathf.Lerp(1856f, 1849f, p);
                RcsTAveF = Mathf.Lerp(545f, 544f, p);
                ControlRodInnerSteps = Mathf.RoundToInt(Mathf.Lerp(196f, 177f, p));
                ControlRodOuterCrossSteps = Mathf.RoundToInt(Mathf.Lerp(197f, 178f, p));
                ControlRodSteps = ControlRodInnerSteps;
                PowerManeuverProgressPct = Mathf.Lerp(21.4f, 85.7f, p);
                PowerManeuverStatusText = p < 0.88f
                    ? $"Ramping Feedwater Pumps to {Mathf.Max(RequestedPowerPct, CoreThermalPowerPct - 1.5f):F1}% Thermal Power"
                    : "Stabilizing Maneuver (15+ minutes)";
            }
            else
            {
                // 구간 4 (이미지 4 → 이미지 5): 79.8% 안정화, 제어봉 178/179, 급수 120.4 lb/s(각 60.2 lb/s, 60.3%), 그래프 수평 정착
                float p = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.78f, 1.0f, u));
                IsRunChartAxesVisible = true;
                RunChartVisibleRatio = 1.0f;
                RunChartDisplayClockSeconds = Mathf.Lerp(8f * 3600f + 963f, 8f * 3600f + 1847f, p);

                CoreThermalPowerPct = Mathf.Lerp(79.1f, 79.8f, p);
                GovernorPct = Mathf.Lerp(50.55f, 50.03f, p);
                FwPumpASpeedPct = Mathf.Lerp(60.63f, 60.30f, p);
                FwPumpBSpeedPct = FwPumpASpeedPct;
                FwFlowLbS = Mathf.Lerp(121f, 120.4f, p);
                MsFlowLbS = FwFlowLbS;
                TurbinePowerPct = Mathf.Lerp(79.6f, 78.7f, p);
                PzrPressurePsia = 1849f;
                RcsTAveF = 544f;
                ControlRodInnerSteps = 178;
                ControlRodOuterCrossSteps = Mathf.RoundToInt(Mathf.Lerp(178f, 179f, p));
                ControlRodSteps = 178;
                PowerManeuverProgressPct = 85.7f;
                PowerManeuverStatusText = "Stabilizing Maneuver (15+ minutes)";
            }

            applySecondaryCoupledFromPower();
        }

        /// <summary>정규화된 진행률 u(0..1)에 따라 80%→100% 출력 상승 및 전체 정상 수치/제어봉/그래프 복귀를 갱신합니다.</summary>
        private void applyPowerAscensionTimeline(float u)
        {
            u = Mathf.Clamp01(u);
            IsRunChartAxesVisible = true;
            RunChartVisibleRatio = 1.0f;
            RunChartDisplayClockSeconds = Mathf.Lerp(8f * 3600f + 1847f, 8f * 3600f + 6347f, u);

            if (u < 0.05f)
            {
                float p = Mathf.SmoothStep(0f, 1f, u / 0.05f);
                CoreThermalPowerPct = Mathf.Lerp(79.8f, 80.3f, p);
                GovernorPct = Mathf.Lerp(50.03f, 51.20f, p);
                FwPumpASpeedPct = Mathf.Lerp(60.30f, 60.70f, p);
                FwPumpBSpeedPct = FwPumpASpeedPct;
                FwFlowLbS = Mathf.Lerp(120.4f, 121.4f, p);
                MsFlowLbS = FwFlowLbS;
                TurbinePowerPct = Mathf.Lerp(78.7f, 79.4f, p);
                PzrPressurePsia = 1849f;
                RcsTAveF = 544f;
                ControlRodInnerSteps = 178;
                ControlRodOuterCrossSteps = 179;
                ControlRodSteps = 178;
                PowerManeuverProgressPct = 7.1f;
                PowerManeuverStatusText = "Ramping Feedwater Pumps to 81.5% Thermal Power";
            }
            else if (u < 0.30f)
            {
                float p = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.05f, 0.30f, u));
                CoreThermalPowerPct = Mathf.Lerp(80.3f, 85.2f, p);
                GovernorPct = Mathf.Lerp(51.20f, 59.20f, p);
                FwPumpASpeedPct = Mathf.Lerp(60.70f, 63.40f, p);
                FwPumpBSpeedPct = FwPumpASpeedPct;
                FwFlowLbS = Mathf.Lerp(121.4f, 129.0f, p);
                MsFlowLbS = FwFlowLbS;
                TurbinePowerPct = Mathf.Lerp(79.4f, 84.5f, p);
                PzrPressurePsia = Mathf.Lerp(1849f, 1844f, p);
                RcsTAveF = Mathf.Lerp(544f, 543.5f, p);
                ControlRodInnerSteps = Mathf.RoundToInt(Mathf.Lerp(178f, 184f, p));
                ControlRodOuterCrossSteps = Mathf.RoundToInt(Mathf.Lerp(179f, 185f, p));
                ControlRodSteps = ControlRodInnerSteps;
                PowerManeuverProgressPct = Mathf.Lerp(7.1f, 28.6f, p);
                PowerManeuverStatusText = $"Ramping Feedwater Pumps to {Mathf.Min(100f, CoreThermalPowerPct + 1.4f):F1}% Thermal Power";
            }
            else if (u < 0.80f)
            {
                float p = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.30f, 0.80f, u));
                CoreThermalPowerPct = Mathf.Lerp(85.2f, 99.6f, p);
                GovernorPct = Mathf.Lerp(59.20f, 83.40f, p);
                FwPumpASpeedPct = Mathf.Lerp(63.40f, 71.15f, p);
                FwPumpBSpeedPct = FwPumpASpeedPct;
                FwFlowLbS = Mathf.Lerp(129.0f, 150.8f, p);
                MsFlowLbS = FwFlowLbS;
                TurbinePowerPct = Mathf.Lerp(84.5f, 99.5f, p);
                PzrPressurePsia = Mathf.Lerp(1844f, 1849f, p);
                RcsTAveF = Mathf.Lerp(543.5f, 543.3f, p);
                ControlRodInnerSteps = Mathf.RoundToInt(Mathf.Lerp(184f, 200f, p));
                ControlRodOuterCrossSteps = Mathf.RoundToInt(Mathf.Lerp(185f, 201f, p));
                ControlRodSteps = ControlRodInnerSteps;
                PowerManeuverProgressPct = Mathf.Lerp(28.6f, 85.7f, p);
                PowerManeuverStatusText = p < 0.90f
                    ? $"Ramping Feedwater Pumps to {Mathf.Min(100f, CoreThermalPowerPct + 1.2f):F1}% Thermal Power"
                    : "Stabilizing Maneuver (15+ minutes)";
            }
            else
            {
                float p = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.80f, 1.0f, u));
                CoreThermalPowerPct = Mathf.Lerp(99.6f, 99.8f, p);
                GovernorPct = Mathf.Lerp(83.40f, 83.85f, p);
                FwPumpASpeedPct = Mathf.Lerp(71.15f, 71.30f, p);
                FwPumpBSpeedPct = FwPumpASpeedPct;
                FwFlowLbS = Mathf.Lerp(150.8f, 151.2f, p);
                MsFlowLbS = FwFlowLbS;
                TurbinePowerPct = Mathf.Lerp(99.5f, 99.8f, p);
                PzrPressurePsia = 1849f;
                RcsTAveF = 543.3f;
                ControlRodInnerSteps = p >= 0.5f ? 201 : 200;
                ControlRodOuterCrossSteps = 201;
                ControlRodSteps = ControlRodInnerSteps;
                PowerManeuverProgressPct = Mathf.Lerp(85.7f, 100.0f, p);
                PowerManeuverStatusText = "Stabilizing Maneuver (15+ minutes)";
            }

            applySecondaryCoupledFromPower();
        }

        /// <summary>현재 노심 열출력 비율(t80: 79.8%~99.8%)에 연동되는 좌측/중앙 2차 계통 계측치를 일괄 계산합니다.</summary>
        private void applySecondaryCoupledFromPower()
        {
            CoreNiPowerPct = Mathf.Clamp(CoreThermalPowerPct, 0f, 105f);
            FwPumpAFlowLbS = FwFlowLbS * 0.5f;
            FwPumpBFlowLbS = FwFlowLbS * 0.5f;
            float t80 = Mathf.InverseLerp(79.8f, 99.8f, CoreThermalPowerPct);
            RcsTHotF = Mathf.Lerp(583f, 589f, t80);
            RcsTColdF = Mathf.Lerp(505f, 498f, t80);
            MsHeaderPressurePsia = Mathf.Lerp(520f, 480f, t80);
            TurbineElectricMw = Mathf.Lerp(39.5f, 50.0f, t80);
            Sg1SuperheatF = Mathf.Lerp(101f, 111f, t80);
            Sg2SuperheatF = Mathf.Lerp(101f, 111f, t80);
            CondenserPressurePsia = Mathf.Lerp(0.89f, 1.02f, t80);
            FwMsMismatchLbS = Mathf.Lerp(0.096f, 0.01f, t80);
            ControlRodOuterSteps = 201;
        }

        private string formatDurationText(float hours)
        {
            int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(hours * 3600f));
            int h = totalSeconds / 3600;
            int m = (totalSeconds % 3600) / 60;
            int s = totalSeconds % 60;
            return $"{h}h {m}m {s}s";
        }

        private void pushHistoryPoint(List<float> buffer, float value)
        {
            buffer.Add(value);
            if (buffer.Count > 24) buffer.RemoveAt(0);
        }

        private void applyPowerCoupledPhysics(float powerPct)
        {
            CoreThermalPowerPct = Mathf.Clamp(powerPct, 0f, 105f);
            CoreNiPowerPct = Mathf.Clamp(powerPct, 0f, 105f);

            // Capture[35..49] 실측 물리 커브 (99.8% <-> 79.8%)
            float t80 = Mathf.InverseLerp(79.8f, 99.8f, CoreThermalPowerPct);
            if (CoreThermalPowerPct >= 79.2f)
            {
                FwFlowLbS = Mathf.Lerp(120.4f, 151.2f, t80);
                MsFlowLbS = FwFlowLbS;
                FwPumpAFlowLbS = FwFlowLbS * 0.5f;
                FwPumpBFlowLbS = FwFlowLbS * 0.5f;
                FwPumpASpeedPct = Mathf.Lerp(60.30f, 71.30f, t80);
                FwPumpBSpeedPct = FwPumpASpeedPct;
                GovernorPct = Mathf.Lerp(50.03f, 83.85f, t80);
                TurbinePowerPct = Mathf.Lerp(78.7f, 99.8f, t80);
                TurbineElectricMw = Mathf.Lerp(39.5f, 50.0f, t80);
                RcsTHotF = Mathf.Lerp(583f, 589f, t80);
                RcsTColdF = Mathf.Lerp(505f, 498f, t80);
                RcsTAveF = 544f - 0.7f * t80;
                PzrPressurePsia = 1849f + 7f * Mathf.Sin(t80 * Mathf.PI);
                MsHeaderPressurePsia = Mathf.Lerp(520f, 480f, t80);
                Sg1SuperheatF = Mathf.Lerp(101f, 111f, t80);
                Sg2SuperheatF = Mathf.Lerp(101f, 111f, t80);
                CondenserPressurePsia = Mathf.Lerp(0.89f, 1.02f, t80);
                FwMsMismatchLbS = Mathf.Lerp(0.096f, 0.01f, t80);
                ControlRodInnerSteps = Mathf.RoundToInt(Mathf.Lerp(178f, 201f, t80));
                ControlRodOuterCrossSteps = Mathf.RoundToInt(Mathf.Lerp(179f, 201f, t80));
                ControlRodSteps = ControlRodInnerSteps;
                ControlRodOuterSteps = 201;
            }
            else
            {
                float tLow = Mathf.Clamp01(CoreThermalPowerPct / 80f);
                FwFlowLbS = Mathf.Lerp(0f, 120.4f, tLow);
                MsFlowLbS = FwFlowLbS;
                FwPumpAFlowLbS = FwFlowLbS * 0.5f;
                FwPumpBFlowLbS = FwFlowLbS * 0.5f;
                FwPumpASpeedPct = Mathf.Lerp(0f, 60.30f, tLow);
                FwPumpBSpeedPct = FwPumpASpeedPct;
                GovernorPct = Mathf.Lerp(0f, 50.03f, tLow);
                TurbinePowerPct = Mathf.Lerp(0f, 78.7f, tLow);
                TurbineElectricMw = Mathf.Lerp(0f, 39.5f, tLow);
                RcsTHotF = Mathf.Lerp(543f, 583f, tLow);
                RcsTColdF = Mathf.Lerp(543f, 505f, tLow);
                RcsTAveF = 543.5f;
                Sg1SuperheatF = Mathf.Lerp(80f, 101f, tLow);
                Sg2SuperheatF = Mathf.Lerp(80f, 101f, tLow);
                CondenserPressurePsia = Mathf.Lerp(0.60f, 0.89f, tLow);
                FwMsMismatchLbS = 0.05f;
                ControlRodSteps = IsRxTripActuated ? 0 : Mathf.RoundToInt(Mathf.Lerp(0f, 178f, tLow));
                ControlRodInnerSteps = ControlRodSteps;
                ControlRodOuterCrossSteps = ControlRodSteps;
                ControlRodOuterSteps = IsRxTripActuated ? 0 : 201;
            }
        }

        public void toggleProcedureStep(string stepKey, bool markAsNa = false)
        {
            try
            {
                if (markAsNa)
                {
                    mProcedureStepNa[stepKey] = true;
                    mProcedureStepChecked[stepKey] = false;
                }
                else
                {
                    bool prev = mProcedureStepChecked.TryGetValue(stepKey, out bool c) && c;
                    mProcedureStepChecked[stepKey] = !prev;
                    if (mProcedureStepChecked[stepKey]) mProcedureStepNa[stepKey] = false;
                }
                notifyChanged();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlantSimulationModel] toggleProcedureStep 오류: {ex}");
            }
        }

        public bool isStepChecked(string stepKey) => mProcedureStepChecked.TryGetValue(stepKey, out bool v) && v;
        public bool isStepNa(string stepKey) => mProcedureStepNa.TryGetValue(stepKey, out bool v) && v;

        public bool validatePowerManeuver(float targetPower, float rampRate, bool useDilution)
        {
            try
            {
                PendingTargetPowerPct = targetPower;
                PendingRampRatePctHr = rampRate;
                PendingUseDilution = useDilution;

                if (float.IsNaN(targetPower) || float.IsNaN(rampRate) ||
                    targetPower < 0f || targetPower > 105f ||
                    rampRate <= 0f || rampRate > 600f)
                {
                    IsPowerManeuverValidated = false;
                    PowerManeuverStatusText = "Invalid range";
                    PowerManeuverDurationText = "--";
                    notifyChanged();
                    return false;
                }

                float deltaPct = Mathf.Abs(CoreThermalPowerPct - targetPower);
                float hours = deltaPct / rampRate;
                int totalSeconds = Mathf.RoundToInt(hours * 3600f);
                int h = totalSeconds / 3600;
                int m = (totalSeconds % 3600) / 60;
                int s = totalSeconds % 60;
                PowerManeuverDurationText = $"{h}h {m}m {s}s";
                IsPowerManeuverValidated = true;
                PowerManeuverStatusText = "Ready to start";
                notifyChanged();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlantSimulationModel] validatePowerManeuver 오류: {ex}");
                IsPowerManeuverValidated = false;
                return false;
            }
        }

        public bool executePowerManeuver()
        {
            try
            {
                if (!IsPowerManeuverValidated || IsRxTripActuated) return false;

                IsSimulatorPaused = false;
                PowerManeuverInitialPct = CoreThermalPowerPct;
                RequestedPowerPct = PendingTargetPowerPct;
                IsPowerManeuverRunning = true;
                resetPowerReductionTimelineState();

                if (RequestedPowerPct < PowerManeuverInitialPct && Mathf.Abs(RequestedPowerPct - 80f) < 1f)
                {
                    applyPowerReductionTimeline(0f);
                    seedHistoryBuffers();
                    updateProcessProgress("AUTO_PWR_MAN", PowerManeuverStatusText, PowerManeuverProgressPct, false);
                }
                else if (RequestedPowerPct > PowerManeuverInitialPct && Mathf.Abs(RequestedPowerPct - 100f) < 1f)
                {
                    applyPowerAscensionTimeline(0f);
                    seedHistoryBuffers();
                    updateProcessProgress("AUTO_PWR_MAN", PowerManeuverStatusText, PowerManeuverProgressPct, false);
                }
                else
                {
                    PowerManeuverProgressPct = 7.1f;
                    PowerManeuverStatusText = "Begin Ramp";
                    IsRunChartAxesVisible = true;
                    RunChartVisibleRatio = 1f;
                    updateProcessProgress("AUTO_PWR_MAN", "Begin Ramp", 7.1f, false);
                }

                notifyChanged();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlantSimulationModel] executePowerManeuver 오류: {ex}");
                return false;
            }
        }

        public void completePowerManeuverImmediately()
        {
            if (!IsPowerManeuverRunning && !IsPowerManeuverValidated) return;
            RequestedPowerPct = PendingTargetPowerPct;
            IsPowerManeuverRunning = false;

            if (Mathf.Abs(RequestedPowerPct - 80f) < 1f && PowerManeuverInitialPct > 85f)
            {
                PowerManeuverElapsedSeconds = PowerManeuverDurationSeconds;
                PowerManeuverNormalizedProgress = 1f;
                applyPowerReductionTimeline(1f);
                HasReached80Percent = true;
                seedHistoryBuffers();
                updateProcessProgress("AUTO_PWR_MAN", PowerManeuverStatusText, 85.7f, false);
                if (CaptureStep == CaptureSequenceStep.Step17_ReturnedToPmRamp80)
                {
                    CaptureStep = CaptureSequenceStep.Step18_Ramp80Completed;
                }
            }
            else if (Mathf.Abs(RequestedPowerPct - 100f) < 1f && PowerManeuverInitialPct < 90f)
            {
                PowerManeuverElapsedSeconds = PowerAscensionDurationSeconds;
                PowerManeuverNormalizedProgress = 1f;
                applyPowerAscensionTimeline(1f);
                HasReached100PercentAfterReduction = true;
                pushAllHistorySnapshots();
                updateProcessProgress("AUTO_PWR_MAN", PowerManeuverStatusText, 100.0f, true);
                updateProcessProgress("PROC_B_PWR_ASC", "Unit06 : B. Power Ascension", 100.0f, true);
                if (CaptureStep == CaptureSequenceStep.Step25_AscPmRamp100)
                {
                    CaptureStep = CaptureSequenceStep.Step26_Scenario1Completed;
                }
            }
            else
            {
                applyPowerCoupledPhysics(RequestedPowerPct);
                IsRunChartAxesVisible = true;
                RunChartVisibleRatio = 1f;
                PowerManeuverNormalizedProgress = 1f;
                PowerManeuverProgressPct = 85.7f;
                PowerManeuverStatusText = "Stabilizing Maneuver (15+ minutes)";
                if (Mathf.Abs(RequestedPowerPct - 80f) < 1f) HasReached80Percent = true;
                if (HasReached80Percent && Mathf.Abs(RequestedPowerPct - 100f) < 1f) HasReached100PercentAfterReduction = true;
                pushAllHistorySnapshots();
                updateProcessProgress("AUTO_PWR_MAN", "Stabilizing Maneuver (15+ minutes)", 85.7f, true);
            }

            syncUnit06Summary();
            notifyChanged();
        }

        public bool getValveOpen(string valveId) => mValveStates.TryGetValue(valveId, out bool isOpen) && isOpen;

        public void operateValve(string valveId, bool open)
        {
            try
            {
                mValveStates[valveId] = open;
                string desc = mValveDescriptions.TryGetValue(valveId, out string d) ? d : valveId;
                selectComponent(valveId, desc, open ? "100.0 %" : "0.0 %", open ? "Opened" : "Closed", true);
                if (!getValveOpen("CVC-ISV-0334") && !getValveOpen("CVC-ISV-0336")) CvcLetdownFlowGpm = 0f;
                if (!getValveOpen("CVC-MOV-0318") && !getValveOpen("CVC-AOV-0339")) RcsLeakRateGpm = Mathf.Min(RcsLeakRateGpm, 0.4f);
                syncUnit06Summary();
                notifyChanged();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlantSimulationModel] operateValve({valveId}) 오류: {ex}");
            }
        }

        public void selectComponent(string tag, string desc, string valText, string stateText, bool isValve)
        {
            SelectedObjectTag = tag;
            SelectedObjectDesc = desc;
            SelectedObjectValueText = valText;
            SelectedObjectStateText = stateText;
            SelectedObjectIsValve = isValve;
            notifyChanged();
        }

        public void setTripSwitchLock(bool unlocked)
        {
            IsTripSwitchUnlocked = unlocked;
            notifyChanged();
        }

        public bool toggleTripSwitch(string switchId)
        {
            try
            {
                if (!IsTripSwitchUnlocked) return false;
                switch (switchId)
                {
                    case "RTS_I": IsRts1Active = !IsRts1Active; break;
                    case "RTS_II": IsRts2Active = !IsRts2Active; break;
                    case "NSE_I": IsNse1Active = !IsNse1Active; break;
                    case "NSE_II": IsNse2Active = !IsNse2Active; break;
                    default: return false;
                }

                if (IsRts1Active && IsRts2Active && !IsRxTripActuated)
                {
                    IsRxTripActuated = true;
                    IsPowerManeuverRunning = false;
                    applyPowerCoupledPhysics(0f);
                    mValveStates["FW-AOV-0117"] = false;
                    mValveStates["MS-AOV-0102"] = false;
                    updateProcessProgress("TRIP_RTS", "Manual Reactor Trip (RTS I/II)", 100f, true);
                }

                if (IsRxTripActuated && !IsRts1Active && !IsRts2Active) HasCompletedTripSwitchReturn = true;

                if (IsNse1Active && IsNse2Active && !IsCisActuated)
                {
                    IsCisActuated = true;
                    IsCvcsiActuated = true;
                    mValveStates["CVC-MOV-0318"] = false;
                    mValveStates["CVC-AOV-0339"] = false;
                    mValveStates["CVC-ISV-0323"] = false;
                    mValveStates["CVC-ISV-0325"] = false;
                    mValveStates["CVC-ISV-0329"] = false;
                    mValveStates["CVC-ISV-0331"] = false;
                }

                syncUnit06Summary();
                notifyChanged();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlantSimulationModel] toggleTripSwitch({switchId}) 오류: {ex}");
                return false;
            }
        }

        public void setLetdownAutomation(bool unlocked, bool enabled)
        {
            IsLetdownLockUnlocked = unlocked;
            if (unlocked) IsAutoLetdownEnabled = enabled;
            notifyChanged();
        }

        public void setRecircPumpState(bool unlocked, bool pumpA, bool pumpB)
        {
            IsRecircLockUnlocked = unlocked;
            if (unlocked) { IsCvcRecircPumpARunning = pumpA; IsCvcRecircPumpBRunning = pumpB; }
            notifyChanged();
        }

        public void executeCvcMakeupStep(bool selectBlend, bool validate, bool execute)
        {
            if (selectBlend) IsCvcMakeupBlendSelected = true;
            if (validate && IsCvcMakeupBlendSelected) IsCvcMakeupValidated = true;
            if (execute && IsCvcMakeupValidated)
            {
                IsCvcMakeupExecuted = true;
                updateProcessProgress("AUTO_CVC_MAKEUP", "CVC Makeup Automation (Blend)", 100f, true);
            }
            notifyChanged();
        }

        public void acknowledgeAllAlarms()
        {
            foreach (var a in Alarms) if (a.state == "Active") a.state = "Acknowledged";
            notifyChanged();
        }

        public void requestFieldOperatorAction(string actionId)
        {
            if (actionId == "HOTWELL_SAMPLE") HasRequestedHotwellSample = true;
            else if (actionId == "RCCW_ISOLATION") HasRequestedRccwIsolation = true;
            notifyChanged();
        }

        private void updateProcessProgress(string id, string title, float progressPct, bool completed)
        {
            var found = Processes.Find(p => p.id == id);
            if (found == null)
            {
                found = new ActiveProcessEntry { id = id };
                Processes.Add(found);
            }
            found.title = title;
            found.progressPct = progressPct;
            found.isCompleted = completed;
            found.statusText = title;
        }

        private void syncUnit06Summary()
        {
            var u6 = AllUnits[5];
            u6.thermalPowerPct = CoreThermalPowerPct;
            u6.tAverageF = RcsTAveF;
            u6.pzrPressurePsia = PzrPressurePsia;
            u6.pzrLevelPct = PzrLevelPct;
            u6.rcsFlowGpm = RcsFlowGpm;
            u6.cntPressurePsia = CnvPressurePsia;
            u6.ceRadCps = CeRt1011Cps;
            u6.carOffGasCpm = IsLocaActive ? 42f : 30f;
            u6.isTripped = IsRxTripActuated;
            u6.sfmStatus[0] = !IsRxTripActuated;
            u6.sfmStatus[1] = !IsLocaActive || getValveOpen("CE-AOV-0130");

            int alm = 0, cau = 0, ntc = 0;
            foreach (var a in Alarms)
            {
                if (a.severity == "Alarm") alm++;
                else if (a.severity == "Caution") cau++;
                else ntc++;
            }
            u6.alarmCount = alm;
            u6.cautionCount = cau;
            // 정상 운전(Alarms 없음) 시 12-Unit Overview 행의 알림 아이콘은 0(비활성) 유지
            u6.noticeCount = ntc;
        }

        public string getFormattedClock()
        {
            int total = Mathf.FloorToInt(SimClockSeconds) % 86400;
            int h = total / 3600;
            int m = (total % 3600) / 60;
            int s = total % 60;
            string ampm = h >= 12 ? "PM" : "AM";
            int h12 = h % 12;
            if (h12 == 0) h12 = 12;
            return $"{h12}:{m:00}:{s:00} {ampm}";
        }

        public string getFormattedRunTime()
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(SimRunTimeSeconds));
            int h = total / 3600;
            int m = (total % 3600) / 60;
            int s = total % 60;
            return $"{h:00}:{m:00}:{s:00}";
        }

        private void notifyChanged()
        {
            OnStateChanged?.Invoke();
        }
    }
}
