using System;
using UnityEngine;

namespace ControlRoom.UI
{
    /// <summary>
    /// VR 시나리오 제작 가이드(1차 납품) 기준의 단계별 진행 상태와 안내 문구를 계산하고,
    /// 시나리오 선택·배속·초기화를 제어하는 가이드 상태 머신입니다.
    /// </summary>
    public class ScenarioGuideController
    {
        public struct GuideStatusInfo
        {
            public string scenarioBadge;
            public string stepTitle;
            public string instructionText;
            public string targetHint;
            public int completedSteps;
            public int totalSteps;
            public bool isGoalCompleted;
        }

        private readonly PlantSimulationModel mModel;
        private MonitorScreenId mCurrentMainScreen = MonitorScreenId.ProcessLibrary;
        private bool mHasOpenedPowerReduction;
        private bool mHasOpenedCeScreen;
        private bool mHasOpenedTripMatrix;

        public ScenarioGuideController(PlantSimulationModel model)
        {
            mModel = model ?? throw new ArgumentNullException(nameof(model));
        }

        /// <summary>메인 모니터 화면 전환 내역을 기록해 단계 달성 여부를 판정합니다.</summary>
        public void notifyScreenVisited(MonitorScreenId screenId)
        {
            mCurrentMainScreen = screenId;
            if (screenId == MonitorScreenId.PrimaryCE) mHasOpenedCeScreen = true;
            if (screenId == MonitorScreenId.TripMatrix) mHasOpenedTripMatrix = true;
        }

        /// <summary>Power Reduction 절차서 열람 여부 기록</summary>
        public void notifyPowerReductionOpened()
        {
            mHasOpenedPowerReduction = true;
        }

        /// <summary>시나리오 초기화 시 가이드 내부 추적 상태도 함께 초기화</summary>
        public void resetTracking()
        {
            mHasOpenedPowerReduction = false;
            mHasOpenedCeScreen = false;
            mHasOpenedTripMatrix = false;
            mCurrentMainScreen = MonitorScreenId.ProcessLibrary;
        }

        /// <summary>현재 시나리오 및 발전소 상태를 평가해 안내 문구와 진행률을 반환합니다.</summary>
        public GuideStatusInfo evaluateGuideStatus()
        {
            try
            {
                switch (mModel.CurrentScenario)
                {
                    case TrainingScenarioType.Standby:
                        return new GuideStatusInfo
                        {
                            scenarioBadge = "스텐바이 (공통 첫화면)",
                            stepTitle = "대기 상태 — 시나리오를 선택하거나 계통 화면을 탐색하세요",
                            instructionText = "좌(Unit Overview) · 중(Process Library 메인) · 우(12-Unit Overview) 화면이 정상 운전(99.8%) 상태로 동기화되어 있습니다.",
                            targetHint = "상단 [시나리오1: 출력조정] 또는 [시나리오2: 비상LOCA] 버튼으로 교육을 시작하세요.",
                            completedSteps = 0,
                            totalSteps = 1,
                            isGoalCompleted = false
                        };

                    case TrainingScenarioType.Scenario1PowerAdjustment:
                        return evaluateScenario1();

                    case TrainingScenarioType.Scenario2LocaEmergency:
                        return evaluateScenario2();

                    default:
                        return default;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ScenarioGuideController] evaluateGuideStatus 오류: {ex}");
                return new GuideStatusInfo { scenarioBadge = "오류", stepTitle = "가이드 평가 오류", instructionText = ex.Message };
            }
        }

