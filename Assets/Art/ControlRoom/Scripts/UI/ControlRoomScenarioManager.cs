using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ControlRoom.UI
{
    /// <summary>시나리오 식별자 (1: 정상 출력조정 100%→80%→100% / 2: 비상 LOCA Small Charging Line Break)</summary>
    public enum ControlRoomScenarioId
    {
        Scenario1 = 1,
        Scenario2 = 2
    }

    /// <summary>인스펙터에서 시나리오 1 단계 활성 여부(스킵)와 순서를 직관적으로 편집하기 위한 항목입니다.</summary>
    [Serializable]
    public class Scenario1StepEntry
    {
        public CaptureSequenceStep step;
        public bool enabled = true;
        public string memo;
    }

    /// <summary>인스펙터에서 시나리오 2 단계 활성 여부(스킵)와 순서를 직관적으로 편집하기 위한 항목입니다.</summary>
    [Serializable]
    public class Scenario2StepEntry
    {
        public Scenario2SequenceStep step;
        public bool enabled = true;
        public string memo;
    }

    /// <summary>
    /// 관제실 VR 시나리오 1·2 전환, 단계 순서/스킵 편집, 출력 변화 시간, 벽면 TV 동기를
    /// 단 한 곳에서 통합 제어하는 중앙 시나리오 매니저입니다.
    /// 코드에서는 ControlRoomScenarioManager.Instance.switchScenario(1 또는 2) 한 줄로 즉시 전환할 수 있습니다.
    /// </summary>
    public class ControlRoomScenarioManager : MonoBehaviour
    {
        public static ControlRoomScenarioManager Instance { get; private set; }

        [Header("1. 기본 시나리오 & 연결 참조")]
        [SerializeField] private ControlRoomScenarioId mDefaultScenario = ControlRoomScenarioId.Scenario1;
        [SerializeField] private bool mResetOnScenarioSwitch = true;
        [SerializeField] private OperatorStationCoordinator mScenario1Coordinator;
        [SerializeField] private Scenario2StationCoordinator mScenario2Coordinator;
        [SerializeField] private WallDisplaySyncController mWallDisplayController;

        [Header("2. 시나리오 1 시간 및 단계 순서 설정 (체크 해제 시 스킵, 드래그로 순서 변경)")]
        [SerializeField, Min(1f)] private float mScenario1ReductionDurationSec = 60f;
        [SerializeField, Min(1f)] private float mScenario1AscensionDurationSec = 60f;
        [SerializeField] private List<Scenario1StepEntry> mScenario1StepOrder = new List<Scenario1StepEntry>();

        [Header("3. 시나리오 2 단계 순서 설정 (체크 해제 시 스킵, 드래그로 순서 변경)")]
        [SerializeField] private List<Scenario2StepEntry> mScenario2StepOrder = new List<Scenario2StepEntry>();

        [Header("4. 시나리오 전환 이벤트")]
        [SerializeField] private UnityEvent<int> mOnScenarioSwitchedEvent = new UnityEvent<int>();

        public event Action<ControlRoomScenarioId> OnScenarioChanged;

        public ControlRoomScenarioId CurrentScenario { get; private set; } = ControlRoomScenarioId.Scenario1;
        public OperatorStationCoordinator Scenario1Coordinator => mScenario1Coordinator;
        public Scenario2StationCoordinator Scenario2Coordinator => mScenario2Coordinator;
        public WallDisplaySyncController WallDisplayController => mWallDisplayController;
        public IReadOnlyList<Scenario1StepEntry> Scenario1StepOrder => mScenario1StepOrder;
        public IReadOnlyList<Scenario2StepEntry> Scenario2StepOrder => mScenario2StepOrder;

        private void Awake()
        {
            try
            {
                if (Instance == null || Instance == this)
                {
                    Instance = this;
                }
                ensureDefaultStepOrdersIfEmpty();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ControlRoomScenarioManager] Awake 오류: {ex}");
            }
        }

        private void Start()
        {
            try
            {
                switchScenario(mDefaultScenario, mResetOnScenarioSwitch);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ControlRoomScenarioManager] Start 오류: {ex}");
            }
        }

        private void OnValidate()
        {
            try
            {
                mScenario1ReductionDurationSec = Mathf.Max(1f, mScenario1ReductionDurationSec);
                mScenario1AscensionDurationSec = Mathf.Max(1f, mScenario1AscensionDurationSec);
                applyScenario1DurationsToCoordinator();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ControlRoomScenarioManager] OnValidate 오류: {ex}");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>에디터 빌더 또는 테스트에서 시나리오 1·2 코디네이터와 벽면 TV 컨트롤러 참조를 주입합니다.</summary>
        public void configureReferences(
            OperatorStationCoordinator s1Coordinator,
            Scenario2StationCoordinator s2Coordinator,
            WallDisplaySyncController wallDisplayController)
        {
            try
            {
                mScenario1Coordinator = s1Coordinator;
                mScenario2Coordinator = s2Coordinator;
                mWallDisplayController = wallDisplayController;
                ensureDefaultStepOrdersIfEmpty();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ControlRoomScenarioManager] configureReferences 오류: {ex}");
            }
        }

        /// <summary>단일 메서드 호출로 시나리오 1 ↔ 2를 전환하고 코디네이터·순서 리졸버·벽면 TV를 즉시 동기화합니다.</summary>
        public bool switchScenario(ControlRoomScenarioId targetScenario, bool? resetOverride = null)
        {
            try
            {
                ensureDefaultStepOrdersIfEmpty();
                bool shouldReset = resetOverride ?? mResetOnScenarioSwitch;

                if (targetScenario == ControlRoomScenarioId.Scenario1)
                {
                    if (mScenario2Coordinator != null) mScenario2Coordinator.gameObject.SetActive(false);
                    if (mScenario1Coordinator != null)
                    {
                        mScenario1Coordinator.gameObject.SetActive(true);
                        mScenario1Coordinator.initializeStation();
                        if (shouldReset) mScenario1Coordinator.resetScenarioToInitial();
                        applyScenario1DurationsToCoordinator();
                        bindResolvers();
                    }
                }
                else if (targetScenario == ControlRoomScenarioId.Scenario2)
                {
                    if (mScenario1Coordinator != null) mScenario1Coordinator.gameObject.SetActive(false);
                    if (mScenario2Coordinator != null)
                    {
                        mScenario2Coordinator.gameObject.SetActive(true);
                        mScenario2Coordinator.initializeStation();
                        if (shouldReset) mScenario2Coordinator.resetScenarioToInitial();
                        bindResolvers();
                    }
                }
                else
                {
                    Debug.LogWarning($"[ControlRoomScenarioManager] 지원하지 않는 시나리오 ID입니다: {targetScenario}");
                    return false;
                }

                CurrentScenario = targetScenario;
                mWallDisplayController?.syncFromActiveCoordinator();
                OnScenarioChanged?.Invoke(CurrentScenario);
                mOnScenarioSwitchedEvent?.Invoke((int)CurrentScenario);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ControlRoomScenarioManager] switchScenario({targetScenario}) 오류: {ex}");
                return false;
            }
        }

        /// <summary>정수(1 또는 2)로 시나리오를 전환하는 오버로드입니다. Unity UI Button OnClick(int)에 직접 연결 가능합니다.</summary>
        public bool switchScenario(int scenarioNumber)
        {
            if (scenarioNumber == 1) return switchScenario(ControlRoomScenarioId.Scenario1);
            if (scenarioNumber == 2) return switchScenario(ControlRoomScenarioId.Scenario2);

            Debug.LogWarning($"[ControlRoomScenarioManager] 잘못된 시나리오 번호({scenarioNumber})입니다. 1 또는 2만 허용됩니다.");
            return false;
        }

        /// <summary>시나리오 1(정상 운전: 100%→80%→100%)로 전환합니다.</summary>
        [ContextMenu("시나리오 1로 전환")]
        public void switchToScenario1()
        {
            switchScenario(ControlRoomScenarioId.Scenario1);
        }

        /// <summary>시나리오 2(비상 LOCA: Small Charging Line Break)로 전환합니다.</summary>
        [ContextMenu("시나리오 2로 전환")]
        public void switchToScenario2()
        {
            switchScenario(ControlRoomScenarioId.Scenario2);
        }

        /// <summary>현재 활성화된 시나리오를 초기 상태로 재설정하고 벽면 TV를 동기화합니다.</summary>
        public void resetCurrentScenario()
        {
            try
            {
                if (CurrentScenario == ControlRoomScenarioId.Scenario1 && mScenario1Coordinator != null)
                {
                    mScenario1Coordinator.resetScenarioToInitial();
                    applyScenario1DurationsToCoordinator();
                }
                else if (CurrentScenario == ControlRoomScenarioId.Scenario2 && mScenario2Coordinator != null)
                {
                    mScenario2Coordinator.resetScenarioToInitial();
                }

                bindResolvers();
                mWallDisplayController?.syncFromActiveCoordinator();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ControlRoomScenarioManager] resetCurrentScenario 오류: {ex}");
            }
        }

        /// <summary>현재 활성 시나리오를 인스펙터 순서 리스트 기준으로 다음 활성 단계 1칸 전진시킵니다.</summary>
        public bool advanceCurrentScenarioStep()
        {
            try
            {
                ensureDefaultStepOrdersIfEmpty();
                if (CurrentScenario == ControlRoomScenarioId.Scenario1 && mScenario1Coordinator?.SimulationModel != null)
                {
                    var current = mScenario1Coordinator.SimulationModel.CaptureStep;
                    int curIdx = mScenario1StepOrder.FindIndex(e => e.step == current);
                    for (int i = curIdx + 1; i < mScenario1StepOrder.Count; i++)
                    {
                        if (mScenario1StepOrder[i].enabled)
                        {
                            mScenario1Coordinator.SimulationModel.setCaptureStep(mScenario1StepOrder[i].step);
                            mWallDisplayController?.syncFromActiveCoordinator();
                            return true;
                        }
                    }
                    return false;
                }

                if (CurrentScenario == ControlRoomScenarioId.Scenario2 && mScenario2Coordinator?.Scenario2Model != null)
                {
                    var current = mScenario2Coordinator.Scenario2Model.CurrentStep;
                    int curIdx = mScenario2StepOrder.FindIndex(e => e.step == current);
                    for (int i = curIdx + 1; i < mScenario2StepOrder.Count; i++)
                    {
                        if (mScenario2StepOrder[i].enabled)
                        {
                            mScenario2Coordinator.Scenario2Model.advanceStep(mScenario2StepOrder[i].step);
                            mWallDisplayController?.syncFromActiveCoordinator();
                            return true;
                        }
                    }
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ControlRoomScenarioManager] advanceCurrentScenarioStep 오류: {ex}");
                return false;
            }
        }

        /// <summary>시나리오 1의 원하는 단계로 즉시 점프합니다.</summary>
        public void jumpToScenario1Step(CaptureSequenceStep targetStep)
        {
            try
            {
                if (CurrentScenario != ControlRoomScenarioId.Scenario1)
                {
                    switchScenario(ControlRoomScenarioId.Scenario1, true);
                }
                mScenario1Coordinator?.SimulationModel?.setCaptureStep(targetStep);
                mWallDisplayController?.syncFromActiveCoordinator();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ControlRoomScenarioManager] jumpToScenario1Step({targetStep}) 오류: {ex}");
            }
        }

        /// <summary>시나리오 2의 원하는 단계로 즉시 점프합니다.</summary>
        public void jumpToScenario2Step(Scenario2SequenceStep targetStep)
        {
            try
            {
                if (CurrentScenario != ControlRoomScenarioId.Scenario2)
                {
                    switchScenario(ControlRoomScenarioId.Scenario2, true);
                }
                mScenario2Coordinator?.Scenario2Model?.advanceStep(targetStep);
                mWallDisplayController?.syncFromActiveCoordinator();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ControlRoomScenarioManager] jumpToScenario2Step({targetStep}) 오류: {ex}");
            }
        }

        /// <summary>시나리오 1 출력 감소(100%→80%) 및 상승 복귀(80%→100%) 소요 시간(초)을 동시 변경합니다.</summary>
        public void setScenario1Durations(float reductionDurationSec, float ascensionDurationSec)
        {
            try
            {
                mScenario1ReductionDurationSec = Mathf.Max(1f, reductionDurationSec);
                mScenario1AscensionDurationSec = Mathf.Max(1f, ascensionDurationSec);
                applyScenario1DurationsToCoordinator();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ControlRoomScenarioManager] setScenario1Durations 오류: {ex}");
            }
        }

        /// <summary>시나리오 1 특정 단계의 활성/스킵 여부를 코드에서 설정합니다.</summary>
        public void setScenario1StepEnabled(CaptureSequenceStep step, bool enabled)
        {
            try
            {
                ensureDefaultStepOrdersIfEmpty();
                var entry = mScenario1StepOrder.Find(e => e.step == step);
                if (entry != null) entry.enabled = enabled;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ControlRoomScenarioManager] setScenario1StepEnabled 오류: {ex}");
            }
        }

        /// <summary>시나리오 2 특정 단계의 활성/스킵 여부를 코드에서 설정합니다.</summary>
        public void setScenario2StepEnabled(Scenario2SequenceStep step, bool enabled)
        {
            try
            {
                ensureDefaultStepOrdersIfEmpty();
                var entry = mScenario2StepOrder.Find(e => e.step == step);
                if (entry != null) entry.enabled = enabled;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ControlRoomScenarioManager] setScenario2StepEnabled 오류: {ex}");
            }
        }

        /// <summary>시나리오 1·2의 단계 순서 목록을 표준 기본 순서(27단계 / 56단계)로 초기화합니다.</summary>
        [ContextMenu("기본 시나리오 1·2 단계 순서로 초기화")]
        public void resetDefaultStepOrders()
        {
            try
            {
                mScenario1StepOrder.Clear();
                foreach (CaptureSequenceStep s in Enum.GetValues(typeof(CaptureSequenceStep)))
                {
                    mScenario1StepOrder.Add(new Scenario1StepEntry
                    {
                        step = s,
                        enabled = true,
                        memo = getScenario1StepMemo(s)
                    });
                }

                mScenario2StepOrder.Clear();
                foreach (Scenario2SequenceStep s in Enum.GetValues(typeof(Scenario2SequenceStep)))
                {
                    mScenario2StepOrder.Add(new Scenario2StepEntry
                    {
                        step = s,
                        enabled = true,
                        memo = getScenario2StepMemo(s)
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ControlRoomScenarioManager] resetDefaultStepOrders 오류: {ex}");
            }
        }

        private void ensureDefaultStepOrdersIfEmpty()
        {
            if (mScenario1StepOrder.Count == 0 || mScenario2StepOrder.Count == 0)
            {
                resetDefaultStepOrders();
            }
        }

        private void applyScenario1DurationsToCoordinator()
        {
            if (mScenario1Coordinator == null) return;
            mScenario1Coordinator.PowerManeuverDurationSeconds = mScenario1ReductionDurationSec;
            mScenario1Coordinator.PowerAscensionDurationSeconds = mScenario1AscensionDurationSec;
        }

        private void bindResolvers()
        {
            if (mScenario1Coordinator?.SimulationModel != null)
            {
                mScenario1Coordinator.SimulationModel.StepSkipResolver = resolveNextScenario1Step;
            }
            if (mScenario2Coordinator?.Scenario2Model != null)
            {
                mScenario2Coordinator.Scenario2Model.StepSkipResolver = resolveNextScenario2Step;
            }
        }

        private CaptureSequenceStep resolveNextScenario1Step(CaptureSequenceStep requested)
        {
            int idx = mScenario1StepOrder.FindIndex(e => e.step == requested);
            if (idx < 0) return requested;
            if (mScenario1StepOrder[idx].enabled) return requested;

            for (int i = idx + 1; i < mScenario1StepOrder.Count; i++)
            {
                if (mScenario1StepOrder[i].enabled) return mScenario1StepOrder[i].step;
            }
            return requested;
        }

        private Scenario2SequenceStep resolveNextScenario2Step(Scenario2SequenceStep requested)
        {
            int idx = mScenario2StepOrder.FindIndex(e => e.step == requested);
            if (idx < 0) return requested;
            if (mScenario2StepOrder[idx].enabled) return requested;

            for (int i = idx + 1; i < mScenario2StepOrder.Count; i++)
            {
                if (mScenario2StepOrder[i].enabled) return mScenario2StepOrder[i].step;
            }
            return requested;
        }

        private static string getScenario1StepMemo(CaptureSequenceStep step)
        {
            switch (step)
            {
                case CaptureSequenceStep.Step00_InstructorPaused: return "초기 정지 상태 (Instructor Paused)";
                case CaptureSequenceStep.Step01_InstructorRunning: return "Instructor ▶ 실행";
                case CaptureSequenceStep.Step02_ProcessLibraryEmpty: return "Process Library 열기";
                case CaptureSequenceStep.Step03_IopExpanded: return "IOP 트리 확장";
                case CaptureSequenceStep.Step04_PowerOpExpanded: return "D. Power Operation 확장";
                case CaptureSequenceStep.Step05_PowerReductionPreview: return "C. Power Reduction 미리보기";
                case CaptureSequenceStep.Step06_PowerReductionActive: return "C. Power Reduction 시작";
                case CaptureSequenceStep.Step07_Step1Checked: return "1번 항목 체크";
                case CaptureSequenceStep.Step08_Step3MenuOpen: return "3번 N/A 메뉴 열기";
                case CaptureSequenceStep.Step09_Step3NaCollapsed: return "3번 N/A 선택";
                case CaptureSequenceStep.Step10_PowerManeuveringSetup: return "Power Maneuvering 이동";
                case CaptureSequenceStep.Step11_PmParamsEntered: return "Target 80 / Rate 300 입력";
                case CaptureSequenceStep.Step12_PmValidated: return "80% 감발 Validate";
                case CaptureSequenceStep.Step13_PmRunningCardView: return "80% 감발 Execute";
                case CaptureSequenceStep.Step14_ReturnedToProcCard: return "C. Power Reduction 복귀";
                case CaptureSequenceStep.Step15_Step6Checked: return "6번 항목 체크";
                case CaptureSequenceStep.Step16_Step8Checked: return "8번 항목 체크";
                case CaptureSequenceStep.Step17_ReturnedToPmRamp80: return "80% 감발 진행 (1분)";
                case CaptureSequenceStep.Step18_Ramp80Completed: return "80% 감발 완료";
                case CaptureSequenceStep.Step19_ProceduresTabForAscension: return "Procedures 탭 복귀";
                case CaptureSequenceStep.Step20_PowerAscensionPreview: return "B. Power Ascension 미리보기";
                case CaptureSequenceStep.Step21_PowerAscensionActive: return "B. Power Ascension 시작";
                case CaptureSequenceStep.Step22_AscPmSetup: return "100% 복귀 Power Maneuvering";
                case CaptureSequenceStep.Step23_AscPmParamsEntered: return "Target 100 / Rate 20 입력";
                case CaptureSequenceStep.Step24_AscPmValidated: return "100% 복귀 Validate";
                case CaptureSequenceStep.Step25_AscPmRamp100: return "100% 상승 진행 (1분)";
                case CaptureSequenceStep.Step26_Scenario1Completed: return "시나리오 1 최종 완료";
                default: return step.ToString();
            }
        }

        private static string getScenario2StepMemo(Scenario2SequenceStep step)
        {
            switch (step)
            {
                case Scenario2SequenceStep.S2_00_ProcLibInitial: return "00:14 Process Library(CE 절차) 시작";
                case Scenario2SequenceStep.S2_03_CeInitialAlarm: return "00:19 Primary > CE 진입";
                case Scenario2SequenceStep.S2_05_Ce0130Opened: return "00:34 CE-AOV-0130 Open";
                case Scenario2SequenceStep.S2_07_CeFlowRerouted: return "00:42 CE-AOV-0128 Close";
                case Scenario2SequenceStep.S2_08_ModOverviewChecked: return "00:54 Module Overview (PZR 58.3%)";
                case Scenario2SequenceStep.S2_12_CvcRecircChecked: return "01:35 CVC Recirc & Makeup 확인";
                case Scenario2SequenceStep.S2_14_CeCheckedForTrip: return "01:55 CE 트립 기준(<7 psia, <20 gpm) 확인";
                case Scenario2SequenceStep.S2_19_Mov0318Closed: return "02:20 CVC-MOV-0318 Close (2차 경보 발생)";
                case Scenario2SequenceStep.S2_21_CvcsValvesClosed: return "02:26 CVC-AOV-0339 Close (좌측 하단)";
                case Scenario2SequenceStep.S2_23_DeminsChecked: return "02:27 Demins & Letdowns (0 gpm)";
                case Scenario2SequenceStep.S2_26_CvcAutoSearched: return "03:00 Automations Search('cvc')";
                case Scenario2SequenceStep.S2_28_CvcMakeupValidated: return "03:06 Boric Acid Only Validate";
                case Scenario2SequenceStep.S2_29_CvcMakeupRunning: return "03:08 CVC Makeup Execute (Pump B)";
                case Scenario2SequenceStep.S2_31_AlarmChecked: return "03:16 상단 경보창 확인 후 닫기";
                case Scenario2SequenceStep.S2_38_RecircManualMode: return "03:53 Recirc Lock -> Manual 전환";
                case Scenario2SequenceStep.S2_40_RecircPumpsStopped: return "04:08 Recirc Pump A 정지 (Alarm 2->1)";
                case Scenario2SequenceStep.S2_43_IsoShowAllActive: return "04:17 Isolation SHOW ALL 토글";
                case Scenario2SequenceStep.S2_44_IsoValvesChecked: return "04:23 CVC S (ISV-0323) 확인";
                case Scenario2SequenceStep.S2_48_CeSampleChecked: return "05:24 CE Sample Vessel (0.11 ft) 확인";
                case Scenario2SequenceStep.S2_54_RccwOpened: return "05:42 0A > Primary > RCCW 진입";
                case Scenario2SequenceStep.S2_55_Scenario2Completed: return "06:05 0A RCCW Tank 21.7% 확인 완료";
                default: return step.ToString();
            }
        }
    }
}
