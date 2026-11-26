using System;
using UnityEngine;

namespace ControlRoom.UI
{
    /// <summary>
    /// 시나리오 2 (비상 상황: 원자로 냉각재 누설 LOCA — Small Charging Line Break)의
    /// 0.25초 단위 영상 분석(비상절차서_LOCA_1-2.mp4, 00:14~06:08)과 제작 가이드 p.14~22를
    /// 100% 일치시킨 56단계 절차 상태 정의입니다.
    /// </summary>
    public enum Scenario2SequenceStep
    {
        S2_00_ProcLibInitial = 0,
        S2_01_StartCatMenuOpen = 1,
        S2_02_StartPrimaryMenuOpen = 2,
        S2_03_CeInitialAlarm = 3,
        S2_04_Ce0130PopupOpen = 4,
        S2_05_Ce0130Opened = 5,
        S2_06_Ce0128PopupOpen = 6,
        S2_07_CeFlowRerouted = 7,
        S2_08_ModOverviewChecked = 8,
        S2_09_CeRecheckBeforeCvc = 9,
        S2_10_CeToCvcMenuOpen = 10,
        S2_11_CvcSubMenuForRecirc = 11,
        S2_12_CvcRecircChecked = 12,
        S2_13_CvcToCeMenuOpen = 13,
        S2_14_CeCheckedForTrip = 14,
        S2_15_CeToCvcForHeatingMenu = 15,
        S2_16_CvcSubMenuForHeating = 16,
        S2_17_HeatingSprayOpened = 17,
        S2_18_Mov0318PopupOpen = 18,
        S2_19_Mov0318Closed = 19,
        S2_20_Aov0339PopupOpen = 20,
        S2_21_CvcsValvesClosed = 21,
        S2_22_CvcSubMenuForDemins = 22,
        S2_23_DeminsChecked = 23,
        S2_24_ProcLibOpened = 24,
        S2_25_AutomationsTabOpened = 25,
        S2_26_CvcAutoSearched = 26,
        S2_27_CvcMakeupSetup = 27,
        S2_28_CvcMakeupValidated = 28,
        S2_29_CvcMakeupRunning = 29,
        S2_30_AlarmDropdownOpen = 30,
        S2_31_AlarmChecked = 31,
        S2_32_ModOverviewBeforeRecirc = 32,
        S2_33_CatMenuForRecircStop = 33,
        S2_34_PrimaryMenuForRecircStop = 34,
        S2_35_CvcSubMenuForRecircStop = 35,
        S2_36_RecircForPumpStop = 36,
        S2_37_RecircLockSelected = 37,
        S2_38_RecircManualMode = 38,
        S2_39_PumpAPopupOpen = 39,
        S2_40_RecircPumpsStopped = 40,
        S2_41_ModOverviewForIso = 41,
        S2_42_IsoTabOpened = 42,
        S2_43_IsoShowAllActive = 43,
        S2_44_IsoValvesChecked = 44,
        S2_45_CatMenuForCeRecheck = 45,
        S2_46_PrimaryMenuForCeRecheck = 46,
        S2_47_CeLeakRechecked = 47,
        S2_48_CeSampleChecked = 48,
        S2_49_ModOverviewRechecked = 49,
        S2_50_UnitDropdownFor0A = 50,
        S2_51_Unit0ASelected = 51,
        S2_52_Unit0ACatMenuOpen = 52,
        S2_53_Unit0APrimaryMenuOpen = 53,
        S2_54_RccwOpened = 54,
        S2_55_Scenario2Completed = 55
    }

    /// <summary>
    /// 시나리오 2 전용 상태 머신과 3대 모니터 실시간 공유 계측치(CE/CVC/Makeup/RCCW/경보)를 관리합니다.
    /// 기존 시나리오 1의 PlantSimulationModel을 내부 공유 모델로 사용해 12-Unit Overview와 상단 경보 배지를 동기화합니다.
    /// </summary>
    public class Scenario2ControllerModel
    {
        public event Action OnScenario2StateChanged;

        /// <summary>중앙 시나리오 매니저가 비활성(스킵) 단계를 건너뛸 때 사용하는 선택적 리졸버입니다.</summary>
        public Func<Scenario2SequenceStep, Scenario2SequenceStep> StepSkipResolver { get; set; }
        private bool mIsResolvingStep;

        public PlantSimulationModel PlantModel { get; }

