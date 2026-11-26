using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ControlRoom.UI
{
    /// <summary>
    /// 중앙 관리자 책상(OP_CENTER)의 3대 모니터(좌·중·우) 16:9 World-Space Canvas를 초기화하고
    /// 실시간 시뮬레이션 상태·사운드·타이머를 동기화하는 코디네이터입니다.
    /// </summary>
    public class OperatorStationCoordinator : MonoBehaviour
    {
        [Header("3대 모니터 World-Space Canvas 연결")]
        [SerializeField] private Canvas mLeftMonitorCanvas;
        [SerializeField] private Canvas mCenterMonitorCanvas;
        [SerializeField] private Canvas mRightMonitorCanvas;

        [Header("폰트 및 스프라이트 번들")]
        [SerializeField] private TMP_FontAsset mKoreanFont;
        [SerializeField] private ControlRoomSpritePack mSpritePack = new ControlRoomSpritePack();

        [Header("오디오 클립 (1차 납품 규격)")]
        [SerializeField] private AudioClip mServerRoomBgClip;
        [SerializeField] private AudioClip mMouseClickClip;

        [Header("Power Maneuvering 변화 시간 설정")]
        [SerializeField, Min(1f)] private float mPowerManeuverDurationSeconds = 60f;
        [SerializeField, Min(1f)] private float mPowerAscensionDurationSeconds = 60f;

        [Header("시나리오 3D 가이드 화살표")]
        [SerializeField] private ScenarioGuideArrow3D mGuideArrow;

        private AudioSource mBgAudioSource;
        private AudioSource mClickAudioSource;

        private const float HEARTBEAT_INTERVAL_SEC = 0.6f;
        private float mHeartbeatTimer;
        private int mHeartbeatIndex;

        public PlantSimulationModel SimulationModel { get; private set; }
        public ScenarioGuideController GuideController { get; private set; }
        public LeftMonitorController LeftMonitor { get; private set; }
        public MainMonitorController MainMonitor { get; private set; }
        public RightMonitorController RightMonitor { get; private set; }
        public ScenarioGuideArrow3D GuideArrow => mGuideArrow;

        /// <summary>Power Maneuvering 출력 감소 전체 소요 시간(초, 인스펙터에서 조정 가능)</summary>
        public float PowerManeuverDurationSeconds
        {
            get => mPowerManeuverDurationSeconds;
            set
            {
                mPowerManeuverDurationSeconds = Mathf.Max(1f, value);
                SimulationModel?.setPowerManeuverDurationSeconds(mPowerManeuverDurationSeconds);
            }
        }

        /// <summary>Power Maneuvering 출력 상승(80%→100%) 전체 소요 시간(초, 인스펙터에서 조정 가능)</summary>
        public float PowerAscensionDurationSeconds
        {
            get => mPowerAscensionDurationSeconds;
            set
            {
                mPowerAscensionDurationSeconds = Mathf.Max(1f, value);
                SimulationModel?.setPowerAscensionDurationSeconds(mPowerAscensionDurationSeconds);
            }
        }

        private bool mIsInitialized;

        private void Awake()
        {
            initializeStation();
        }

        private void Start()
        {
            bindCanvasWorldCamera();
            deduplicateEventSystems();
            startAmbientSound();
        }

        private void Update()
        {
            if (mIsInitialized && SimulationModel != null)
            {
                float dt = Time.deltaTime;
                SimulationModel.setPowerManeuverDurationSeconds(mPowerManeuverDurationSeconds);
                SimulationModel.setPowerAscensionDurationSeconds(mPowerAscensionDurationSeconds);
                tickSharedChrome(dt);
                MainMonitor?.tickVisualTimers(dt);
                SimulationModel.tickSimulation(dt);
                mGuideArrow?.tickAnimation(dt);
            }
        }

        private void OnValidate()
        {
            mPowerManeuverDurationSeconds = Mathf.Max(1f, mPowerManeuverDurationSeconds);
            mPowerAscensionDurationSeconds = Mathf.Max(1f, mPowerAscensionDurationSeconds);
            SimulationModel?.setPowerManeuverDurationSeconds(mPowerManeuverDurationSeconds);
            SimulationModel?.setPowerAscensionDurationSeconds(mPowerAscensionDurationSeconds);
        }

        /// <summary>3개 모니터의 상단 3점 하트비트 순환(0.6초 간격) 및 하단 기기 실시간 시계를 동기화합니다.</summary>
        public void tickSharedChrome(float deltaTime)
        {
            try
            {
                mHeartbeatTimer += Mathf.Max(0f, deltaTime);
                if (mHeartbeatTimer >= HEARTBEAT_INTERVAL_SEC)
                {
                    int steps = Mathf.FloorToInt(mHeartbeatTimer / HEARTBEAT_INTERVAL_SEC);
                    mHeartbeatTimer -= steps * HEARTBEAT_INTERVAL_SEC;
                    mHeartbeatIndex = (mHeartbeatIndex + steps) % 3;
                }

                LeftMonitor?.tickChrome(mHeartbeatIndex);
                MainMonitor?.tickChrome(mHeartbeatIndex);
                RightMonitor?.tickChrome(mHeartbeatIndex);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OperatorStationCoordinator] tickSharedChrome 오류: {ex}");
            }
        }

        private void OnDestroy()
        {
            if (SimulationModel != null)
            {
                SimulationModel.OnStateChanged -= handleModelStateChanged;
            }
        }

        /// <summary>중앙 시나리오 매니저에서 시나리오 1을 초기 스탠바이 상태로 재초기화할 때 호출합니다.</summary>
        public void resetScenarioToInitial()
        {
            try
            {
                if (!mIsInitialized) initializeStation();
                SimulationModel?.resetToStandby();
                LeftMonitor?.refreshUI();
                MainMonitor?.refreshUI();
                RightMonitor?.refreshUI();
                refreshGuideArrowTarget();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OperatorStationCoordinator] resetScenarioToInitial 오류: {ex}");
            }
        }

        /// <summary>에디터 빌더 또는 런타임에서 3개 모니터 UI와 시뮬레이션 모델을 초기화합니다.</summary>
        public void initializeStation()
        {
            try
            {
                if (mIsInitialized) return;

                ControlRoomUIFactory.DefaultFont = mKoreanFont;
                ControlRoomUIFactory.OnAnyClickSound = playMouseClickSound;

                SimulationModel = new PlantSimulationModel();
                SimulationModel.setPowerManeuverDurationSeconds(mPowerManeuverDurationSeconds);
                SimulationModel.setPowerAscensionDurationSeconds(mPowerAscensionDurationSeconds);
                GuideController = new ScenarioGuideController(SimulationModel);

                LeftMonitor = new LeftMonitorController(SimulationModel);
                MainMonitor = new MainMonitorController(SimulationModel, GuideController);
                RightMonitor = new RightMonitorController(SimulationModel);

                if (mLeftMonitorCanvas != null)
                {
                    clearChildren(mLeftMonitorCanvas.transform);
                    LeftMonitor.buildUI(mLeftMonitorCanvas.transform, mSpritePack);
                }

                if (mCenterMonitorCanvas != null)
                {
                    clearChildren(mCenterMonitorCanvas.transform);
                    MainMonitor.buildUI(mCenterMonitorCanvas.transform, mSpritePack);
                }

                if (mRightMonitorCanvas != null)
                {
                    clearChildren(mRightMonitorCanvas.transform);
                    RightMonitor.buildUI(mRightMonitorCanvas.transform, mSpritePack);
                }

                SimulationModel.OnStateChanged += handleModelStateChanged;
                mHeartbeatIndex = 0;
                mHeartbeatTimer = 0f;
                tickSharedChrome(0f);
                ensureGuideArrow();
                refreshGuideArrowTarget();
                mIsInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OperatorStationCoordinator] initializeStation 오류: {ex}");
            }
        }

        public void configureReferences(
            Canvas leftCanvas,
            Canvas centerCanvas,
            Canvas rightCanvas,
            TMP_FontAsset font,
            ControlRoomSpritePack spritePack,
            AudioClip bgClip,
            AudioClip clickClip)
        {
            mLeftMonitorCanvas = leftCanvas;
            mCenterMonitorCanvas = centerCanvas;
            mRightMonitorCanvas = rightCanvas;
            mKoreanFont = font;
            mSpritePack = spritePack ?? new ControlRoomSpritePack();
            mServerRoomBgClip = bgClip;
            mMouseClickClip = clickClip;
        }

        private void handleModelStateChanged()
        {
            LeftMonitor?.refreshUI();
            MainMonitor?.refreshUI();
            RightMonitor?.refreshUI();
            refreshGuideArrowTarget();
        }

        /// <summary>시나리오 3D 가이드 화살표 오브젝트를 확보하거나 생성합니다.</summary>
        public void ensureGuideArrow()
        {
            try
            {
                if (mGuideArrow == null)
                {
                    Transform existing = transform.Find("GuideArrow_Scenario3D");
                    if (existing != null)
                    {
                        mGuideArrow = existing.GetComponent<ScenarioGuideArrow3D>() ?? existing.gameObject.AddComponent<ScenarioGuideArrow3D>();
                    }
                    else
                    {
                        var arrowGo = new GameObject("GuideArrow_Scenario3D");
                        arrowGo.transform.SetParent(transform, false);
                        mGuideArrow = arrowGo.AddComponent<ScenarioGuideArrow3D>();
                    }
                }
                mGuideArrow.ensureVisuals();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OperatorStationCoordinator] ensureGuideArrow 오류: {ex}");
            }
        }

        /// <summary>현재 시나리오 진행 단계에 맞춰 3D 가이드 화살표 대상 버튼과 활성 여부를 동기화합니다.</summary>
        public void refreshGuideArrowTarget()
        {
            try
            {
                if (mGuideArrow == null || MainMonitor == null) return;
                RectTransform targetRect = MainMonitor.getCurrentScenarioGuideTargetRect();
                mGuideArrow.setTarget(targetRect, mCenterMonitorCanvas);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OperatorStationCoordinator] refreshGuideArrowTarget 오류: {ex}");
            }
        }

        private void clearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        private void bindCanvasWorldCamera()
        {
            try
            {
                Camera targetCam = Camera.main ?? FindObjectOfType<Camera>();
                if (targetCam == null) return;

                if (mLeftMonitorCanvas != null) mLeftMonitorCanvas.worldCamera = targetCam;
                if (mCenterMonitorCanvas != null) mCenterMonitorCanvas.worldCamera = targetCam;
                if (mRightMonitorCanvas != null) mRightMonitorCanvas.worldCamera = targetCam;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OperatorStationCoordinator] bindCanvasWorldCamera 경고: {ex.Message}");
            }
        }

        private void deduplicateEventSystems()
        {
            try
            {
                var systems = FindObjectsOfType<EventSystem>(true);
                if (systems.Length <= 1) return;

                EventSystem keep = systems[0];
                foreach (var es in systems)
                {
                    if (es.GetComponent<XRUIInputModule>() != null)
                    {
                        keep = es;
                        break;
                    }
                }
                foreach (var es in systems)
                {
                    if (es != keep) es.gameObject.SetActive(false);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OperatorStationCoordinator] deduplicateEventSystems 경고: {ex.Message}");
            }
        }

        private void startAmbientSound()
        {
            try
            {
                if (!Application.isPlaying) return;

                if (mServerRoomBgClip != null && mBgAudioSource == null)
                {
                    mBgAudioSource = gameObject.AddComponent<AudioSource>();
                    mBgAudioSource.clip = mServerRoomBgClip;
                    mBgAudioSource.loop = true;
                    mBgAudioSource.volume = 0.25f;
                    mBgAudioSource.spatialBlend = 0f;
                    mBgAudioSource.Play();
                }

                if (mMouseClickClip != null && mClickAudioSource == null)
                {
                    mClickAudioSource = gameObject.AddComponent<AudioSource>();
                    mClickAudioSource.playOnAwake = false;
                    mClickAudioSource.volume = 0.65f;
                    mClickAudioSource.spatialBlend = 0f;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OperatorStationCoordinator] startAmbientSound 경고: {ex.Message}");
            }
        }

        public void playMouseClickSound()
        {
            try
            {
                if (Application.isPlaying && mClickAudioSource != null && mMouseClickClip != null)
                {
                    mClickAudioSource.PlayOneShot(mMouseClickClip);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[OperatorStationCoordinator] playMouseClickSound 경고: {ex.Message}");
            }
        }
    }
}