        private GuideStatusInfo evaluateScenario1()
        {
            // 1차 납품 목표: 단계 3~4 (80% / 300%/Hr 입력 → Validate → Execute), 이후 단계 5~7 연속 확인 가능
            if (mModel.HasReached100PercentAfterReduction)
            {
                return new GuideStatusInfo
                {
                    scenarioBadge = "시나리오 1: 출력 조정",
                    stepTitle = "단계 7 완료: 출력 100% 복귀 및 운전 안정화 확인 완료",
                    instructionText = "출력 80% 감발 후 100%(20%/hr) 복귀 및 급수펌프 2대 정상 운전 확인까지 모두 완료되었습니다.",
                    targetHint = "초기화하거나 시나리오 2로 전환할 수 있습니다.",
                    completedSteps = 5,
                    totalSteps = 5,
                    isGoalCompleted = true
                };
            }

            if (mModel.IsPowerManeuverRunning && Mathf.Abs(mModel.RequestedPowerPct - 100f) < 1f)
            {
                return new GuideStatusInfo
                {
                    scenarioBadge = "시나리오 1: 출력 조정",
                    stepTitle = $"단계 7 진행 중: 목표 출력 100%로 상승 중 (현재 {mModel.CoreThermalPowerPct:0.0}%)",
                    instructionText = "좌측 모니터(FW/MS, 출력)와 중앙 모니터(제어봉 인출, 그래프), 우측 모니터(06호기 열출력)가 100% 정상 상태로 복귀 중입니다.",
                    targetHint = "출력 100% 도달 시 시나리오 1이 완료됩니다.",
                    completedSteps = 4,
                    totalSteps = 5,
                    isGoalCompleted = false
                };
            }

            if (mModel.HasReached80Percent)
            {
                return new GuideStatusInfo
                {
                    scenarioBadge = "시나리오 1: 출력 조정",
                    stepTitle = "단계 5 완료 (1차 목표 달성) · 단계 6: 출력 100% 상승 실행",
                    instructionText = "출력이 80%에 도달했습니다(급수유량 120 lb/s, GOV 50%). 연속 진행: Procedures > B. Power Ascension 링크 → Target 100, Rate 20 입력 후 Validate → Execute.",
                    targetHint = "1차 목표(80% 출력 감소) 완료! 계속하려면 Procedures에서 B. Power Ascension을 선택하세요.",
                    completedSteps = 4,
                    totalSteps = 5,
                    isGoalCompleted = true
                };
            }

            if (mModel.IsPowerManeuverRunning && Mathf.Abs(mModel.RequestedPowerPct - 80f) < 1f)
            {
                return new GuideStatusInfo
                {
                    scenarioBadge = "시나리오 1: 출력 조정",
                    stepTitle = $"단계 5 진행 중: 목표 출력 80%로 감소 중 (현재 {mModel.CoreThermalPowerPct:0.0}%)",
                    instructionText = "좌측 모니터(FW/MS, 출력)와 우측 모니터(06호기 열출력/유량)가 동시에 감소합니다. 상단 [15x] 또는 [즉시 완료]로 대기 시간을 단축할 수 있습니다.",
                    targetHint = "출력 80% 도달 시 1차 목표가 완료됩니다.",
                    completedSteps = 3,
                    totalSteps = 5,
                    isGoalCompleted = false
                };
            }

            if (mModel.IsPowerManeuverValidated && Mathf.Abs(mModel.PendingTargetPowerPct - 80f) < 1f && Mathf.Abs(mModel.PendingRampRatePctHr - 300f) < 1f)
            {
                return new GuideStatusInfo
                {
                    scenarioBadge = "시나리오 1: 출력 조정",
                    stepTitle = "단계 4-2: Execution Summary 확인 후 [Execute] 클릭",
                    instructionText = "Target 80%, Ramp Rate 300%/Hr 검증 완료(Ready to start). 우측 하단 [Execute] 버튼을 눌러 출력 감소를 시작하세요.",
                    targetHint = "우측 하단 [Execute] 클릭",
                    completedSteps = 2,
                    totalSteps = 5,
                    isGoalCompleted = false
                };
            }

            bool inPowerManeuverScreen = mCurrentMainScreen == MonitorScreenId.ProcessLibrary &&
                                         (mHasOpenedPowerReduction || mModel.SelectedProcedureId == "PowerManeuvering");
            if (inPowerManeuverScreen && mModel.SelectedProcedureId == "PowerManeuvering")
            {
                return new GuideStatusInfo
                {
                    scenarioBadge = "시나리오 1: 출력 조정",
                    stepTitle = "단계 4-1: Target Power 80, Ramp Rate 300 입력 후 [Validate]",
                    instructionText = "Use Dilution은 체크하지 않은 상태에서 Target Power(%)=80, Ramp Rate(%/Hr)=300을 입력하고 [Validate] 버튼을 누르세요.",
                    targetHint = "Target 80 / Rate 300 입력 → [Validate] 클릭",
                    completedSteps = 1,
                    totalSteps = 5,
                    isGoalCompleted = false
                };
            }

            return new GuideStatusInfo
            {
                scenarioBadge = "시나리오 1: 출력 조정",
                stepTitle = "단계 3: C. Power Reduction 절차서 확인 및 자동화 이동",
                instructionText = "좌측 트리에서 [IOP > D. Power Operation > C. Power Reduction]을 열고 [Start Procedure] 후 6번 항목 [Link to: Power Maneuvering Automation]을 클릭하세요.",
                targetHint = "1~5번 확인(Use Dilution 미선택) → 6번 자동화 링크 클릭",
                completedSteps = 0,
                totalSteps = 5,
                isGoalCompleted = false
            };
        }

