using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ControlRoom.UI
{
    /// <summary>
    /// 3개 모니터가 공유하는 원본 NuScale E2 추출 텍스처·아이콘·계통 심볼 스프라이트 번들입니다.
    /// </summary>
    [Serializable]
    public class ControlRoomSpritePack
    {
        [Header("원본 추출 화면 및 크롬")]
        public Sprite e2ChromeTop;
        public Sprite e2ChromeBottom;
        public Sprite e2LeftUnitOverview;
        public Sprite e2RightSfm;
        public Sprite e2LogoInstructor;

        [Header("상/하단 공통 벡터 아이콘")]
        public Sprite navDots;
        public Sprite menuList;
        public Sprite chevronLeft;
        public Sprite chevronRight;
        public Sprite alarmOff;
        public Sprite alarmOn;
        public Sprite cautionOff;
        public Sprite cautionOn;
        public Sprite noticeOff;
        public Sprite noticeOn;
        public Sprite qbTree;
        public Sprite qbUnit;
        public Sprite qb12Unit;
        public Sprite qbNotif;
        public Sprite qbEvent;
        public Sprite qbProc;
        public Sprite qbInst;
        public Sprite infoIcon;
        public Sprite rodSymbol;
        public Sprite docNote;
        public Sprite bookBlue;
        public Sprite icoCheckStep;
        public Sprite icoNoteDoc;
        public Sprite icoSpeechBubble;
        public Sprite icoBookBlue;
        public Sprite icoGearListBlue;
        public Sprite icoGovCyan;
        public Sprite icoPumpCyan;
        public Sprite icoPumpDark;
        public Sprite circleDot;
        public Sprite bottomOneBadge;

        [Header("계통 심볼")]
        public Sprite valveOpen;
        public Sprite valveClosed;
        public Sprite pumpOn;
        public Sprite pumpOff;
        public Sprite lockClosed;
        public Sprite lockOpen;
        public Sprite upBadge;
        public Sprite downBadge;

        [Header("보조 P&ID 도면 백드롭")]
        public Sprite diagNpmCutaway;
        public Sprite diagFwmsMimic;
        public Sprite diagCePid;
        public Sprite diagCvcHeatingSpray;
        public Sprite diagCvcDemins;
        public Sprite diagCvcRecirc;
        public Sprite diagRccw;
    }

    /// <summary>
    /// 3개 모니터 UI의 모든 패널, 호버/클릭 버튼, 아이콘, 텍스트, 배관, 트렌드 차트를 생성하는 UI 팩토리입니다.
    /// </summary>
    public static class ControlRoomUIFactory
    {
        public static TMP_FontAsset DefaultFont { get; set; }
        public static Action OnAnyClickSound { get; set; }

        public static RectTransform createPanel(Transform parent, string name, Color color, Sprite sprite = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            stretchFull(rt);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.sprite = sprite;
            img.raycastTarget = false;
            return rt;
        }

        public static RectTransform createBorderedBox(Transform parent, string name, Color fillColor, Color borderColor, float borderThickness = 1.5f)
        {
            var outer = createPanel(parent, name, borderColor);
            var inner = createPanel(outer, "Inner", fillColor);
            setOffsets(inner, borderThickness, borderThickness, -borderThickness, -borderThickness);
            return outer;
        }

        public static void stretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        public static void setOffsets(RectTransform rt, float left, float bottom, float right, float top)
        {
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(right, top);
        }

        public static RectTransform setTopLeftRect(RectTransform rt, float x, float yFromTop, float width, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -yFromTop);
            rt.sizeDelta = new Vector2(width, height);
            rt.localScale = Vector3.one;
            return rt;
        }

        public static TextMeshProUGUI createLabel(
            Transform parent,
            string name,
            string text,
            float fontSize = 18f,
            Color? color = null,
            TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft,
            FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            stretchFull(rt);

            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (DefaultFont != null) tmp.font = DefaultFont;
            tmp.text = text ?? string.Empty;
            tmp.fontSize = fontSize;
            tmp.color = color ?? ControlRoomTheme.TextPrimary;
            tmp.alignment = alignment;
            tmp.fontStyle = style;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.raycastTarget = false;
            return tmp;
        }

        /// <summary>NuScale E2 호버 감쇠·의도된 클릭 전용 인터랙티브 영역 생성 (텍스트 포함 옵션)</summary>
        public static E2InteractiveWidget createE2Control(
            Transform parent,
            string name,
            float x,
            float y,
            float w,
            float h,
            Color normalColor,
            Color selectedColor,
            Func<bool> isSelectedProvider,
            Func<bool> isIntendedProvider,
            UnityAction onIntendedClick,
            string labelText = null,
            float fontSize = 16f,
            Color? textColor = null,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center,
            GameObject tooltipObject = null,
            bool bordered = false,
            Color? borderColor = null,
            float borderThickness = 1.5f)
        {
            RectTransform rt;
            Image targetImg;
            if (bordered)
            {
                rt = createBorderedBox(parent, name, normalColor, borderColor ?? ControlRoomTheme.BorderSubtle, borderThickness);
                setTopLeftRect(rt, x, y, w, h);
                targetImg = rt.Find("Inner").GetComponent<Image>();
            }
            else
            {
                rt = createPanel(parent, name, normalColor);
                setTopLeftRect(rt, x, y, w, h);
                targetImg = rt.GetComponent<Image>();
            }
            targetImg.raycastTarget = true;

            if (!string.IsNullOrEmpty(labelText))
            {
                var lbl = createLabel(targetImg.transform, "Label", labelText, fontSize, textColor ?? ControlRoomTheme.TextPrimary, alignment);
                setOffsets(lbl.rectTransform, 6f, 2f, -6f, -2f);
            }

            var widget = rt.gameObject.AddComponent<E2InteractiveWidget>();
            widget.configure(targetImg, normalColor, selectedColor, isSelectedProvider, isIntendedProvider, onIntendedClick, tooltipObject);
            return widget;
        }

        /// <summary>원본 크롬/도면 위의 수동(호버만 반응하고 클릭 시 무동작) 히트박스 배치</summary>
        public static E2InteractiveWidget createHoverOnlyZone(Transform parent, string name, float x, float y, float w, float h, GameObject tooltip = null)
        {
            return createE2Control(parent, name, x, y, w, h, Color.clear, Color.clear, null, () => false, null, null, 14f, null, TextAlignmentOptions.Center, tooltip, false);
        }

        /// <summary>파란 테두리 E2 툴팁 배지 생성 (Process Library / C. Power Reduction 등)</summary>
        public static GameObject createE2Tooltip(Transform parent, string name, string text, float x, float y, float w, float h)
        {
            var tip = createBorderedBox(parent, name, ControlRoomTheme.BgDarkest, ControlRoomTheme.HighlightBlue, 1.5f);
            setTopLeftRect(tip, x, y, w, h);
            var lbl = createLabel(tip.Find("Inner"), "Text", text, 14f, ControlRoomTheme.TextPrimary, TextAlignmentOptions.Center);
            lbl.enableWordWrapping = false;
            tip.gameObject.SetActive(false);
            return tip.gameObject;
        }

        /// <summary>기존 호환 Button 생성 (호버 시 진해지고 클릭 시 #00A5FF 색상 점등 적용)</summary>
        public static Button createButton(
            Transform parent,
            string name,
            string labelText,
            UnityAction onClick,
            Color? bgColor = null,
            Color? textColor = null,
            float fontSize = 16f,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center,
            bool playClickSound = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.localScale = Vector3.one;

            Color norm = bgColor ?? ControlRoomTheme.BgButton;
            var img = go.GetComponent<Image>();
            img.color = norm;
            img.raycastTarget = true;

            var btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(ControlRoomTheme.HoverDarkenFactor, ControlRoomTheme.HoverDarkenFactor, ControlRoomTheme.HoverDarkenFactor, 1f);
            cb.pressedColor = ControlRoomTheme.ClickPressBlue;
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.65f);
            btn.colors = cb;

            if (!string.IsNullOrEmpty(labelText))
            {
                var lbl = createLabel(go.transform, "Label", labelText, fontSize, textColor ?? ControlRoomTheme.TextPrimary, alignment);
                setOffsets(lbl.rectTransform, 6f, 2f, -6f, -2f);
            }

            btn.onClick.AddListener(() =>
            {
                try
                {
                    if (playClickSound) OnAnyClickSound?.Invoke();
                    onClick?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ControlRoomUIFactory] 버튼 '{name}' 클릭 오류: {ex}");
                }
            });

            return btn;
        }

        public static Button createIconButton(
            Transform parent,
            string name,
            Sprite iconSprite,
            Color bgColor,
            Color iconTint,
            UnityAction onClick,
            float padding = 6f)
        {
            var btn = createButton(parent, name, string.Empty, onClick, bgColor);
            var iconRt = createPanel(btn.transform, "Icon", iconTint, iconSprite);
            setOffsets(iconRt, padding, padding, -padding, -padding);
            return btn;
        }

        public static Image createPipeLine(Transform parent, string name, Vector2 p1TopLeft, Vector2 p2TopLeft, float thickness, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);

            Vector2 a = new Vector2(p1TopLeft.x, -p1TopLeft.y);
            Vector2 b = new Vector2(p2TopLeft.x, -p2TopLeft.y);
            Vector2 diff = b - a;
            float len = diff.magnitude + thickness * 0.55f;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

            rt.anchoredPosition = (a + b) * 0.5f;
            rt.sizeDelta = new Vector2(len, thickness);
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
            rt.localScale = Vector3.one;

            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static List<Image> createPipePath(Transform parent, string baseName, float thickness, Color color, params Vector2[] points)
        {
            var list = new List<Image>();
            for (int i = 0; i < points.Length - 1; i++)
            {
                list.Add(createPipeLine(parent, $"{baseName}_Seg{i}", points[i], points[i + 1], thickness, color));
            }
            return list;
        }

        public static Button createValveButton(
            Transform parent,
            string valveId,
            Vector2 centerTopLeft,
            float size,
            Sprite valveSprite,
            bool vertical,
            UnityAction onClick,
            out Image iconImage)
        {
            var btn = createButton(parent, $"Valve_{valveId}", string.Empty, onClick, Color.clear);
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.38f);
            rt.anchoredPosition = new Vector2(centerTopLeft.x, -centerTopLeft.y);
            rt.sizeDelta = new Vector2(size, size);
            if (vertical) rt.localRotation = Quaternion.Euler(0f, 0f, 90f);

            iconImage = btn.GetComponent<Image>();
            iconImage.sprite = valveSprite;
            iconImage.color = Color.white;
            return btn;
        }

        public static Image createStaticValve(RectTransform parent, float cx, float cy, float size, Sprite valveSpr, bool vertical = false)
        {
            var go = createPanel(parent, $"StaticValve_{cx}_{cy}", Color.white, valveSpr);
            go.anchorMin = new Vector2(0f, 1f);
            go.anchorMax = new Vector2(0f, 1f);
            go.pivot = new Vector2(0.5f, 0.38f);
            go.anchoredPosition = new Vector2(cx, -cy);
            go.sizeDelta = new Vector2(size, size);
            if (vertical) go.localRotation = Quaternion.Euler(0f, 0f, 90f);
            return go.GetComponent<Image>();
        }

        public static Image createStaticPump(RectTransform parent, string name, float cx, float cy, float size, Sprite pumpSpr)
        {
            var go = createPanel(parent, name, Color.white, pumpSpr);
            go.anchorMin = new Vector2(0f, 1f);
            go.anchorMax = new Vector2(0f, 1f);
            go.pivot = new Vector2(0.5f, 0.5f);
            go.anchoredPosition = new Vector2(cx, -cy);
            go.sizeDelta = new Vector2(size, size);
            return go.GetComponent<Image>();
        }

        /// <summary>순수 PlotArea 위젯만 생성하거나(추출 도면 오버레이용), 격자 포함 트렌드 차트를 생성합니다.</summary>
        public static TrendChartWidget createTrendChart(
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height,
            float minVal,
            float maxVal,
            int gridCols = 8,
            int gridRows = 5,
            bool drawGrid = true)
        {
            var root = drawGrid
                ? createBorderedBox(parent, name, ControlRoomTheme.BgInput, ControlRoomTheme.BorderSubtle, 1f)
                : createPanel(parent, name, Color.clear);
            setTopLeftRect(root, x, y, width, height);

            float leftMargin = drawGrid ? 50f : 0f;
            float rightMargin = drawGrid ? 14f : 0f;
            float topMargin = drawGrid ? 12f : 0f;
            float botMargin = drawGrid ? 24f : 0f;

            var chartArea = createPanel(root, "PlotArea", drawGrid ? new Color(0.255f, 0.255f, 0.255f, 1f) : Color.clear);
            setOffsets(chartArea, leftMargin, botMargin, -rightMargin, -topMargin);

            float plotW = width - leftMargin - rightMargin;
            float plotH = height - topMargin - botMargin;

            if (drawGrid)
            {
                for (int c = 0; c <= gridCols; c++)
                {
                    float gx = (plotW / gridCols) * c;
                    createPipeLine(chartArea, $"GridV_{c}", new Vector2(gx, 0f), new Vector2(gx, plotH), 1f, new Color(1f, 1f, 1f, 0.16f));
                }
                for (int r = 0; r <= gridRows; r++)
                {
                    float gy = (plotH / gridRows) * r;
                    createPipeLine(chartArea, $"GridH_{r}", new Vector2(0f, gy), new Vector2(plotW, gy), 1f, new Color(1f, 1f, 1f, 0.16f));
                    float val = Mathf.Lerp(maxVal, minVal, r / (float)gridRows);
                    var yLbl = createLabel(root, $"YLabel_{r}", val.ToString("0"), 11f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.MidlineRight);
                    setTopLeftRect(yLbl.rectTransform, leftMargin - 44f, 12f + gy - 9f, 40f, 18f);
                }
                string[] times = { "07:55:29", "07:56:29", "07:57:29", "07:58:29", "07:59:29", "08:00:29" };
                for (int i = 0; i < times.Length; i++)
                {
                    float tx = leftMargin + (plotW / (times.Length - 1)) * i - 34f;
                    var tl = createLabel(root, $"Time_{i}", times[i], 10.5f, ControlRoomTheme.TextSecondary, TextAlignmentOptions.Center);
                    setTopLeftRect(tl.rectTransform, tx, height - 22f, 68f, 18f);
                }
            }

            var widget = root.gameObject.AddComponent<TrendChartWidget>();
            widget.configure(chartArea, plotW, plotH, minVal, maxVal);
            return widget;
        }
    }
}