        public Scenario2SequenceStep CurrentStep { get; private set; } = Scenario2SequenceStep.S2_00_ProcLibInitial;
        public MonitorScreenId CurrentMainScreen { get; private set; } = MonitorScreenId.ProcessLibrary;
        public string CenterHeaderUnitCode { get; private set; } = "06";
        public string CenterHeaderCategory { get; private set; } = "Special";
        public string CenterHeaderScreenTitle { get; private set; } = "Process Library";
        public string RightHeaderUnitCode { get; private set; } = "00";

        // 영상 실측 기반 시나리오 2 세부 계측치
        public float RcsLeakRateGpm { get; private set; } = 5.88f;
        public float CeSampleVesselLevelFt { get; private set; } = 0.001f;
        public float CeRt1011Cps { get; private set; } = 54.0f;
        public float CeRt1007Cps { get; private set; } = 54.0f;
        public float CnvPressurePsia { get; private set; } = 1.02f;
        public float PzrLevelPct { get; private set; } = 58.3f;
        public float CvcInjectionFlowGpm { get; private set; } = 22.1f;
        public float CvcDischargeFlowGpm { get; private set; } = 22.5f;
        public float CvcSprayFlowGpm { get; private set; } = 0.37f;
        public float RccwExpansionTankLevelPct { get; private set; } = 21.7f;
        public float FilterDpPsi { get; private set; } = 0.003f;

        // CVC Makeup 자동화 상태 (영상 실측: Boric Acid Only 유지 + Pump B 가동)
        public bool IsCvcMakeupBlendSelected { get; private set; }
        public bool IsCvcMakeupValidated { get; private set; }
        public bool IsCvcMakeupRunning { get; private set; }
        public bool IsMakeupPumpBRunning { get; private set; }
        public float CvcMakeupElapsedSec { get; private set; }
        public float CvcMakeupProgressPct { get; private set; }
        public string CvcMakeupStatusText { get; private set; } = string.Empty;

        // Recirc & Makeup 제어 모드 / 펌프 상태
        public bool IsRecircManualMode { get; private set; }
        public bool IsRecircPumpARunning { get; private set; } = true;
        public bool IsRecircPumpBRunning { get; private set; }

        // Module Overview / Process Library / 하단 툴바 상태
        public bool IsModuleIsoTabSelected { get; private set; }
        public bool IsIsoShowAllActive { get; private set; }
        public bool IsCvcSearchApplied { get; private set; }
        public bool IsTagLabelsVisible { get; private set; }

        // 우측 Details 패널 표시 상태
        public string SelectedTag { get; private set; } = "06_CE_RT_1011";
        public string SelectedDescription { get; private set; } = "CE Module 06 Gaseous Discharge Iodine Radiation Transmitter";
        public string SelectedValueText { get; private set; } = "54.0 cps";
        public string SelectedPositionText { get; private set; } = "--";
        public string SelectedStateText { get; private set; } = "ACKNOWLEDGED";
        public bool ShowRecircControlInDetails { get; private set; }

        // 경보 창 항목
        public bool HasSecondaryAlarmsAdded { get; private set; }
        public bool IsCeAlarmAcknowledged { get; private set; } = true;

        private float mSimRunTimeSec = 67f;

        public Scenario2ControllerModel(PlantSimulationModel sharedPlantModel)
        {
            PlantModel = sharedPlantModel ?? throw new ArgumentNullException(nameof(sharedPlantModel));
        }

