using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ControlRoom.UI
{
    /// <summary>
    /// 관제실 전면 벽면 TV(중앙 시계·12호기 현황 TV, 06호기 상단/하단 TV)의
    /// 실시간 시스템 시계(DateTime.Now) 및 경보/주의 아이콘·상승 배지·하트비트를 동기화하는 컨트롤러입니다.
    /// 기존 시나리오 1(OperatorStationCoordinator)과 시나리오 2(Scenario2StationCoordinator) 중
    /// 현재 활성화된 코디네이터의 PlantSimulationModel을 자동 감지하여 연동합니다.
    /// </summary>
    public class WallDisplaySyncController : MonoBehaviour
    {
        [Header("벽면 TV World-Space Canvas 연결")]
        [SerializeField] private Canvas mCenterWallCanvas;
        [SerializeField] private Canvas mUnit06UpperCanvas;
        [SerializeField] private Canvas mUnit06LowerCanvas;
        [SerializeField] private Canvas[] mAdditionalUnitUpperCanvases;
        [SerializeField] private Canvas[] mAdditionalUnitLowerCanvases;

        [Header("폰트 및 스프라이트 번들")]
        [SerializeField] private TMP_FontAsset mKoreanFont;
        [SerializeField] private ControlRoomSpritePack mSpritePack = new ControlRoomSpritePack();

        // 중앙 벽면 TV (ST_CTR_TV_Upper_Screen) 위젯
        private TextMeshProUGUI mCenterTimeTxt;
        private TextMeshProUGUI mCenterDateTxt;
        private readonly Image[] mCenterHeartbeatDots = new Image[3];
        private Image mCtrAlarmImg;
        private Image mCtrCautionImg;
        private TextMeshProUGUI mCtrAlarmLeftTxt;
        private TextMeshProUGUI mCtrAlarmTopTxt;
        private TextMeshProUGUI mCtrAlarmBotTxt;
        private TextMeshProUGUI mCtrCautionTopTxt;
        private TextMeshProUGUI mCtrCautionBotTxt;
        private readonly GameObject[] mUnit06UpArrows = new GameObject[3];

        // 06호기 상단 TV (ST_06_TV_Upper_Screen) 위젯
        private Image mU6AlarmImg;
        private Image mU6CautionImg;
        private TextMeshProUGUI mU6AlarmLeftTxt;
        private TextMeshProUGUI mU6AlarmTopTxt;
        private TextMeshProUGUI mU6AlarmBotTxt;
        private TextMeshProUGUI mU6CautionTopTxt;
        private TextMeshProUGUI mU6CautionBotTxt;
        private readonly Image[] mU6HeartbeatDots = new Image[3];

        // 06호기 하단 4분할 TV (ST_06_TV_Lower_Screen) E2 서브화면 배지 및 좌측 상단 하트비트
        private readonly Image[] mLowerAlarmImgs = new Image[3];
        private readonly Image[] mLowerCautionImgs = new Image[3];
        private readonly Image[] mLowerNoticeImgs = new Image[3];
        private readonly Image[] mU6LowerHeartbeatDots = new Image[3];

        // 나머지 호기(01~05, 07~12) 상단/하단 TV 3점 하트비트 오버레이 그룹
        private readonly List<Image[]> mAdditionalHeartbeatDotGroups = new List<Image[]>();
        private readonly List<Image[]> mAdditionalLowerHeartbeatDotGroups = new List<Image[]>();

        private const float HEARTBEAT_INTERVAL_SEC = 0.6f;
        private float mBlinkTimer;
        private int mHeartbeatIndex;
        private bool mBlinkPhaseOn = true;
        private int mLastSecond = -1;
        private bool mIsInitialized;

        // 단위 테스트 검증용 읽기 전용 프로퍼티
        public string CenterClockTimeText => mCenterTimeTxt != null ? mCenterTimeTxt.text : string.Empty;
        public string CenterClockDateText => mCenterDateTxt != null ? mCenterDateTxt.text : string.Empty;
        public string Unit06AlarmCountText => mU6AlarmTopTxt != null ? mU6AlarmTopTxt.text : string.Empty;
        public string Unit06CautionCountText => mU6CautionTopTxt != null ? mU6CautionTopTxt.text : string.Empty;
        public bool IsUnit06AlarmIconOn { get; private set; }
        public bool IsUnit06CautionIconOn { get; private set; }
        public bool AreUnit06UpArrowsVisible { get; private set; }
        public int ActiveHeartbeatIndex => mHeartbeatIndex;
        public int TotalHeartbeatGroupCount =>
            (mCenterHeartbeatDots[0] != null ? 1 : 0)
            + (mU6HeartbeatDots[0] != null ? 1 : 0)
            + (mU6LowerHeartbeatDots[0] != null ? 1 : 0)
            + mAdditionalHeartbeatDotGroups.Count
            + mAdditionalLowerHeartbeatDotGroups.Count;

        /// <summary>등록된 모든 벽면 TV 하트비트(중앙 + 12개 호기 상단/하단 TV)가 지정된 점등 인덱스로 동기화되어 있는지 검증합니다.</summary>
        public bool areAllHeartbeatGroupsSyncedTo(int expectedIndex)
        {
            try
            {
                int normIdx = ((expectedIndex % 3) + 3) % 3;
                int checkedGroups = 0;

                if (mCenterHeartbeatDots[0] != null)
                {
                    if (!isHeartbeatGroupMatching(mCenterHeartbeatDots, normIdx)) return false;
                    checkedGroups++;
                }

                if (mU6HeartbeatDots[0] != null)
                {
                    if (!isHeartbeatGroupMatching(mU6HeartbeatDots, normIdx)) return false;
                    checkedGroups++;
                }

                if (mU6LowerHeartbeatDots[0] != null)
                {
                    if (!isHeartbeatGroupMatching(mU6LowerHeartbeatDots, normIdx)) return false;
                    checkedGroups++;
                }

                if (!areHeartbeatGroupListMatching(mAdditionalHeartbeatDotGroups, normIdx, ref checkedGroups)) return false;
                if (!areHeartbeatGroupListMatching(mAdditionalLowerHeartbeatDotGroups, normIdx, ref checkedGroups)) return false;

                return checkedGroups > 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WallDisplaySyncController] areAllHeartbeatGroupsSyncedTo 오류: {ex}");
                return false;
            }
        }

        private bool areHeartbeatGroupListMatching(List<Image[]> groups, int normIdx, ref int checkedGroups)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                Image[] group = groups[i];
                if (group == null || group.Length < 3 || group[0] == null) return false;
                if (!isHeartbeatGroupMatching(group, normIdx)) return false;
                checkedGroups++;
            }
            return true;
        }

        private bool isHeartbeatGroupMatching(Image[] dots, int activeIdx)
        {
            for (int i = 0; i < 3; i++)
            {
                if (dots[i] == null) return false;
                Color expected = i == activeIdx ? ControlRoomTheme.HeartbeatActiveBlue : ControlRoomTheme.HeartbeatIdleDark;
                if (dots[i].color != expected) return false;
            }
            return true;
        }

        private void Awake()
        {
            initializeWallDisplays();
        }

        private void Start()
        {
            bindCanvasWorldCamera();
            refreshSystemClock();
            syncFromActiveCoordinator();
        }

        private void Update()
        {
            try
            {
                if (!mIsInitialized) return;

                float dt = Time.deltaTime;
                mBlinkTimer += Mathf.Max(0f, dt);
                if (mBlinkTimer >= HEARTBEAT_INTERVAL_SEC)
                {
                    int steps = Mathf.FloorToInt(mBlinkTimer / HEARTBEAT_INTERVAL_SEC);
                    mBlinkTimer -= steps * HEARTBEAT_INTERVAL_SEC;
                    mHeartbeatIndex = (mHeartbeatIndex + steps) % 3;
                    mBlinkPhaseOn = !mBlinkPhaseOn;
                }

                refreshSystemClock();
                syncFromActiveCoordinator();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WallDisplaySyncController] Update 오류: {ex}");
            }
        }

        /// <summary>에디터 빌더 및 테스트 코드에서 벽면 TV 캔버스와 에셋 참조를 주입합니다.</summary>
        public void configureReferences(
            Canvas centerWallCanvas,
            Canvas unit06UpperCanvas,
            Canvas unit06LowerCanvas,
            TMP_FontAsset koreanFont,
            ControlRoomSpritePack spritePack)
        {
            try
            {
                mCenterWallCanvas = centerWallCanvas;
                mUnit06UpperCanvas = unit06UpperCanvas;
                mUnit06LowerCanvas = unit06LowerCanvas;
                mKoreanFont = koreanFont;
                mSpritePack = spritePack ?? new ControlRoomSpritePack();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WallDisplaySyncController] configureReferences 오류: {ex}");
            }
        }

        /// <summary>06호기를 제외한 나머지 호기(01~05, 07~12) 상단 TV 하트비트용 캔버스 배열을 주입합니다.</summary>
        public void configureAdditionalUnitCanvases(Canvas[] additionalUnitUpperCanvases)
        {
            try
            {
                mAdditionalUnitUpperCanvases = additionalUnitUpperCanvases;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WallDisplaySyncController] configureAdditionalUnitCanvases 오류: {ex}");
            }
        }

        /// <summary>06호기를 제외한 나머지 호기(01~05, 07~12) 하단 TV 하트비트용 캔버스 배열을 주입합니다.</summary>
        public void configureAdditionalUnitLowerCanvases(Canvas[] additionalUnitLowerCanvases)
        {
            try
            {
                mAdditionalUnitLowerCanvases = additionalUnitLowerCanvases;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WallDisplaySyncController] configureAdditionalUnitLowerCanvases 오류: {ex}");
            }
        }

        /// <summary>벽면 TV(중앙 시계·06호기 상/하단·전체 12호기 상/하단 하트비트) 위에 동적 오버레이 UI를 구축합니다.</summary>
        public void initializeWallDisplays()
        {
            try
            {
                ControlRoomUIFactory.DefaultFont = mKoreanFont;
                if (mSpritePack == null) mSpritePack = new ControlRoomSpritePack();

                buildCenterWallOverlay();
                buildUnit06UpperOverlay();
                buildUnit06LowerOverlay();
                buildAdditionalUnitHeartbeatOverlays();
                buildAdditionalUnitLowerHeartbeatOverlays();

                mLastSecond = -1;
                mIsInitialized = true;
                updateSystemClock(DateTime.Now);
                applyDisplayState(0, 0, 1, false, false, 0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WallDisplaySyncController] initializeWallDisplays 오류: {ex}");
            }
        }

        /// <summary>현재 PC/기기 로컬 시각(DateTime.Now)을 확인해 초 단위로 중앙 메인 벽면 시계를 갱신합니다.</summary>
        public void refreshSystemClock()
        {
            try
            {
                DateTime now = DateTime.Now;
                if (now.Second != mLastSecond)
                {
                    mLastSecond = now.Second;
                    updateSystemClock(now);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WallDisplaySyncController] refreshSystemClock 오류: {ex}");
            }
        }

        /// <summary>지정된 DateTime 기준으로 중앙 메인 벽면 시계(HH:mm:ss / dddd, MMM. dd, yyyy)만 갱신합니다.</summary>
        public void updateSystemClock(DateTime localNow)
        {
            try
            {
                if (mCenterTimeTxt != null)
                {
                    mCenterTimeTxt.text = localNow.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
                }
                if (mCenterDateTxt != null)
                {
                    mCenterDateTxt.text = localNow.ToString("dddd, MMM. dd, yyyy", CultureInfo.InvariantCulture);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WallDisplaySyncController] updateSystemClock 오류: {ex}");
            }
        }

        /// <summary>현재 활성화된 시나리오 1 또는 시나리오 2 코디네이터의 상태를 읽어 벽면 TV 아이콘을 자동 동기화합니다.</summary>
        public void syncFromActiveCoordinator()
        {
            try
            {
                PlantSimulationModel activePlant = null;
                bool isLoca = false;

                // 활성화된 시나리오 2 코디네이터 우선 확인
                var s2List = FindObjectsOfType<Scenario2StationCoordinator>(false);
                for (int i = 0; i < s2List.Length; i++)
                {
                    var s2 = s2List[i];
                    if (s2 != null && s2.gameObject.activeInHierarchy && s2.SharedPlantModel != null)
                    {
                        activePlant = s2.SharedPlantModel;
                        isLoca = activePlant.IsLocaActive;
                        break;
                    }
                }

                // 시나리오 2가 비활성이면 시나리오 1 코디네이터 확인
                if (activePlant == null)
                {
                    var s1List = FindObjectsOfType<OperatorStationCoordinator>(false);
                    for (int i = 0; i < s1List.Length; i++)
                    {
                        var s1 = s1List[i];
                        if (s1 != null && s1.gameObject.activeInHierarchy && s1.SimulationModel != null)
                        {
                            activePlant = s1.SimulationModel;
                            isLoca = activePlant.IsLocaActive;
                            break;
                        }
                    }
                }

                if (activePlant != null)
                {
                    applyDisplayState(
                        activePlant.ActiveAlarmCount,
                        activePlant.ActiveCautionCount,
                        activePlant.NoticeBadgeCount,
                        isLoca,
                        mBlinkPhaseOn,
                        mHeartbeatIndex);
                }
                else
                {
                    applyDisplayState(0, 0, 1, false, false, mHeartbeatIndex);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WallDisplaySyncController] syncFromActiveCoordinator 오류: {ex}");
            }
        }

        /// <summary>전달된 경보/주의/알림 카운트, LOCA 여부, 점멸 위상, 하트비트 인덱스를 벽면 TV에 반영합니다.</summary>
        public void applyDisplayState(int alarmCount, int cautionCount, int noticeCount, bool isLocaActive, bool blinkPhaseOn, int heartbeatIndex)
        {
            try
            {
                mHeartbeatIndex = ((heartbeatIndex % 3) + 3) % 3;
                mBlinkPhaseOn = blinkPhaseOn;

                bool showAlarmOn = alarmCount > 0 && blinkPhaseOn;
                bool showCautionOn = cautionCount > 0 && blinkPhaseOn;
                bool showNoticeOn = noticeCount > 0;
                bool showUpArrows = isLocaActive || alarmCount > 0;

                IsUnit06AlarmIconOn = showAlarmOn;
                IsUnit06CautionIconOn = showCautionOn;
                AreUnit06UpArrowsVisible = showUpArrows;

                string almStr = alarmCount.ToString();
                string cauStr = cautionCount.ToString();

                // 중앙 벽면 TV 상단 녹색 박스 내부 경보(X)·주의(!) 아이콘 및 카운트 동기화
                applyAlarmCautionHeader(
                    mCtrAlarmImg, mCtrCautionImg,
                    mCtrAlarmLeftTxt, mCtrAlarmTopTxt, mCtrAlarmBotTxt,
                    mCtrCautionTopTxt, mCtrCautionBotTxt,
                    showAlarmOn, showCautionOn, almStr, cauStr);

                // 06호기 상단 TV 녹색 박스 내부 경보(X)·주의(!) 아이콘 및 카운트 동기화
                applyAlarmCautionHeader(
                    mU6AlarmImg, mU6CautionImg,
                    mU6AlarmLeftTxt, mU6AlarmTopTxt, mU6AlarmBotTxt,
                    mU6CautionTopTxt, mU6CautionBotTxt,
                    showAlarmOn, showCautionOn, almStr, cauStr);

                // 중앙 벽면 TV UNIT 06 행 상승 화살표 3개 (CNV PRESS, CE RAD MON, RCS LEAK RATE)
                for (int i = 0; i < mUnit06UpArrows.Length; i++)
                {
                    if (mUnit06UpArrows[i] != null)
                    {
                        mUnit06UpArrows[i].SetActive(showUpArrows);
                    }
                }

                // 하트비트 3점 갱신 (중앙 상단 + 12개 호기 상단 + 12개 호기 하단 TV 전체 동기화)
                updateHeartbeatArray(mCenterHeartbeatDots, mHeartbeatIndex);
                updateHeartbeatArray(mU6HeartbeatDots, mHeartbeatIndex);
                updateHeartbeatArray(mU6LowerHeartbeatDots, mHeartbeatIndex);
                for (int i = 0; i < mAdditionalHeartbeatDotGroups.Count; i++)
                {
                    updateHeartbeatArray(mAdditionalHeartbeatDotGroups[i], mHeartbeatIndex);
                }
                for (int i = 0; i < mAdditionalLowerHeartbeatDotGroups.Count; i++)
                {
                    updateHeartbeatArray(mAdditionalLowerHeartbeatDotGroups[i], mHeartbeatIndex);
                }

                // 06호기 하단 4분할 TV 우상단 배지 동기화
                for (int i = 0; i < 3; i++)
                {
                    if (mLowerAlarmImgs[i] != null)
                        mLowerAlarmImgs[i].sprite = showAlarmOn ? mSpritePack.alarmOn : mSpritePack.alarmOff;
                    if (mLowerCautionImgs[i] != null)
                        mLowerCautionImgs[i].sprite = showCautionOn ? mSpritePack.cautionOn : mSpritePack.cautionOff;
                    if (mLowerNoticeImgs[i] != null)
                        mLowerNoticeImgs[i].sprite = showNoticeOn ? mSpritePack.noticeOn : mSpritePack.noticeOff;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WallDisplaySyncController] applyDisplayState 오류: {ex}");
            }
        }

        private void applyAlarmCautionHeader(
            Image alarmImg,
            Image cautionImg,
            TextMeshProUGUI alarmLeftTxt,
            TextMeshProUGUI alarmTopTxt,
            TextMeshProUGUI alarmBotTxt,
            TextMeshProUGUI cautionTopTxt,
            TextMeshProUGUI cautionBotTxt,
            bool showAlarmOn,
            bool showCautionOn,
            string almStr,
            string cauStr)
        {
            if (alarmImg != null)
            {
                alarmImg.sprite = showAlarmOn ? mSpritePack.alarmOn : mSpritePack.alarmOff;
                alarmImg.color = Color.white;
            }
            if (cautionImg != null)
            {
                cautionImg.sprite = showCautionOn ? mSpritePack.cautionOn : mSpritePack.cautionOff;
                cautionImg.color = Color.white;
            }

            if (alarmLeftTxt != null) alarmLeftTxt.text = "0";
            if (alarmTopTxt != null) alarmTopTxt.text = almStr;
            if (alarmBotTxt != null) alarmBotTxt.text = almStr;
            if (cautionTopTxt != null) cautionTopTxt.text = cauStr;
            if (cautionBotTxt != null) cautionBotTxt.text = cauStr;
        }

        // =====================================================================
        // 벽면 TV 3종 오버레이 UI 구축
        // =====================================================================
        private void buildCenterWallOverlay()
        {
            if (mCenterWallCanvas == null) return;
            clearChildren(mCenterWallCanvas.transform);

            var root = ControlRoomUIFactory.createPanel(mCenterWallCanvas.transform, "CenterWallRoot", Color.clear);
            ControlRoomUIFactory.setTopLeftRect(root, 0f, 0f, ControlRoomTheme.CanvasWidth, ControlRoomTheme.CanvasHeight);

            // 1. 시계/날짜 배경 마스크 (#414141) 및 시스템 시각 텍스트 (메인 시계 유지)
            Color clockBg = new Color(65f / 255f, 65f / 255f, 65f / 255f, 1f);
            var clockMask = ControlRoomUIFactory.createPanel(root, "ClockMask", clockBg);
            ControlRoomUIFactory.setTopLeftRect(clockMask, 666f, 206f, 588f, 204f);

            mCenterTimeTxt = ControlRoomUIFactory.createLabel(clockMask, "CenterTime", "00:00:00", 112f, Color.white, TextAlignmentOptions.Center, FontStyles.Normal);
            mCenterTimeTxt.enableWordWrapping = false;
            mCenterTimeTxt.overflowMode = TextOverflowModes.Overflow;
            ControlRoomUIFactory.setTopLeftRect(mCenterTimeTxt.rectTransform, 0f, 8f, 588f, 128f);

            mCenterDateTxt = ControlRoomUIFactory.createLabel(clockMask, "CenterDate", "Monday, Jan. 01, 0001", 33f, Color.white, TextAlignmentOptions.Center, FontStyles.Normal);
            mCenterDateTxt.enableWordWrapping = false;
            mCenterDateTxt.overflowMode = TextOverflowModes.Overflow;
            ControlRoomUIFactory.setTopLeftRect(mCenterDateTxt.rectTransform, 0f, 142f, 588f, 52f);

            // 2. 상단 녹색 테두리(x=657..660, 1263..1266, y=0..3, 124..126) 안쪽 전체를 #1B1B1B로 채워 잘림 방지
            Color boxBg = new Color(27f / 255f, 27f / 255f, 27f / 255f, 1f);
            var greenFill = ControlRoomUIFactory.createPanel(root, "CenterGreenBoxFill", boxBg);
            ControlRoomUIFactory.setTopLeftRect(greenFill, 661f, 4f, 602f, 120f);

            // 좌측 3점 하트비트 (원본 텍스처 실측 좌표 기준)
            float[] ctrDotYs = { 10f, 46f, 82f };
            for (int i = 0; i < 3; i++)
            {
                var dotRt = ControlRoomUIFactory.createPanel(greenFill, $"HbDot_{i}", ControlRoomTheme.HeartbeatIdleDark, mSpritePack.circleDot);
                ControlRoomUIFactory.setTopLeftRect(dotRt, 20f, ctrDotYs[i], 27f, 27f);
                var img = dotRt.GetComponent<Image>();
                img.preserveAspect = true;
                mCenterHeartbeatDots[i] = img;
            }

            // 녹색 박스 내부 경보(X)·주의(!) 아이콘 및 카운트 (원본 실측 좌표 그대로 배치)
            mCtrAlarmLeftTxt = createCounterLabel(greenFill, "AlarmLeft", "0", 160f, 65f, 36f, 42f, 34f);

            var ctrAlmRt = ControlRoomUIFactory.createPanel(greenFill, "AlarmIcon", Color.white, mSpritePack.alarmOff);
            ControlRoomUIFactory.setTopLeftRect(ctrAlmRt, 192f, 10f, 100f, 100f);
            mCtrAlarmImg = ctrAlmRt.GetComponent<Image>();
            mCtrAlarmImg.preserveAspect = true;

            mCtrAlarmTopTxt = createCounterLabel(greenFill, "AlarmTop", "0", 290f, 6f, 36f, 42f, 34f);
            mCtrAlarmBotTxt = createCounterLabel(greenFill, "AlarmBot", "0", 290f, 65f, 36f, 42f, 34f);

            var ctrCauRt = ControlRoomUIFactory.createPanel(greenFill, "CautionIcon", Color.white, mSpritePack.cautionOff);
            ControlRoomUIFactory.setTopLeftRect(ctrCauRt, 366f, 10f, 100f, 100f);
            mCtrCautionImg = ctrCauRt.GetComponent<Image>();
            mCtrCautionImg.preserveAspect = true;

            mCtrCautionTopTxt = createCounterLabel(greenFill, "CautionTop", "0", 464f, 6f, 36f, 42f, 34f);
            mCtrCautionBotTxt = createCounterLabel(greenFill, "CautionBot", "0", 464f, 65f, 36f, 42f, 34f);

            // 3. 좌측 UNIT 06 행 상승 화살표 3개 (CNV PRESS 하단, CE RAD MON 상단, RCS LEAK RATE 상단)
            var arrowPositions = new Vector2[]
            {
                new Vector2(404f, 908f), // Col 5 하단: CNV PRESS
                new Vector2(483f, 862f), // Col 6 상단: CE RAD MON
                new Vector2(562f, 862f)  // Col 7 상단: RCS LEAK RATE
            };
            for (int i = 0; i < arrowPositions.Length; i++)
            {
                var badge = createUpArrowBadge(root, $"Unit06_UpArrow_{i}", arrowPositions[i].x, arrowPositions[i].y, 68f, 40f);
                badge.SetActive(false);
                mUnit06UpArrows[i] = badge;
            }
        }

        private GameObject createUpArrowBadge(RectTransform parent, string name, float x, float y, float w, float h)
        {
            var card = ControlRoomUIFactory.createBorderedBox(parent, name, new Color(0.84f, 0.86f, 0.90f, 1f), new Color(0.24f, 0.27f, 0.32f, 1f), 1.5f);
            ControlRoomUIFactory.setTopLeftRect(card, x, y, w, h);
            if (mSpritePack.upBadge != null)
            {
                var ico = ControlRoomUIFactory.createPanel(card.Find("Inner"), "UpSprite", Color.white, mSpritePack.upBadge);
                ico.GetComponent<Image>().preserveAspect = true;
                ControlRoomUIFactory.setOffsets(ico, 4f, 2f, -4f, -2f);
            }
            else
            {
                var lbl = ControlRoomUIFactory.createLabel(card.Find("Inner"), "UpGlyph", "▲", 24f, new Color(0.10f, 0.14f, 0.20f, 1f), TextAlignmentOptions.Center, FontStyles.Bold);
                ControlRoomUIFactory.setOffsets(lbl.rectTransform, 0f, 0f, 0f, 0f);
            }
            return card.gameObject;
        }

        private void buildUnit06UpperOverlay()
        {
            if (mUnit06UpperCanvas == null) return;
            clearChildren(mUnit06UpperCanvas.transform);

            var root = ControlRoomUIFactory.createPanel(mUnit06UpperCanvas.transform, "U6UpperRoot", Color.clear);
            ControlRoomUIFactory.setTopLeftRect(root, 0f, 0f, ControlRoomTheme.CanvasWidth, ControlRoomTheme.CanvasHeight);

            Color boxBg = new Color(27f / 255f, 27f / 255f, 27f / 255f, 1f);

            // 1. 좌측 상단 3점 하트비트
            var hbMask = ControlRoomUIFactory.createPanel(root, "U6HbMask", boxBg);
            ControlRoomUIFactory.setTopLeftRect(hbMask, 8f, 6f, 44f, 134f);
            float[] u6DotYs = { 8f, 52f, 96f };
            for (int i = 0; i < 3; i++)
            {
                var dotRt = ControlRoomUIFactory.createPanel(hbMask, $"HbDot_{i}", ControlRoomTheme.HeartbeatIdleDark, mSpritePack.circleDot);
                ControlRoomUIFactory.setTopLeftRect(dotRt, 6f, u6DotYs[i], 30f, 30f);
                var img = dotRt.GetComponent<Image>();
                img.preserveAspect = true;
                mU6HeartbeatDots[i] = img;
            }

            // 2. 상단 녹색 테두리(x=591..593, 1246..1248, y=0..3, 144..146) 안쪽 전체를 #1B1B1B 패널로 채워 잘림 방지
            var greenFill = ControlRoomUIFactory.createPanel(root, "U6GreenBoxFill", boxBg);
            ControlRoomUIFactory.setTopLeftRect(greenFill, 594f, 4f, 652f, 140f);

            // 좌측 UNIT 06 타이틀 라벨 (원본 실측 위치 그대로 재구성)
            var unitTitle = ControlRoomUIFactory.createLabel(greenFill, "UnitTitle", "UNIT", 24f, Color.white, TextAlignmentOptions.Center, FontStyles.Normal);
            unitTitle.enableWordWrapping = false;
            unitTitle.overflowMode = TextOverflowModes.Overflow;
            ControlRoomUIFactory.setTopLeftRect(unitTitle.rectTransform, 110f, 20f, 116f, 30f);

            var unitNum = ControlRoomUIFactory.createLabel(greenFill, "UnitNumber", "06", 56f, Color.white, TextAlignmentOptions.Center, FontStyles.Normal);
            unitNum.enableWordWrapping = false;
            unitNum.overflowMode = TextOverflowModes.Overflow;
            ControlRoomUIFactory.setTopLeftRect(unitNum.rectTransform, 110f, 50f, 116f, 68f);

            // 우측 경보(X)·주의(!) 아이콘 및 카운트
            mU6AlarmLeftTxt = createCounterLabel(greenFill, "AlarmLeft", "0", 272f, 86f, 36f, 44f, 36f);

            var almRt = ControlRoomUIFactory.createPanel(greenFill, "AlarmIcon", Color.white, mSpritePack.alarmOff);
            ControlRoomUIFactory.setTopLeftRect(almRt, 306f, 20f, 100f, 100f);
            mU6AlarmImg = almRt.GetComponent<Image>();
            mU6AlarmImg.preserveAspect = true;

            mU6AlarmTopTxt = createCounterLabel(greenFill, "AlarmTop", "0", 404f, 6f, 36f, 44f, 36f);
            mU6AlarmBotTxt = createCounterLabel(greenFill, "AlarmBot", "0", 404f, 86f, 36f, 44f, 36f);

            var cauRt = ControlRoomUIFactory.createPanel(greenFill, "CautionIcon", Color.white, mSpritePack.cautionOff);
            ControlRoomUIFactory.setTopLeftRect(cauRt, 479f, 20f, 100f, 100f);
            mU6CautionImg = cauRt.GetComponent<Image>();
            mU6CautionImg.preserveAspect = true;

            mU6CautionTopTxt = createCounterLabel(greenFill, "CautionTop", "0", 577f, 6f, 36f, 44f, 36f);
            mU6CautionBotTxt = createCounterLabel(greenFill, "CautionBot", "0", 577f, 86f, 36f, 44f, 36f);
        }

        private TextMeshProUGUI createCounterLabel(RectTransform parent, string name, string initial, float x, float y, float w = 40f, float h = 44f, float fontSize = 38f)
        {
            var lbl = ControlRoomUIFactory.createLabel(parent, name, initial, fontSize, Color.white, TextAlignmentOptions.Center, FontStyles.Normal);
            lbl.enableWordWrapping = false;
            lbl.overflowMode = TextOverflowModes.Overflow;
            ControlRoomUIFactory.setTopLeftRect(lbl.rectTransform, x, y, w, h);
            return lbl;
        }

        private void buildUnit06LowerOverlay()
        {
            if (mUnit06LowerCanvas == null) return;
            clearChildren(mUnit06LowerCanvas.transform);

            var root = ControlRoomUIFactory.createPanel(mUnit06LowerCanvas.transform, "U6LowerRoot", Color.clear);
            ControlRoomUIFactory.setTopLeftRect(root, 0f, 0f, ControlRoomTheme.CanvasWidth, ControlRoomTheme.CanvasHeight);

            // 1. 좌측 상단 3점 하트비트 (2048x1152 -> 1920x1080 환산 실측 좌표, 우측 녹색 MET 카드 미침범)
            buildLowerHeartbeatMask(root, "U6LowerHbMask", mU6LowerHeartbeatDots);

            // 2. SCR_Wall_Unit06_Lower.png의 E2 서브 화면 3개 우상단 경보/주의/알림 배지 동기화 (하단 작은 시계는 제거)
            var badgeBoxes = new Rect[]
            {
                new Rect(1762f, 4f, 146f, 32f),
                new Rect(816f, 542f, 140f, 30f),
                new Rect(1784f, 542f, 130f, 32f)
            };
            Color chromeMaskColor = new Color(100f / 255f, 100f / 255f, 100f / 255f, 1f);
            for (int i = 0; i < badgeBoxes.Length; i++)
            {
                Rect r = badgeBoxes[i];
                var mask = ControlRoomUIFactory.createPanel(root, $"SubBadgesMask_{i}", chromeMaskColor);
                ControlRoomUIFactory.setTopLeftRect(mask, r.x, r.y, r.width, r.height);

                float icoSize = Mathf.Max(18f, r.height - 6f);
                float gap = (r.width - icoSize * 3f) / 4f;
                float iy = (r.height - icoSize) * 0.5f;

                mLowerAlarmImgs[i] = createSubIcon(mask, "Alm", gap, iy, icoSize, mSpritePack.alarmOff);
                mLowerCautionImgs[i] = createSubIcon(mask, "Cau", gap * 2f + icoSize, iy, icoSize, mSpritePack.cautionOff);
                mLowerNoticeImgs[i] = createSubIcon(mask, "Ntc", gap * 3f + icoSize * 2f, iy, icoSize, mSpritePack.noticeOn);
            }
        }

        private Image createSubIcon(RectTransform parent, string name, float x, float y, float size, Sprite initial)
        {
            var rt = ControlRoomUIFactory.createPanel(parent, name, Color.white, initial);
            ControlRoomUIFactory.setTopLeftRect(rt, x, y, size, size);
            var img = rt.GetComponent<Image>();
            img.preserveAspect = true;
            return img;
        }

        private void buildAdditionalUnitHeartbeatOverlays()
        {
            mAdditionalHeartbeatDotGroups.Clear();
            if (mAdditionalUnitUpperCanvases == null) return;

            Color boxBg = new Color(27f / 255f, 27f / 255f, 27f / 255f, 1f);
            float[] unitDotYs = { 8f, 52f, 96f };

            for (int c = 0; c < mAdditionalUnitUpperCanvases.Length; c++)
            {
                Canvas unitCanvas = mAdditionalUnitUpperCanvases[c];
                if (unitCanvas == null) continue;

                clearChildren(unitCanvas.transform);

                var root = ControlRoomUIFactory.createPanel(unitCanvas.transform, "UnitHbRoot", Color.clear);
                ControlRoomUIFactory.setTopLeftRect(root, 0f, 0f, ControlRoomTheme.CanvasWidth, ControlRoomTheme.CanvasHeight);

                // 모든 12개 호기 상단 TV 좌측 상단 동일 위치(x=8, y=6, w=44, h=134)에 3점 하트비트 오버레이 배치
                var hbMask = ControlRoomUIFactory.createPanel(root, "UnitHbMask", boxBg);
                ControlRoomUIFactory.setTopLeftRect(hbMask, 8f, 6f, 44f, 134f);

                var dots = new Image[3];
                for (int i = 0; i < 3; i++)
                {
                    var dotRt = ControlRoomUIFactory.createPanel(hbMask, $"HbDot_{i}", ControlRoomTheme.HeartbeatIdleDark, mSpritePack.circleDot);
                    ControlRoomUIFactory.setTopLeftRect(dotRt, 6f, unitDotYs[i], 30f, 30f);
                    var img = dotRt.GetComponent<Image>();
                    img.preserveAspect = true;
                    dots[i] = img;
                }
                mAdditionalHeartbeatDotGroups.Add(dots);
            }
        }

        private void buildAdditionalUnitLowerHeartbeatOverlays()
        {
            mAdditionalLowerHeartbeatDotGroups.Clear();
            if (mAdditionalUnitLowerCanvases == null) return;

            for (int c = 0; c < mAdditionalUnitLowerCanvases.Length; c++)
            {
                Canvas unitLowerCanvas = mAdditionalUnitLowerCanvases[c];
                if (unitLowerCanvas == null) continue;

                clearChildren(unitLowerCanvas.transform);

                var root = ControlRoomUIFactory.createPanel(unitLowerCanvas.transform, "UnitLowerHbRoot", Color.clear);
                ControlRoomUIFactory.setTopLeftRect(root, 0f, 0f, ControlRoomTheme.CanvasWidth, ControlRoomTheme.CanvasHeight);

                var dots = new Image[3];
                buildLowerHeartbeatMask(root, "UnitLowerHbMask", dots);
                mAdditionalLowerHeartbeatDotGroups.Add(dots);
            }
        }

        /// <summary>각 호기 하단 TV 좌측 상단의 3점 하트비트 마스크를 공통 규격(x=0, y=8, w=26, h=82)으로 생성합니다.</summary>
        private void buildLowerHeartbeatMask(RectTransform root, string maskName, Image[] outDots)
        {
            Color boxBg = new Color(27f / 255f, 27f / 255f, 27f / 255f, 1f);
            var hbMask = ControlRoomUIFactory.createPanel(root, maskName, boxBg);
            ControlRoomUIFactory.setTopLeftRect(hbMask, 0f, 8f, 26f, 82f);

            float[] lowerDotYs = { 5f, 32f, 59f };
            for (int i = 0; i < 3; i++)
            {
                var dotRt = ControlRoomUIFactory.createPanel(hbMask, $"HbDot_{i}", ControlRoomTheme.HeartbeatIdleDark, mSpritePack.circleDot);
                ControlRoomUIFactory.setTopLeftRect(dotRt, 3f, lowerDotYs[i], 18f, 18f);
                var img = dotRt.GetComponent<Image>();
                img.preserveAspect = true;
                outDots[i] = img;
            }
        }

        private void updateHeartbeatArray(Image[] dots, int activeIndex)
        {
            for (int i = 0; i < dots.Length; i++)
            {
                if (dots[i] != null)
                {
                    dots[i].color = i == activeIndex ? ControlRoomTheme.HeartbeatActiveBlue : ControlRoomTheme.HeartbeatIdleDark;
                }
            }
        }

        private void bindCanvasWorldCamera()
        {
            try
            {
                Camera targetCam = Camera.main ?? FindObjectOfType<Camera>();
                if (targetCam == null) return;

                if (mCenterWallCanvas != null) mCenterWallCanvas.worldCamera = targetCam;
                if (mUnit06UpperCanvas != null) mUnit06UpperCanvas.worldCamera = targetCam;
                if (mUnit06LowerCanvas != null) mUnit06LowerCanvas.worldCamera = targetCam;
                bindCanvasArrayWorldCamera(mAdditionalUnitUpperCanvases, targetCam);
                bindCanvasArrayWorldCamera(mAdditionalUnitLowerCanvases, targetCam);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WallDisplaySyncController] bindCanvasWorldCamera 경고: {ex.Message}");
            }
        }

        private void bindCanvasArrayWorldCamera(Canvas[] canvases, Camera targetCam)
        {
            if (canvases == null) return;
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i] != null)
                {
                    canvases[i].worldCamera = targetCam;
                }
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
    }
}
