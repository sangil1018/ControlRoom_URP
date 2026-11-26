using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ControlRoom.UI
{
    /// <summary>
    /// 3개 모니터(좌·중·우) 공통 상단 크롬(56px)과 하단 크롬(50px)을 무결점(잘림 없음)으로 구성합니다.
    /// 상단 호기 번호·상태 아이콘 3개 및 3점 하트비트는 3모니터 간 실시간 동기화되며,
    /// 하단 날짜·시간은 실행 기기(에디터 PC / Quest APK)의 실제 로컬 시각(DateTime.Now)을 표시합니다.
    /// </summary>
    public class MonitorCommonChrome
    {
        public RectTransform Root { get; private set; }
        public RectTransform TopBar { get; private set; }
        public RectTransform ContentArea { get; private set; }
        public RectTransform BottomBar { get; private set; }

        public E2InteractiveWidget ProcessLibraryNavWidget { get; private set; }
        public E2InteractiveWidget UnitBoxWidget { get; private set; }
        public E2InteractiveWidget CategoryBoxWidget { get; private set; }
        public E2InteractiveWidget ScreenBoxWidget { get; private set; }
        public E2InteractiveWidget AlarmBadgeWidget { get; private set; }
        public E2InteractiveWidget UnitOverviewNavWidget { get; private set; }
        public E2InteractiveWidget BackNavWidget { get; private set; }
        public E2InteractiveWidget TagToggleNavWidget { get; private set; }

        public string CurrentUnitCode { get; private set; } = "06";
        public int CurrentAlarmCount { get; private set; }
        public int CurrentCautionCount { get; private set; }
        public int CurrentNoticeCount { get; private set; } = 1;
        public int ActiveHeartbeatIndex { get; private set; }
        public string ClockTextValue => mClockTxt != null ? mClockTxt.text : string.Empty;

        private readonly Image[] mHeartbeatDots = new Image[3];
        private TextMeshProUGUI mUnitText;
        private TextMeshProUGUI mCategoryText;
        private RectTransform mCategoryBoxRt;
        private RectTransform mSeparatorRt;
        private TextMeshProUGUI mScreenTitleText;
        private RectTransform mScreenBoxRt;
        private TextMeshProUGUI mAlarmCountTxt;
        private TextMeshProUGUI mAlarmLeftTxt;
        private TextMeshProUGUI mCautionCountTxt;
        private TextMeshProUGUI mNoticeCountTxt;
        private Image mAlarmImg;
        private Image mCautionImg;
        private Image mNoticeImg;
        private TextMeshProUGUI mConnectedToTxt;
        private TextMeshProUGUI mClockTxt;
        private readonly List<E2InteractiveWidget> mAllWidgets = new List<E2InteractiveWidget>();

        public void buildChrome(
            Transform parent,
            bool isMainMonitor,
            ControlRoomSpritePack sprites,
            Func<bool> isProcessLibraryIntended,
            Action onOpenProcessLibrary,
            string connectedEndpoint = "net.tcp://SIMLABHS01:9080/service/tcp")
        {
            try
            {
                mAllWidgets.Clear();
                Root = ControlRoomUIFactory.createPanel(parent, "ScreenRoot", ControlRoomTheme.BgDarkPane);
                ControlRoomUIFactory.setTopLeftRect(Root, 0f, 0f, ControlRoomTheme.CanvasWidth, ControlRoomTheme.CanvasHeight);

                // 콘텐츠 영역 먼저 생성해 상/하단 툴팁이 항상 위에 표시되도록 계층 순서를 맞춥니다.
                ContentArea = ControlRoomUIFactory.createPanel(Root, "ContentArea", Color.clear);
                ControlRoomUIFactory.setTopLeftRect(ContentArea, 0f, ControlRoomTheme.TopBarHeight, ControlRoomTheme.CanvasWidth, ControlRoomTheme.ContentAreaHeight);

                // -----------------------------------------------------------------
                // 상단 크롬 바 (56px — 모든 아이콘·텍스트 상하 여백 확보)
                // -----------------------------------------------------------------
                TopBar = ControlRoomUIFactory.createPanel(Root, "TopBar", Color.white, sprites != null ? sprites.e2ChromeTop : null);
                if (sprites == null || sprites.e2ChromeTop == null) TopBar.GetComponent<Image>().color = ControlRoomTheme.BgChrome;
                ControlRoomUIFactory.setTopLeftRect(TopBar, 0f, 0f, ControlRoomTheme.CanvasWidth, ControlRoomTheme.TopBarHeight);

                // 좌측 끝 하트비트 3점 (일정 간격으로 위->중간->아래 순환 점등)
                float[] dotYs = { 8f, 22f, 36f };
                for (int i = 0; i < 3; i++)
                {
                    var dotRt = ControlRoomUIFactory.createPanel(TopBar, $"HeartbeatDot_{i}", ControlRoomTheme.HeartbeatIdleDark, sprites != null ? sprites.circleDot : null);
                    ControlRoomUIFactory.setTopLeftRect(dotRt, 14f, dotYs[i], 11f, 11f);
                    var dotImg = dotRt.GetComponent<Image>();
                    dotImg.preserveAspect = true;
                    mHeartbeatDots[i] = dotImg;
                }
                updateHeartbeat(0);

                // 좌측 4개 크롬 아이콘(하트비트, [=], <, >) 호버 전용 히트박스
                mAllWidgets.Add(ControlRoomUIFactory.createHoverOnlyZone(TopBar, "ChromeDots", 2f, 2f, 38f, 52f));
                mAllWidgets.Add(ControlRoomUIFactory.createHoverOnlyZone(TopBar, "ChromeList", 44f, 4f, 48f, 48f));
                BackNavWidget = ControlRoomUIFactory.createHoverOnlyZone(TopBar, "ChromeBack", 98f, 4f, 48f, 48f);
                mAllWidgets.Add(BackNavWidget);
                mAllWidgets.Add(ControlRoomUIFactory.createHoverOnlyZone(TopBar, "ChromeFwd", 150f, 4f, 48f, 48f));

                // 브레드크럼: [06] : [Special ▼] > [Screen ▼]
                Color crumbFill = ControlRoomTheme.hex("#686A6E");
                Color crumbBorder = new Color(0.76f, 0.78f, 0.82f, 1f);
                var unitBox = ControlRoomUIFactory.createBorderedBox(TopBar, "UnitBox", crumbFill, crumbBorder, 1.0f);
                ControlRoomUIFactory.setTopLeftRect(unitBox, 214f, 7f, 118f, 42f);
                mUnitText = ControlRoomUIFactory.createLabel(unitBox.Find("Inner"), "Val", "06", 24f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
                mUnitText.enableWordWrapping = false;
                mUnitText.rectTransform.offsetMin = new Vector2(10f, 0f);
                UnitBoxWidget = ControlRoomUIFactory.createHoverOnlyZone(unitBox, "Hit", 0f, 0f, 118f, 42f);
                mAllWidgets.Add(UnitBoxWidget);

                ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(TopBar, "ColonSep", ":", 24f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.Center).rectTransform, 332f, 6f, 18f, 42f);

                mCategoryBoxRt = ControlRoomUIFactory.createBorderedBox(TopBar, "CatBox", crumbFill, crumbBorder, 1.0f);
                ControlRoomUIFactory.setTopLeftRect(mCategoryBoxRt, 350f, 7f, 134f, 42f);
                mCategoryText = ControlRoomUIFactory.createLabel(mCategoryBoxRt.Find("Inner"), "Val", "Special", 21f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
                mCategoryText.enableWordWrapping = false;
                mCategoryText.rectTransform.offsetMin = new Vector2(10f, 0f);
                ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mCategoryBoxRt, "Arr", "▼", 10f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.Center).rectTransform, 110f, 14f, 18f, 16f);
                CategoryBoxWidget = ControlRoomUIFactory.createHoverOnlyZone(mCategoryBoxRt, "Hit", 0f, 0f, 134f, 42f);
                mAllWidgets.Add(CategoryBoxWidget);

                mSeparatorRt = ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(TopBar, "ChevronSep", "›", 30f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.Center).rectTransform, 488f, 4f, 24f, 44f);

                mScreenBoxRt = ControlRoomUIFactory.createBorderedBox(TopBar, "ScreenBox", crumbFill, crumbBorder, 1.0f);
                ControlRoomUIFactory.setTopLeftRect(mScreenBoxRt, 514f, 7f, 228f, 42f);
                mScreenTitleText = ControlRoomUIFactory.createLabel(mScreenBoxRt.Find("Inner"), "Val", "Instructor Station", 21f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
                mScreenTitleText.enableWordWrapping = false;
                mScreenTitleText.rectTransform.offsetMin = new Vector2(10f, 0f);
                ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createLabel(mScreenBoxRt, "Arr", "▼", 10f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.Center).rectTransform, 206f, 14f, 18f, 16f);
                ScreenBoxWidget = ControlRoomUIFactory.createHoverOnlyZone(mScreenBoxRt, "Hit", 0f, 0f, 320f, 42f);
                mAllWidgets.Add(ScreenBoxWidget);

                // 우측 상단 경보 / 주의 / 알림 배지 3개 (상하 여백 확보, 숫자 잘림 방지)
                var almRt = ControlRoomUIFactory.createPanel(TopBar, "AlarmBadge", Color.white, sprites != null ? sprites.alarmOff : null);
                ControlRoomUIFactory.setTopLeftRect(almRt, 1624f, 10f, 36f, 36f);
                mAlarmImg = almRt.GetComponent<Image>();
                mAlarmImg.preserveAspect = true;

                mAlarmLeftTxt = ControlRoomUIFactory.createLabel(TopBar, "AlarmL", "0", 11.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center);
                mAlarmLeftTxt.enableWordWrapping = false;
                mAlarmLeftTxt.overflowMode = TextOverflowModes.Overflow;
                ControlRoomUIFactory.setTopLeftRect(mAlarmLeftTxt.rectTransform, 1604f, 30f, 18f, 18f);

                mAlarmCountTxt = ControlRoomUIFactory.createLabel(TopBar, "AlarmCnt", "0\n0", 11.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center);
                mAlarmCountTxt.enableWordWrapping = false;
                mAlarmCountTxt.overflowMode = TextOverflowModes.Overflow;
                mAlarmCountTxt.lineSpacing = 45f;
                ControlRoomUIFactory.setTopLeftRect(mAlarmCountTxt.rectTransform, 1664f, 6f, 22f, 44f);

                var cauRt = ControlRoomUIFactory.createPanel(TopBar, "CautionBadge", Color.white, sprites != null ? sprites.cautionOff : null);
                ControlRoomUIFactory.setTopLeftRect(cauRt, 1712f, 10f, 36f, 36f);
                mCautionImg = cauRt.GetComponent<Image>();
                mCautionImg.preserveAspect = true;

                mCautionCountTxt = ControlRoomUIFactory.createLabel(TopBar, "CautionCnt", "0\n0", 11.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center);
                mCautionCountTxt.enableWordWrapping = false;
                mCautionCountTxt.overflowMode = TextOverflowModes.Overflow;
                mCautionCountTxt.lineSpacing = 45f;
                ControlRoomUIFactory.setTopLeftRect(mCautionCountTxt.rectTransform, 1752f, 6f, 22f, 44f);

                var ntcRt = ControlRoomUIFactory.createPanel(TopBar, "NoticeBadge", Color.white, sprites != null ? sprites.noticeOn : null);
                ControlRoomUIFactory.setTopLeftRect(ntcRt, 1802f, 10f, 36f, 36f);
                mNoticeImg = ntcRt.GetComponent<Image>();
                mNoticeImg.preserveAspect = true;

                mNoticeCountTxt = ControlRoomUIFactory.createLabel(TopBar, "NoticeCnt", "1", 12.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);
                mNoticeCountTxt.enableWordWrapping = false;
                mNoticeCountTxt.overflowMode = TextOverflowModes.Overflow;
                ControlRoomUIFactory.setTopLeftRect(mNoticeCountTxt.rectTransform, 1842f, 28f, 24f, 20f);

                AlarmBadgeWidget = ControlRoomUIFactory.createHoverOnlyZone(TopBar, "HitBadges", 1602f, 2f, 280f, 52f);
                mAllWidgets.Add(AlarmBadgeWidget);

                // -----------------------------------------------------------------
                // 하단 크롬 바 (50px — 모든 벡터 아이콘·(i)·시계·(1) 상하 여백 확보해 잘림 방지)
                // -----------------------------------------------------------------
                BottomBar = ControlRoomUIFactory.createPanel(Root, "BottomBar", Color.white, sprites != null ? sprites.e2ChromeBottom : null);
                if (sprites == null || sprites.e2ChromeBottom == null) BottomBar.GetComponent<Image>().color = ControlRoomTheme.BgChrome;
                ControlRoomUIFactory.setTopLeftRect(BottomBar, 0f, ControlRoomTheme.CanvasHeight - ControlRoomTheme.BottomBarHeight, ControlRoomTheme.CanvasWidth, ControlRoomTheme.BottomBarHeight);

                Sprite[] bottomIcons = sprites != null
                    ? new[] { sprites.qbTree, sprites.qbUnit, sprites.qb12Unit, sprites.qbNotif, sprites.qbEvent, sprites.qbProc, sprites.qbInst }
                    : new Sprite[7];
                float[] iconXs = { 16f, 78f, 140f, 202f, 264f, 326f, 388f };

                for (int i = 0; i < iconXs.Length; i++)
                {
                    var icoRt = ControlRoomUIFactory.createPanel(BottomBar, $"BottomIcoImg_{i}", Color.white, bottomIcons[i]);
                    ControlRoomUIFactory.setTopLeftRect(icoRt, iconXs[i] + 8f, 8f, 34f, 34f);
                    var icoImg = icoRt.GetComponent<Image>();
                    icoImg.preserveAspect = true;
                    if (bottomIcons[i] == null) icoImg.color = Color.clear;

                    if (isMainMonitor && i == 5)
                    {
                        var tip = ControlRoomUIFactory.createE2Tooltip(BottomBar, "ProcLibTooltip", "Process Library", iconXs[i] - 4f, -34f, 126f, 28f);
                        ProcessLibraryNavWidget = ControlRoomUIFactory.createE2Control(
                            BottomBar,
                            "BtnBottom_ProcessLibrary",
                            iconXs[i] - 2f, 4f, 54f, 42f,
                            Color.clear, ControlRoomTheme.ClickPressBlue,
                            null,
                            () => isProcessLibraryIntended != null && isProcessLibraryIntended(),
                            () => onOpenProcessLibrary?.Invoke(),
                            null, 14f, null, TextAlignmentOptions.Center, tip);
                        mAllWidgets.Add(ProcessLibraryNavWidget);
                    }
                    else
                    {
                        var hoverWidget = ControlRoomUIFactory.createHoverOnlyZone(BottomBar, $"BottomIcon_{i}", iconXs[i] - 2f, 4f, 54f, 42f);
                        if (i == 0) TagToggleNavWidget = hoverWidget;
                        if (i == 1) UnitOverviewNavWidget = hoverWidget;
                        mAllWidgets.Add(hoverWidget);
                    }
                }

                // 세로 구분선 & Connected To 라벨
                ControlRoomUIFactory.setTopLeftRect(ControlRoomUIFactory.createPanel(BottomBar, "BotDivider", ControlRoomTheme.TextSecondary), 446f, 8f, 2f, 34f);
                mConnectedToTxt = ControlRoomUIFactory.createLabel(BottomBar, "ConnTxt", $"Connected To: {connectedEndpoint}", 15f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.MidlineLeft);
                ControlRoomUIFactory.setTopLeftRect(mConnectedToTxt.rectTransform, 462f, 6f, 760f, 38f);

                // (i) 정보 아이콘 (잘림 없는 30x30 중앙 정렬)
                var infoRt = ControlRoomUIFactory.createPanel(BottomBar, "BottomInfoIco", Color.white, sprites != null ? sprites.infoIcon : null);
                ControlRoomUIFactory.setTopLeftRect(infoRt, 1632f, 10f, 30f, 30f);
                var infoImg = infoRt.GetComponent<Image>();
                infoImg.preserveAspect = true;
                if (sprites == null || sprites.infoIcon == null) infoImg.color = Color.clear;
                mAllWidgets.Add(ControlRoomUIFactory.createHoverOnlyZone(BottomBar, "BottomInfoHit", 1626f, 4f, 42f, 42f));

                // 실시간 기기 날짜/시간 (PC 에디터 / Quest APK 로컬 시각)
                mClockTxt = ControlRoomUIFactory.createLabel(BottomBar, "ClockTxt", "", 13.5f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center);
                mClockTxt.enableWordWrapping = false;
                mClockTxt.overflowMode = TextOverflowModes.Overflow;
                ControlRoomUIFactory.setTopLeftRect(mClockTxt.rectTransform, 1670f, 4f, 184f, 42f);
                refreshDeviceClock();

                // 우측 (1) 원형 아이콘 (상하 7px 여백으로 외곽선 잘림 없음)
                var oneRt = ControlRoomUIFactory.createPanel(BottomBar, "BottomOneBadge", Color.white, sprites != null ? sprites.bottomOneBadge : null);
                ControlRoomUIFactory.setTopLeftRect(oneRt, 1864f, 7f, 36f, 36f);
                var oneImg = oneRt.GetComponent<Image>();
                oneImg.preserveAspect = true;
                if (sprites == null || sprites.bottomOneBadge == null) oneImg.color = Color.clear;
                mAllWidgets.Add(ControlRoomUIFactory.createHoverOnlyZone(BottomBar, "BottomOneHit", 1858f, 4f, 48f, 42f));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MonitorCommonChrome] buildChrome 오류: {ex}");
            }
        }

        /// <summary>상단 좌측 3개 하트비트 점 중 지정 인덱스(0~2)만 파란색으로 점등합니다.</summary>
        public void updateHeartbeat(int activeIndex)
        {
            int normalized = ((activeIndex % 3) + 3) % 3;
            ActiveHeartbeatIndex = normalized;
            for (int i = 0; i < mHeartbeatDots.Length; i++)
            {
                if (mHeartbeatDots[i] != null)
                {
                    mHeartbeatDots[i].color = i == normalized ? ControlRoomTheme.HeartbeatActiveBlue : ControlRoomTheme.HeartbeatIdleDark;
                }
            }
        }

        /// <summary>실행 중인 기기(PC / Meta Quest)의 현재 로컬 날짜와 시각으로 하단 시계를 갱신합니다.</summary>
        public void refreshDeviceClock()
        {
            updateDeviceClock(DateTime.Now);
        }

        /// <summary>전달된 DateTime 기준 E2 형식(h:mm:ss tt / M/d/yyyy)으로 하단 시계를 갱신합니다.</summary>
        public void updateDeviceClock(DateTime localNow)
        {
            if (mClockTxt == null) return;
            string timePart = localNow.ToString("h:mm:ss tt", CultureInfo.InvariantCulture);
            string datePart = localNow.ToString("M/d/yyyy", CultureInfo.InvariantCulture);
            mClockTxt.text = $"{timePart}\n{datePart}";
        }

        public void updateHeaderAndClock(string unitCode, string categoryText, string screenTitle, int alarmCount, int cautionCount, int noticeCount, string _unusedClockText, ControlRoomSpritePack sprites)
        {
            CurrentUnitCode = unitCode;
            CurrentAlarmCount = alarmCount;
            CurrentCautionCount = cautionCount;
            CurrentNoticeCount = noticeCount;

            if (mUnitText != null) mUnitText.text = unitCode;
            if (mCategoryText != null) mCategoryText.text = categoryText;
            if (mScreenTitleText != null)
            {
                mScreenTitleText.text = screenTitle;
                if (mScreenBoxRt != null)
                {
                    float w = screenTitle.Length > 40 ? 560f : (screenTitle.Length > 26 ? 470f : (screenTitle.Length > 18 ? 316f : (screenTitle.Length > 14 ? 226f : 196f)));
                    mScreenBoxRt.sizeDelta = new Vector2(w, 42f);
                    var arr = mScreenBoxRt.Find("Arr")?.GetComponent<RectTransform>();
                    if (arr != null) ControlRoomUIFactory.setTopLeftRect(arr, w - 24f, 14f, 18f, 16f);
                    var hit = mScreenBoxRt.Find("Hit")?.GetComponent<RectTransform>();
                    if (hit != null) hit.sizeDelta = new Vector2(w, 42f);
                }
            }

            if (mAlarmLeftTxt != null) mAlarmLeftTxt.text = alarmCount.ToString();
            if (mAlarmCountTxt != null) mAlarmCountTxt.text = $"{alarmCount}\n{alarmCount}";
            if (mCautionCountTxt != null) mCautionCountTxt.text = $"{cautionCount}\n{cautionCount}";
            if (mNoticeCountTxt != null) mNoticeCountTxt.text = noticeCount.ToString();

            if (sprites != null)
            {
                if (mAlarmImg != null) mAlarmImg.sprite = alarmCount > 0 ? sprites.alarmOn : sprites.alarmOff;
                if (mCautionImg != null) mCautionImg.sprite = cautionCount > 0 ? sprites.cautionOn : sprites.cautionOff;
                if (mNoticeImg != null) mNoticeImg.sprite = noticeCount > 0 ? sprites.noticeOn : sprites.noticeOff;
            }

            refreshDeviceClock();
        }

        public void refreshHoverWidgetColors()
        {
            for (int i = 0; i < mAllWidgets.Count; i++)
            {
                if (mAllWidgets[i] != null) mAllWidgets[i].refreshVisualColor();
            }
        }

        /// <summary>시나리오 2 전용: 상단 브레드크럼·뒤로가기·경보창·하단 Unit Overview·Tag 토글 버튼의 클릭 동작을 재바인딩합니다.</summary>
        public void configureScenario2Interactions(
            Func<bool> isUnitIntended, Action onUnitClick,
            Func<bool> isCategoryIntended, Action onCategoryClick,
            Func<bool> isScreenIntended, Action onScreenClick,
            Func<bool> isAlarmIntended, Action onAlarmClick,
            Func<bool> isUnitOverviewIntended, Action onUnitOverviewClick,
            Func<bool> isBackIntended = null, Action onBackClick = null,
            Func<bool> isTagToggleActive = null, Action onTagToggleClick = null)
        {
            try
            {
                rebindHoverWidget(UnitBoxWidget, Color.clear, isUnitIntended, onUnitClick);
                rebindHoverWidget(CategoryBoxWidget, Color.clear, isCategoryIntended, onCategoryClick);
                rebindHoverWidget(ScreenBoxWidget, Color.clear, isScreenIntended, onScreenClick);
                rebindHoverWidget(AlarmBadgeWidget, Color.clear, isAlarmIntended, onAlarmClick);
                rebindHoverWidget(UnitOverviewNavWidget, ControlRoomTheme.ClickPressBlue, isUnitOverviewIntended, onUnitOverviewClick);
                rebindHoverWidget(BackNavWidget, Color.clear, isBackIntended, onBackClick);

                // 하단 Tag Labels 토글 버튼은 모든 시나리오2 P&ID 확인 시 자유 토글이 가능하도록 구성
                if (TagToggleNavWidget != null)
                {
                    var img = TagToggleNavWidget.GetComponent<Image>();
                    if (img != null)
                    {
                        TagToggleNavWidget.configure(
                            img,
                            Color.clear,
                            ControlRoomTheme.AccentBlue,
                            isTagToggleActive,
                            () => onTagToggleClick != null,
                            onTagToggleClick != null ? () => onTagToggleClick() : null);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MonitorCommonChrome] configureScenario2Interactions 오류: {ex}");
            }
        }

        private void rebindHoverWidget(E2InteractiveWidget widget, Color selectedColor, Func<bool> isIntended, Action onClick)
        {
            if (widget == null) return;
            var img = widget.GetComponent<Image>();
            if (img == null) return;
            widget.configure(img, Color.clear, selectedColor, null, isIntended, onClick != null ? () => onClick() : null);
        }
    }
}