        /// <summary>시나리오 2 초기 상태(영상 00:14, Process Library CE 절차 선택 직후)로 초기화합니다.</summary>
        public void initializeScenario2()
        {
            try
            {
                PlantModel.startScenario2LocaEmergency();
                PlantModel.setActiveUnitCode("06");

                CurrentStep = Scenario2SequenceStep.S2_00_ProcLibInitial;
                CurrentMainScreen = MonitorScreenId.ProcessLibrary;
                CenterHeaderUnitCode = "06";
                CenterHeaderCategory = "Special";
                CenterHeaderScreenTitle = "Process Library";
                RightHeaderUnitCode = "00";

                RcsLeakRateGpm = 5.88f;
                CeSampleVesselLevelFt = 0.001f;
                CeRt1011Cps = 54.0f;
                CeRt1007Cps = 54.0f;
                CnvPressurePsia = 1.02f;
                PzrLevelPct = 58.3f;
                CvcInjectionFlowGpm = 22.1f;
                CvcDischargeFlowGpm = 22.5f;
                CvcSprayFlowGpm = 0.37f;
                RccwExpansionTankLevelPct = 21.7f;
                FilterDpPsi = 0.003f;

                IsCvcMakeupBlendSelected = false;
                IsCvcMakeupValidated = false;
                IsCvcMakeupRunning = false;
                IsMakeupPumpBRunning = false;
                CvcMakeupElapsedSec = 0f;
                CvcMakeupProgressPct = 0f;
                CvcMakeupStatusText = string.Empty;

                IsRecircManualMode = false;
                IsRecircPumpARunning = true;
                IsRecircPumpBRunning = false;
                IsModuleIsoTabSelected = false;
                IsIsoShowAllActive = false;
                IsCvcSearchApplied = false;
                IsTagLabelsVisible = false;
                ShowRecircControlInDetails = false;
                HasSecondaryAlarmsAdded = false;
                IsCeAlarmAcknowledged = true;
                mSimRunTimeSec = 67f;

                PlantModel.operateValve("CE-AOV-0130", false);
                PlantModel.operateValve("CE-AOV-0128", true);
                PlantModel.operateValve("CVC-MOV-0318", true);
                PlantModel.operateValve("CVC-AOV-0339", true);
                PlantModel.operateValve("CVC-ISV-0329", true);
                PlantModel.operateValve("CVC-ISV-0331", true);

                setSelectedDetails("06_CE_RT_1011", "CE Module 06 Gaseous Discharge Iodine Radiation Transmitter", "54.0 cps", "--", "ACKNOWLEDGED");

                syncSharedUnit06Summary();
                notifyChanged();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Scenario2ControllerModel] initializeScenario2 오류: {ex}");
            }
        }