        private GuideStatusInfo evaluateScenario2()
        {
            // 1차 납품 목표: 단계 1.0~4.0 (수동 트립 후 스위치 원위치까지 완료)
            if (mModel.HasCompletedTripSwitchReturn)
            {
                return new GuideStatusInfo
                {
                    scenarioBadge = "시나리오 2: 원자로 냉각재 누설",
                    stepTitle = "단계 4.0 완료 (1차 목표 달성) · 후속 조치(5.0~12.0) 진행 가능",
                    instructionText = "원자로 수동 트립(RXTRIP ACTUATED) 및 RTS 스위치 복귀까지 완료되었습니다. 상단 경로로 CVC(Heating&Spray/Demins/Recir), Makeup, RCCW 점검을 이어갈 수 있습니다.",
                    targetHint = "1차 납품 목표 완료! (추가 점검: Primary > CVC > Heating & Spray)",
                    completedSteps = 5,
                    totalSteps = 5,
                    isGoalCompleted = true
                };
            }

            if (mModel.IsRxTripActuated)
            {
                return new GuideStatusInfo
                {
                    scenarioBadge = "시나리오 2: 원자로 냉각재 누설",
                    stepTitle = "단계 4.0-2: 수동 트립 발동 확인 → RTS I, RTS II 스위치 원위치 복귀",
                    instructionText = "RXTRIP ACTUATED 상태가 확인되었습니다. 절차에 따라 RTS I, RTS II 스위치를 다시 눌러 원위치(해제)하고 자물쇠를 잠그세요.",
                    targetHint = "하단 Switches 바의 [RTS I], [RTS II] 해제 → 자물쇠 잠금",
                    completedSteps = 4,
                    totalSteps = 5,
                    isGoalCompleted = false
                };
            }

            bool ceRoutedToGrw = mModel.getValveOpen("CE-AOV-0130") && !mModel.getValveOpen("CE-AOV-0128");
            if (ceRoutedToGrw)
            {
                if (mCurrentMainScreen == MonitorScreenId.TripMatrix || mHasOpenedTripMatrix)
                {
                    return new GuideStatusInfo
                    {
                        scenarioBadge = "시나리오 2: 원자로 냉각재 누설",
                        stepTitle = "단계 4.0-1: Trip Matrix 스위치 잠금 해제 후 [RTS I], [RTS II] 선택",
                        instructionText = "RCS 누설률 > 20 gpm으로 수동 트립 조건 충족! 하단 Switches 바의 자물쇠를 풀어 잠금 해제 후 [RTS I]과 [RTS II]를 눌러 원자로를 정지하세요.",
                        targetHint = "하단 자물쇠 클릭 → [RTS I] & [RTS II] 클릭",
                        completedSteps = 3,
                        totalSteps = 5,
                        isGoalCompleted = false
                    };
                }

                return new GuideStatusInfo
                {
                    scenarioBadge = "시나리오 2: 원자로 냉각재 누설",
                    stepTitle = "단계 3.0~4.0: 누설 증상 확인 후 Trip Matrix로 이동",
                    instructionText = "좌측/중앙 화면에서 RCS Leak Rate(>20 gpm), CE-RT-1007(>50 cps), PZR Level(<58.5%)을 확인하고 상단 메뉴 [06 : I&C > Trip Matrix]로 이동하세요.",
                    targetHint = "상단 Breadcrumb → [I&C > (MPS) Trip Matrix] 이동",
                    completedSteps = 2,
                    totalSteps = 5,
                    isGoalCompleted = false
                };
            }

            if (mHasOpenedCeScreen || mCurrentMainScreen == MonitorScreenId.PrimaryCE)
            {
                return new GuideStatusInfo
                {
                    scenarioBadge = "시나리오 2: 원자로 냉각재 누설",
                    stepTitle = "단계 1.0~2.0: 방사능 50cps 확인 및 배출 유로 전환 (CE-AOV-0130 개방 / 0128 차단)",
                    instructionText = "CE_RT_1011(54.2 cps ≥ 50) 확인 후 계통도에서 [CE-AOV-0130](GRW)을 클릭해 OPEN(개방), [CE-AOV-0128](RBV)을 클릭해 CLOSE(차단)하세요.",
                    targetHint = "CE-AOV-0130 [OPEN] → CE-AOV-0128 [CLOSE]",
                    completedSteps = 1,
                    totalSteps = 5,
                    isGoalCompleted = false
                };
            }

            return new GuideStatusInfo
            {
                scenarioBadge = "시나리오 2: 원자로 냉각재 누설",
                stepTitle = "단계 1.0: 경보 확인 및 Primary > (CE) Containment Evacuation 이동",
                instructionText = "06호기 'CE gaseous discharge radioactivity Hi' 경보가 발생했습니다. 우측 상단 경보 아이콘 또는 상단 Breadcrumb으로 [Primary > CE] 화면으로 이동하세요.",
                targetHint = "상단 [06 : Special] 드롭다운 또는 경보창에서 [Primary > CE] 이동",
                completedSteps = 0,
                totalSteps = 5,
                isGoalCompleted = false
            };
        }
    }
}
