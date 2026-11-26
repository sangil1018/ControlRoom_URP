using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace ControlRoom.UI
{
    /// <summary>
    /// 시나리오 2(원자로 냉각재 누설 LOCA: Small Charging Line Break) 전용
    /// 중앙 책상(OP_CENTER) 3대 모니터(좌: Instructor Primary Breaks / 중: 조작 메인 / 우: 12-Unit Overview) 코디네이터입니다.
    /// </summary>
    public class Scenario2StationCoordinator : MonoBehaviour
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

        [Header("시나리오 3D 가이드 화살표")]
        [SerializeField] private ScenarioGuideArrow3D mGuideArrow;

        private AudioSource mBgAudioSource;
        private AudioSource mClickAudioSource;

        private const float HEARTBEAT_INTERVAL_SEC = 0.6f;
        private float mHeartbeatTimer;
        private int mHeartbeatIndex;
        private bool mIsInitialized;

        public PlantSimulationModel SharedPlantModel { get; private set; }
        public Scenario2ControllerModel Scenario2Model { get; private set; }
        public Scenario2LeftMonitorController LeftMonitor { get; private set; }
        public Scenario2MainMonitorController MainMonitor { get; private set; }
        public RightMonitorController RightMonitor { get; private set; }
        public ScenarioGuideArrow3D GuideArrow => mGuideArrow;

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
            if (mIsInitialized && Scenario2Model != null)
            {
                float dt = Time.deltaTime;
                tickSharedChrome(dt);
                Scenario2Model.tick(dt);
                mGuideArrow?.tickAnimation(dt);
            }
        }

        private void OnDestroy()
        {
            if (Scenario2Model != null)
            {
                Scenario2Model.OnScenario2StateChanged -= handleScenario2StateChanged;
            }
        }

        /// <summary>중앙 시나리오 매니저에서 시나리오 2를 초기 LOCA 상태로 재초기화할 때 호출합니다.</summary>
        public void resetScenarioToInitial()
        {
            try
            {
                if (!mIsInitialized) initializeStation();
                Scenario2Model?.initializeScenario2();
                LeftMonitor?.refreshUI();
                MainMonitor?.refreshUI();
                RightMonitor?.refreshUI();
                refreshGuideArrowTarget();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Scenario2StationCoordinator] resetScenarioToInitial 오류: {ex}");
            }
        }

        /// <summary>에디터 빌더 또는 런타임에서 시나리오 2 3모니터 UI와 상태 모델을 초기화합니다.</summary>
        public void initializeStation()
        {
            try
            {
                if (mIsInitialized) return;

                ControlRoomUIFactory.DefaultFont = mKoreanFont;
                ControlRoomUIFactory.OnAnyClickSound = playMouseClickSound;

                SharedPlantModel = new PlantSimulationModel();
                Scenario2Model = new Scenario2ControllerModel(SharedPlantModel);
                Scenario2Model.initializeScenario2();

                LeftMonitor = new Scenario2LeftMonitorController(Scenario2Model);
                MainMonitor = new Scenario2MainMonitorController(Scenario2Model);
                RightMonitor = new RightMonitorController(SharedPlantModel);
                RightMonitor.setHeaderUnitCodeOverride("00");

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

                Scenario2Model.OnScenario2StateChanged += handleScenario2StateChanged;
                mHeartbeatIndex = 0;
                mHeartbeatTimer = 0f;
                tickSharedChrome(0f);
                ensureGuideArrow();
                refreshGuideArrowTarget();
                mIsInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Scenario2StationCoordinator] initializeStation 오류: {ex}");
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
                Debug.LogError($"[Scenario2StationCoordinator] tickSharedChrome 오류: {ex}");
            }
        }

        private void handleScenario2StateChanged()
        {
            LeftMonitor?.refreshUI();
            MainMonitor?.refreshUI();
            RightMonitor?.refreshUI();
            refreshGuideArrowTarget();
        }

        /// <summary>시나리오 2 3D 가이드 화살표 오브젝트를 확보하거나 생성합니다.</summary>
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
                Debug.LogError($"[Scenario2StationCoordinator] ensureGuideArrow 오류: {ex}");
            }
        }

        /// <summary>현재 시나리오 2 단계에 맞춰 3D 가이드 화살표 타겟을 갱신합니다.</summary>
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
                Debug.LogError($"[Scenario2StationCoordinator] refreshGuideArrowTarget 오류: {ex}");
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
                Debug.LogWarning($"[Scenario2StationCoordinator] bindCanvasWorldCamera 경고: {ex.Message}");
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
                Debug.LogWarning($"[Scenario2StationCoordinator] deduplicateEventSystems 경고: {ex.Message}");
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
                Debug.LogWarning($"[Scenario2StationCoordinator] startAmbientSound 경고: {ex.Message}");
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
                Debug.LogWarning($"[Scenario2StationCoordinator] playMouseClickSound 경고: {ex.Message}");
            }
        }
    }
}