        /// <summary>하단 툴바 0번 아이콘 클릭 시 P&amp;ID 밸브·계측기 태그 라벨 오버레이 표시를 토글합니다.</summary>
        public void toggleTagLabels()
        {
            try
            {
                IsTagLabelsVisible = !IsTagLabelsVisible;
                notifyChanged();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Scenario2ControllerModel] toggleTagLabels 오류: {ex}");
            }
        }

        /// <summary>시나리오 2 단계를 전진시키고 화면·밸브·경보·계측치 상태를 0.25초 영상 절차에 맞춰 갱신합니다. StepSkipResolver가 설정되면 비활성 단계를 자동 건너뜁니다.</summary>
        public void advanceStep(Scenario2SequenceStep nextStep)
        {
            try
            {
                if (!mIsResolvingStep && StepSkipResolver != null)
                {
                    mIsResolvingStep = true;
                    try
                    {
                        Scenario2SequenceStep resolved = StepSkipResolver(nextStep);
                        if ((int)resolved > (int)nextStep && (int)resolved - (int)nextStep <= 20)
                        {
                            for (int s = (int)nextStep; s <= (int)resolved; s++)
                            {
                                applySingleStep((Scenario2SequenceStep)s);
                            }
                        }
                        else
                        {
                            applySingleStep(resolved);
                        }
                        syncSharedUnit06Summary();
                        notifyChanged();
                        return;
                    }
                    finally
                    {
                        mIsResolvingStep = false;
                    }
                }

                applySingleStep(nextStep);
                syncSharedUnit06Summary();
                notifyChanged();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Scenario2ControllerModel] advanceStep({nextStep}) 오류: {ex}");
            }
        }

        private void applySingleStep(Scenario2SequenceStep nextStep)
        {
            CurrentStep = nextStep;
            ShowRecircControlInDetails = false;

            switch (nextStep)
                {
                    case Scenario2SequenceStep.S2_01_StartCatMenuOpen:
                    case Scenario2SequenceStep.S2_02_StartPrimaryMenuOpen:
                        break;

                    case Scenario2SequenceStep.S2_03_CeInitialAlarm:
                        CurrentMainScreen = MonitorScreenId.PrimaryCE;
                        CenterHeaderUnitCode = "06";
                        CenterHeaderCategory = "Primary";
                        CenterHeaderScreenTitle = "(CE) Containment Evacuation";
                        setSelectedDetails("06_CE_RT_1011", "CE Module 06 Gaseous Discharge Iodine Radiation Transmitter", $"{CeRt1011Cps:F1} cps", "--", "HI ALARM (ACK)");
                        break;

                    case Scenario2SequenceStep.S2_04_Ce0130PopupOpen:
                        setSelectedDetails("06_CE_AOV_0130", "CE Module 06 Gaseous Discharge Flow to GRW Isolation Valve", "0.0 %", "0.0 %", "Closed");
                        break;

                    case Scenario2SequenceStep.S2_05_Ce0130Opened:
                        PlantModel.operateValve("CE-AOV-0130", true);
                        setSelectedDetails("06_CE_AOV_0130", "CE Module 06 Gaseous Discharge Flow to GRW Isolation Valve", "100.0 %", "100.0 %", "Opened");
                        break;

                    case Scenario2SequenceStep.S2_06_Ce0128PopupOpen:
                        setSelectedDetails("06_CE_AOV_0128", "CE Module 06 Gaseous Discharge Flow to RBV Isolation Valve", "100.0 %", "100.0 %", "Opened");
                        break;

                    case Scenario2SequenceStep.S2_07_CeFlowRerouted:
                        PlantModel.operateValve("CE-AOV-0128", false);
                        FilterDpPsi = 0f;
                        setSelectedDetails("06_CE_AOV_0128", "CE Module 06 Gaseous Discharge Flow to RBV Isolation Valve", "0.0 %", "0.0 %", "Closed");
                        break;

                    case Scenario2SequenceStep.S2_08_ModOverviewChecked:
                        CurrentMainScreen = MonitorScreenId.ModuleOverview;
                        CenterHeaderCategory = "Special";
                        CenterHeaderScreenTitle = "Module Overview";
                        IsModuleIsoTabSelected = false;
                        setSelectedDetails("06_RCS_LE_0015A", "PZR Level Normalized Transmitter", $"{PzrLevelPct:F1} %", "--", "Decreasing (< 58.5%)");
                        break;

                    case Scenario2SequenceStep.S2_09_CeRecheckBeforeCvc:
                        CurrentMainScreen = MonitorScreenId.PrimaryCE;
                        CenterHeaderCategory = "Primary";
                        CenterHeaderScreenTitle = "(CE) Containment Evacuation";
                        setSelectedDetails("06_CE_TT_1002", "CE Module 06 Condenser Inlet Temperature Transmitter", "137 °F", "--", "Monitoring");
                        break;

                    case Scenario2SequenceStep.S2_10_CeToCvcMenuOpen:
                    case Scenario2SequenceStep.S2_11_CvcSubMenuForRecirc:
                        break;

                    case Scenario2SequenceStep.S2_12_CvcRecircChecked:
                        CurrentMainScreen = MonitorScreenId.PrimaryCvcRecirMakeup;
                        CenterHeaderCategory = "Primary";
                        CenterHeaderScreenTitle = "(CVC) Chemical & Volume Control > Recirc & Makeup";
                        setSelectedDetails("06_CVC_RT_3016", "CVC Module 06 Regenerative Heat Exchanger Cooling Inlet Radiation", "0 mrem/hr", "--", "Normal");
                        break;

                    case Scenario2SequenceStep.S2_13_CvcToCeMenuOpen:
                        break;

                    case Scenario2SequenceStep.S2_14_CeCheckedForTrip:
                        CurrentMainScreen = MonitorScreenId.PrimaryCE;
                        CenterHeaderCategory = "Primary";
                        CenterHeaderScreenTitle = "(CE) Containment Evacuation";
                        CnvPressurePsia = Mathf.Max(CnvPressurePsia, 1.10f);
                        RcsLeakRateGpm = Mathf.Max(RcsLeakRateGpm, 7.12f);
                        CeSampleVesselLevelFt = Mathf.Max(CeSampleVesselLevelFt, 0.017f);
                        setSelectedDetails("06_CE_PT_1003", "CE Module 06 Condenser Pressure Transmitter (Trip 기준 < 7 psia, 누설 < 20 gpm)", $"{CnvPressurePsia:F2} psia", "--", "No Trip Required");
                        break;

                    case Scenario2SequenceStep.S2_15_CeToCvcForHeatingMenu:
                    case Scenario2SequenceStep.S2_16_CvcSubMenuForHeating:
                        break;

                    case Scenario2SequenceStep.S2_17_HeatingSprayOpened:
                        CurrentMainScreen = MonitorScreenId.PrimaryCvcHeatingSpray;
                        CenterHeaderCategory = "Primary";
                        CenterHeaderScreenTitle = "(CVC) Chemical & Volume Control > Heating & Spray";
                        setSelectedDetails("06_CVC_MOV_0318", "CVC Module 06 Module Injection Isolation Valve", "100.0 %", "100.0 %", "Opened");
                        break;

                    case Scenario2SequenceStep.S2_18_Mov0318PopupOpen:
                        setSelectedDetails("06_CVC_MOV_0318", "CVC Module 06 Module Injection Isolation Valve", "100.0 %", "100.0 %", "Opened");
                        break;

                    case Scenario2SequenceStep.S2_19_Mov0318Closed:
                        PlantModel.operateValve("CVC-MOV-0318", false);
                        CvcInjectionFlowGpm = 8.38f;
                        CvcSprayFlowGpm = -7.66f;
                        CvcDischargeFlowGpm = 1.33f;
                        addSecondaryAlarmsIfNeeded();
                        setSelectedDetails("06_CVC_MOV_0318", "CVC Module 06 Module Injection Isolation Valve", "0.0 %", "0.0 %", "Closed");
                        break;

                    case Scenario2SequenceStep.S2_20_Aov0339PopupOpen:
                        setSelectedDetails("06_CVC_AOV_0339", "CVC Module 06 Module Discharge Isolation Valve", "100.0 %", "100.0 %", "Opened");
                        break;

                    case Scenario2SequenceStep.S2_21_CvcsValvesClosed:
                        PlantModel.operateValve("CVC-AOV-0339", false);
                        CvcSprayFlowGpm = -0.68f;
                        CvcInjectionFlowGpm = 0.64f;
                        CvcDischargeFlowGpm = 0.61f;
                        setSelectedDetails("06_CVC_AOV_0339", "CVC Module 06 Module Discharge Isolation Valve", "0.0 %", "0.0 %", "Closed");
                        break;

                    case Scenario2SequenceStep.S2_22_CvcSubMenuForDemins:
                        break;

                    case Scenario2SequenceStep.S2_23_DeminsChecked:
                        CurrentMainScreen = MonitorScreenId.PrimaryCvcDeminsLetdowns;
                        CenterHeaderCategory = "Primary";
                        CenterHeaderScreenTitle = "(CVC) Chemical & Volume Control > Demins & Letdowns";
                        setSelectedDetails("06_CVC_FIT_2012", "LRW Letdown Flow Transmitter", "0 gpm", "--", "Isolated (0 gpm)");
                        break;

                    case Scenario2SequenceStep.S2_24_ProcLibOpened:
                        CurrentMainScreen = MonitorScreenId.ProcessLibrary;
                        CenterHeaderCategory = "Special";
                        CenterHeaderScreenTitle = "Process Library";
                        break;

                    case Scenario2SequenceStep.S2_25_AutomationsTabOpened:
                        IsCvcSearchApplied = false;
                        break;

                    case Scenario2SequenceStep.S2_26_CvcAutoSearched:
                        IsCvcSearchApplied = true;
                        break;

                    case Scenario2SequenceStep.S2_27_CvcMakeupSetup:
                        PzrLevelPct = Mathf.Min(PzrLevelPct, 57.6f);
                        IsCvcMakeupBlendSelected = false;
                        IsCvcMakeupValidated = false;
                        CvcMakeupStatusText = string.Empty;
                        setSelectedDetails("06_CVC_MAKEUP", "Unit06 : CVC Makeup Automation (Boric Acid Only)", $"{PzrLevelPct:F1} %", "59.92 %", "Setup");
                        break;

                    case Scenario2SequenceStep.S2_28_CvcMakeupValidated:
                        IsCvcMakeupBlendSelected = false;
                        IsCvcMakeupValidated = true;
                        CvcMakeupStatusText = "Ready to start";
                        setSelectedDetails("06_CVC_MAKEUP", "Unit06 : CVC Makeup Automation (Boric Acid Only)", $"{PzrLevelPct:F1} %", "59.92 %", "Ready to start");
                        break;

                    case Scenario2SequenceStep.S2_29_CvcMakeupRunning:
                        IsCvcMakeupRunning = true;
                        IsMakeupPumpBRunning = true;
                        CvcMakeupElapsedSec = 0f;
                        CvcMakeupProgressPct = 16.7f;
                        CvcMakeupStatusText = "Aligning Boric Acid System";
                        setSelectedDetails("06_CVC_MAKEUP", "Unit06 : CVC Makeup Automation", $"{PzrLevelPct:F1} %", "59.92 %", "Active (16.7%)");
                        break;

                    case Scenario2SequenceStep.S2_30_AlarmDropdownOpen:
                        // 영상 실측: 경보 드롭다운을 열었을 때 기존 CE Gaseous만 ACKNOWLEDGED, 신규 2건은 ACTIVE 유지
                        addSecondaryAlarmsIfNeeded();
                        break;

                    case Scenario2SequenceStep.S2_31_AlarmChecked:
                        break;

                    case Scenario2SequenceStep.S2_32_ModOverviewBeforeRecirc:
                        CurrentMainScreen = MonitorScreenId.ModuleOverview;
                        CenterHeaderCategory = "Special";
                        CenterHeaderScreenTitle = "Module Overview";
                        IsModuleIsoTabSelected = false;
                        CnvPressurePsia = Mathf.Max(CnvPressurePsia, 1.15f);
                        PzrLevelPct = Mathf.Min(PzrLevelPct, 57.5f);
                        setSelectedDetails("06_RCS_LE_0015A", "PZR Level Normalized Transmitter (CNV 1.15 psia > 0.3 psia)", $"{PzrLevelPct:F1} %", "--", "Monitoring");
                        break;

                    case Scenario2SequenceStep.S2_33_CatMenuForRecircStop:
                    case Scenario2SequenceStep.S2_34_PrimaryMenuForRecircStop:
                    case Scenario2SequenceStep.S2_35_CvcSubMenuForRecircStop:
                        break;

                    case Scenario2SequenceStep.S2_36_RecircForPumpStop:
                        CurrentMainScreen = MonitorScreenId.PrimaryCvcRecirMakeup;
                        CenterHeaderCategory = "Primary";
                        CenterHeaderScreenTitle = "(CVC) Chemical & Volume Control > Recirc & Makeup";
                        break;

                    case Scenario2SequenceStep.S2_37_RecircLockSelected:
                        ShowRecircControlInDetails = true;
                        setSelectedDetails("06_CVC_RECIRC_CTRL", "CVC Recirculation Control", "Locked", "--", "Recirculation");
                        break;

                    case Scenario2SequenceStep.S2_38_RecircManualMode:
                        IsRecircManualMode = true;
                        ShowRecircControlInDetails = true;
                        setSelectedDetails("06_CVC_RECIRC_CTRL", "CVC Recirculation Control", "Unlocked", "--", "Manual");
                        break;

                    case Scenario2SequenceStep.S2_39_PumpAPopupOpen:
                        ShowRecircControlInDetails = false;
                        setSelectedDetails("06_CVC_P_0004A", "CVC Recirculation Pump A", "100.0 %", "--", "Running");
                        break;

                    case Scenario2SequenceStep.S2_40_RecircPumpsStopped:
                        IsRecircPumpARunning = false;
                        IsRecircPumpBRunning = false;
                        PlantModel.setRecircPumpState(true, false, false);
                        clearRecircLowFlowAlarm();
                        setSelectedDetails("06_CVC_P_0004A", "CVC Recirculation Pump A", "0.0 %", "--", "Stopped");
                        break;

                    case Scenario2SequenceStep.S2_41_ModOverviewForIso:
                        CurrentMainScreen = MonitorScreenId.ModuleOverview;
                        CenterHeaderCategory = "Special";
                        CenterHeaderScreenTitle = "Module Overview";
                        IsModuleIsoTabSelected = false;
                        IsIsoShowAllActive = false;
                        break;

                    case Scenario2SequenceStep.S2_42_IsoTabOpened:
                        IsModuleIsoTabSelected = true;
                        IsIsoShowAllActive = false;
                        break;

                    case Scenario2SequenceStep.S2_43_IsoShowAllActive:
                        IsModuleIsoTabSelected = true;
                        IsIsoShowAllActive = true;
                        break;

                    case Scenario2SequenceStep.S2_44_IsoValvesChecked:
                        IsModuleIsoTabSelected = true;
                        IsIsoShowAllActive = false;
                        setSelectedDetails("06_CVC_ISV_0323", "CNT Module 06 PZR Spray Outboard Containment Isolation Valve (ISV-0329/0331 정상 개방 확인)", "100.0 %", "100.0 %", "Opened");
                        break;

                    case Scenario2SequenceStep.S2_45_CatMenuForCeRecheck:
                    case Scenario2SequenceStep.S2_46_PrimaryMenuForCeRecheck:
                        break;

                    case Scenario2SequenceStep.S2_47_CeLeakRechecked:
                        CurrentMainScreen = MonitorScreenId.PrimaryCE;
                        CenterHeaderCategory = "Primary";
                        CenterHeaderScreenTitle = "(CE) Containment Evacuation";
                        CeSampleVesselLevelFt = Mathf.Max(CeSampleVesselLevelFt, 0.11f);
                        CnvPressurePsia = Mathf.Max(CnvPressurePsia, 1.17f);
                        RcsLeakRateGpm = 6.70f;
                        setSelectedDetails("06_RCS_LEAK_CALC", "RCS Calculated Leak Rate", $"{RcsLeakRateGpm:F2} gpm", "--", "Monitoring");
                        break;

                    case Scenario2SequenceStep.S2_48_CeSampleChecked:
                        setSelectedDetails("06_CE_LIT_1009", "CE Module 06 Sample Vessel Level Transmitter", $"{CeSampleVesselLevelFt:F2} ft", "--", "Rising (> 0 ft)");
                        break;

                    case Scenario2SequenceStep.S2_49_ModOverviewRechecked:
                        CurrentMainScreen = MonitorScreenId.ModuleOverview;
                        CenterHeaderCategory = "Special";
                        CenterHeaderScreenTitle = "Module Overview";
                        IsModuleIsoTabSelected = false;
                        CnvPressurePsia = Mathf.Max(CnvPressurePsia, 1.18f);
                        PzrLevelPct = Mathf.Min(PzrLevelPct, 57.0f);
                        setSelectedDetails("06_RCS_LE_0015A", "PZR Level Normalized Transmitter", $"{PzrLevelPct:F1} %", "--", "Stabilizing");
                        break;

                    case Scenario2SequenceStep.S2_50_UnitDropdownFor0A:
                        break;

                    case Scenario2SequenceStep.S2_51_Unit0ASelected:
                        CenterHeaderUnitCode = "0A";
                        CurrentMainScreen = MonitorScreenId.ModuleOverview;
                        CenterHeaderCategory = "Special";
                        CenterHeaderScreenTitle = "Module Overview";
                        break;

                    case Scenario2SequenceStep.S2_52_Unit0ACatMenuOpen:
                    case Scenario2SequenceStep.S2_53_Unit0APrimaryMenuOpen:
                        break;

                    case Scenario2SequenceStep.S2_54_RccwOpened:
                        CenterHeaderUnitCode = "0A";
                        CurrentMainScreen = MonitorScreenId.PrimaryRccw;
                        CenterHeaderCategory = "Primary";
                        CenterHeaderScreenTitle = "(RCCW) Reactor Component Cooling (Overview)";
                        setSelectedDetails("0A_RCCW_LIT_1039NRM", "0A RCCW Expansion Tank Level Transmitter", $"{RccwExpansionTankLevelPct:F1} %", "--", "Normal (<= 25%)");
                        break;

                    case Scenario2SequenceStep.S2_55_Scenario2Completed:
                        setSelectedDetails("0A_RCCW_LIT_1039NRM", "0A RCCW Expansion Tank Level Transmitter (21.7% <= 25%: 누설 없음 확인 완료)", "21.7 %", "--", "Procedure Complete");
                        break;
                }
        }

        /// <summary>매 프레임 경과 시간, LOCA 물리량 추이, CVC Makeup 진행률을 갱신합니다.</summary>
        public void tick(float deltaTime)
        {
            try
            {
                if (deltaTime <= 0f) return;

                mSimRunTimeSec += deltaTime;

                if (CurrentStep < Scenario2SequenceStep.S2_21_CvcsValvesClosed)
                {
                    RcsLeakRateGpm = Mathf.Min(7.12f, RcsLeakRateGpm + 0.008f * deltaTime);
                    CeSampleVesselLevelFt = Mathf.Min(0.025f, CeSampleVesselLevelFt + 0.00018f * deltaTime);
                    CnvPressurePsia = Mathf.Min(1.11f, CnvPressurePsia + 0.0012f * deltaTime);
                    PzrLevelPct = Mathf.Max(57.5f, PzrLevelPct - 0.012f * deltaTime);
                }
                else if (CurrentStep < Scenario2SequenceStep.S2_55_Scenario2Completed)
                {
                    CnvPressurePsia = Mathf.Min(1.18f, CnvPressurePsia + 0.0006f * deltaTime);
                    CeSampleVesselLevelFt = Mathf.Min(0.11f, CeSampleVesselLevelFt + 0.00045f * deltaTime);
                    if (IsCvcMakeupRunning)
                    {
                        PzrLevelPct = Mathf.Min(57.9f, PzrLevelPct + 0.003f * deltaTime);
                    }
                }

                if (IsCvcMakeupRunning && CvcMakeupProgressPct < 50.0f)
                {
                    CvcMakeupElapsedSec += deltaTime;
                    if (CvcMakeupElapsedSec < 2.0f)
                    {
                        CvcMakeupProgressPct = 16.7f;
                        CvcMakeupStatusText = "Aligning Boric Acid System";
                    }
                    else if (CvcMakeupElapsedSec < 5.0f)
                    {
                        CvcMakeupProgressPct = 33.3f;
                        CvcMakeupStatusText = "Starting Makeup Pump";
                    }
                    else
                    {
                        CvcMakeupProgressPct = 50.0f;
                        CvcMakeupStatusText = "Making Up to 59.92% Pressurizer Level";
                    }
                }

                syncSharedUnit06Summary();
                notifyChanged();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Scenario2ControllerModel] tick 오류: {ex}");
            }
        }

        public string getFormattedRunTime()
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(mSimRunTimeSec));
            int h = total / 3600;
            int m = (total % 3600) / 60;
            int s = total % 60;
            return $"{h:00}:{m:00}:{s:00}";
        }

        private void addSecondaryAlarmsIfNeeded()
        {
            if (HasSecondaryAlarmsAdded) return;
            HasSecondaryAlarmsAdded = true;

            PlantModel.Alarms.Add(new AlarmNotificationEntry
            {
                id = "ALM_CVC_RECIRC_LOW",
                severity = "Alarm",
                unitText = "06",
                state = "Active",
                shortTitle = "06 - CVC RECIRCULATION PUMP A FLOW LOW LOW",
                fullDescription = "CVC Recirculation Pump A flow dropped below Low-Low setpoint following injection isolation.",
                plantSystem = "CVC",
                deviceTag = "06_CVC_P_0004A",
                setpoint = "LOW LOW",
                timeText = PlantModel.getFormattedClock(),
                targetScreen = MonitorScreenId.PrimaryCvcRecirMakeup
            });

            PlantModel.Alarms.Add(new AlarmNotificationEntry
            {
                id = "CAU_CE_VAC_SUCTION",
                severity = "Caution",
                unitText = "06",
                state = "Active",
                shortTitle = "06 - CE VACUUM PUMP SUCTION PRESSURE HI HI",
                fullDescription = "CE Vacuum Pump suction pressure elevated during containment evacuation alignment.",
                plantSystem = "CE",
                deviceTag = "06_CE_PT_1001",
                setpoint = "HI HI",
                timeText = PlantModel.getFormattedClock(),
                targetScreen = MonitorScreenId.PrimaryCE
            });
        }

        private void clearRecircLowFlowAlarm()
        {
            PlantModel.Alarms.RemoveAll(a => string.Equals(a.id, "ALM_CVC_RECIRC_LOW", StringComparison.OrdinalIgnoreCase));
        }

        private void setSelectedDetails(string tag, string desc, string valText, string posText, string stateText)
        {
            SelectedTag = tag;
            SelectedDescription = desc;
            SelectedValueText = valText;
            SelectedPositionText = posText;
            SelectedStateText = stateText;
        }

        private void syncSharedUnit06Summary()
        {
            var u6 = PlantModel.AllUnits[5];
            u6.thermalPowerPct = 99.7f;
            u6.tAverageF = 543.8f;
            u6.pzrPressurePsia = 1849f;
            u6.pzrLevelPct = PzrLevelPct;
            u6.rcsFlowGpm = 9425f;
            u6.cntPressurePsia = CnvPressurePsia;
            u6.ceRadCps = CeRt1011Cps;
            u6.carOffGasCpm = 42f;
            u6.alarmCount = PlantModel.ActiveAlarmCount;
            u6.cautionCount = PlantModel.ActiveCautionCount;
            u6.noticeCount = PlantModel.NoticeBadgeCount;
            u6.isTripped = false;
            u6.sfmStatus[0] = true;
            u6.sfmStatus[1] = PlantModel.getValveOpen("CE-AOV-0130");
        }

        private void notifyChanged()
        {
            OnScenario2StateChanged?.Invoke();
        }
    }
}
