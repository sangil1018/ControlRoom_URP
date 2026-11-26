using UnityEngine;

namespace ControlRoom.UI
{
    /// <summary>
    /// NuScale E2 시뮬레이터 실사(Capture[00..49] 및 교육자료 p.65·p.67·p.70·p.74) 기준 색상·상호작용·One Dark Pro CSS 변수 호환 테마입니다.
    /// Linear URP + Global Volume 환경에서 원본 캡처 화면과 동일한 명도·중성 회색·호버/클릭 컬러를 유지하도록 보정되어 있습니다.
    /// </summary>
    public static class ControlRoomTheme
    {
        // =====================================================================
        // 기준 해상도 규격 (16:9 World-Space UI Canvas 표준)
        // =====================================================================
        public const float CanvasWidth = 1920f;
        public const float CanvasHeight = 1080f;

        // =====================================================================
        // NuScale E2 원본 팔레트 (Capture & 교육자료 실측값 기반)
        // =====================================================================
        /// <summary>상/하단 크롬 바 배경 (#646464)</summary>
        public static readonly Color BgChrome = hex("#646464");

        /// <summary>Process Library / Instructor 빈 작업 영역 중간 회색 (#515151)</summary>
        public static readonly Color BgWorkspaceEmpty = hex("#515151");

        /// <summary>서브 네비게이션 바 (#5A5D61)</summary>
        public static readonly Color BgSubBar = hex("#5A5D61");

        /// <summary>주 패널 및 차트 영역 배경 (#414141)</summary>
        public static readonly Color BgPrimary = hex("#414141");

        /// <summary>좌/우 사이드 패널 및 어두운 도면 패널 (#2F2F2F)</summary>
        public static readonly Color BgDarkPane = hex("#2F2F2F");

        /// <summary>가장 어두운 입력창·배경 (#26282B)</summary>
        public static readonly Color BgDarkest = hex("#26282B");

        /// <summary>리세스드 카드 내부 (#3A3D40)</summary>
        public static readonly Color BgPanel = hex("#3A3D40");

        /// <summary>카드·절차서 본문 행 배경 (#3E4145)</summary>
        public static readonly Color BgCard = hex("#3E4145");

        /// <summary>좌측 목록 기본 행 배경 (#484B50)</summary>
        public static readonly Color BgListItem = hex("#484B50");

        /// <summary>상호작용 버튼 기본 배경 (#565A60)</summary>
        public static readonly Color BgButton = hex("#565A60");

        /// <summary>입력 박스 배경 (#242629)</summary>
        public static readonly Color BgInput = hex("#242629");

        /// <summary>얇은 패널 구분선 및 테두리 (#767A80)</summary>
        public static readonly Color BorderSubtle = hex("#767A80");

        /// <summary>선택된 메뉴·활성 탭 블루 — E2 Azure (#008BE8)</summary>
        public static readonly Color AccentBlue = hex("#008BE8");

        /// <summary>클릭(PointerDown) 순간 점등 블루 (#00A5FF — Capture 실측값)</summary>
        public static readonly Color ClickPressBlue = hex("#00A5FF");

        /// <summary>절차서 NOTE·Active 카드 테두리 블루 (#1DA6FF)</summary>
        public static readonly Color HighlightBlue = hex("#1DA6FF");

        /// <summary>절차서/자동화 활성 상단 탭 녹색 (#3EE890)</summary>
        public static readonly Color TabActiveGreen = hex("#3EE890");

        /// <summary>계측 수치 및 배관 시안 (#5CE6E6)</summary>
        public static readonly Color TelemetryCyan = hex("#5CE6E6");

        /// <summary>계측 수치 표준 그린 (#4CE896)</summary>
        public static readonly Color TelemetryGreen = hex("#4CE896");

        /// <summary>정상 운전 / SFM MET / 체크 그린 (#20E68C)</summary>
        public static readonly Color StatusGreen = hex("#20E68C");

        /// <summary>노심 제어봉 기본 민트-시안 (#3CE8B0)</summary>
        public static readonly Color RodCyan = hex("#3CE8B0");

        /// <summary>노심 제어봉 감발 조절 뱅크 황색 (#EFE85B)</summary>
        public static readonly Color RodYellow = hex("#EFE85B");

        /// <summary>상단 하트비트 활성 점 파란색 (#0090FF)</summary>
        public static readonly Color HeartbeatActiveBlue = hex("#0090FF");

        /// <summary>상단 하트비트 비활성 점 어두운 회색 (#282B30)</summary>
        public static readonly Color HeartbeatIdleDark = hex("#282B30");

        /// <summary>절차서 체크박스 선명한 외곽선 (#E8ECF0)</summary>
        public static readonly Color CheckboxBorder = hex("#E8ECF0");

        /// <summary>부드러운 녹색 강조 (#98C379)</summary>
        public static readonly Color SoftGreen = hex("#98C379");

        /// <summary>주의(Caution) 황색 (#F2CA38)</summary>
        public static readonly Color StatusCaution = hex("#F2CA38");

        /// <summary>경보(Alarm) / 연결 붉은 표시 적색 (#EE404B)</summary>
        public static readonly Color StatusAlarm = hex("#EE404B");

        /// <summary>ACTUATED 주황 강조 (#FF7828)</summary>
        public static readonly Color ActuatedOrange = hex("#FF7828");

        /// <summary>비활성 회색 (#6E7276)</summary>
        public static readonly Color StatusInactive = hex("#6E7276");

        /// <summary>배관 기본 회색선 (#9499A0)</summary>
        public static readonly Color PipeInactive = hex("#9499A0");

        /// <summary>기본 밝은 흰색 텍스트 (#F0F2F4)</summary>
        public static readonly Color TextPrimary = hex("#F0F2F4");

        /// <summary>보조 밝은 회색 텍스트 (#C8CCD2)</summary>
        public static readonly Color TextSecondary = hex("#C8CCD2");

        /// <summary>흐린 텍스트 (#92969B)</summary>
        public static readonly Color TextMuted = hex("#92969B");

        /// <summary>가이드 하이라이트 노란색 (#FFCC33)</summary>
        public static readonly Color GuideHighlight = hex("#FFCC33");

        // =====================================================================
        // 호버 명도 감쇠 계수 (Capture[05], [13], [31] 실측: 호버 시 살짝 어두워짐)
        // =====================================================================
        public const float HoverDarkenFactor = 0.68f;

        // =====================================================================
        // 공통 레이아웃 치수 변수 (e2_chrome_top 56px, e2_chrome_bottom 50px)
        // =====================================================================
        public const float TopBarHeight = 56f;
        public const float BottomBarHeight = 50f;
        public const float ContentAreaHeight = 974f;
        public const float RightDrawerWidth = 310f;

        /// <summary>호버 시 기본 색상을 살짝 진하게 보정한 색상을 반환합니다.</summary>
        public static Color darkenForHover(Color baseColor)
        {
            // 완전 투명 히트박스는 반투명 어두운 하이라이트로 반응
            if (baseColor.a < 0.08f)
            {
                return new Color(0.08f, 0.09f, 0.11f, 0.34f);
            }
            return new Color(
                baseColor.r * HoverDarkenFactor,
                baseColor.g * HoverDarkenFactor,
                baseColor.b * HoverDarkenFactor,
                baseColor.a);
        }

        /// <summary>16진수 색상 문자열을 Unity Color로 변환합니다.</summary>
        public static Color hex(string htmlColor)
        {
            try
            {
                if (ColorUtility.TryParseHtmlString(htmlColor, out Color parsed))
                {
                    return parsed;
                }
            }
            catch
            {
                // 잘못된 색상 문자열 입력 시 기본 회색 반환
            }
            return Color.gray;
        }
    }
}
