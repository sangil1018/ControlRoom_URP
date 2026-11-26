using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ControlRoom.UI
{
    /// <summary>
    /// UGUI Image 선분을 재사용해 다중 시계열 그래프를 경량 렌더링하는 위젯입니다.
    /// Unity 프리팹 직렬화(MonoScript GUID 매칭)를 위해 클래스명과 동일한 개별 파일로 관리합니다.
    /// </summary>
    public class TrendChartWidget : MonoBehaviour
    {
        private RectTransform mPlotArea;
        private float mWidth;
        private float mHeight;
        private float mMinVal;
        private float mMaxVal;
        private readonly List<List<Image>> mSeriesSegments = new List<List<Image>>();

        /// <summary>차트 플롯 영역과 Y축 최소/최대 범위를 설정합니다.</summary>
        public void configure(RectTransform plotArea, float width, float height, float minVal, float maxVal)
        {
            try
            {
                mPlotArea = plotArea;
                mWidth = width;
                mHeight = height;
                mMinVal = minVal;
                mMaxVal = Mathf.Max(minVal + 0.001f, maxVal);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TrendChartWidget] configure 오류: {ex}");
            }
        }

        /// <summary>차트 Y축 최소/최대 값 범위를 동적으로 변경합니다.</summary>
        public void setRange(float minVal, float maxVal)
        {
            try
            {
                mMinVal = minVal;
                mMaxVal = Mathf.Max(minVal + 0.001f, maxVal);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TrendChartWidget] setRange 오류: {ex}");
            }
        }

        /// <summary>지정된 시계열 인덱스의 샘플 데이터를 전체 가로 폭(0..1)에 걸쳐 선분 Image들로 갱신합니다.</summary>
        public void updateSeries(int seriesIndex, IReadOnlyList<float> samples, Color lineColor, float thickness = 2.5f)
        {
            updateSeries(seriesIndex, samples, lineColor, thickness, 0f, 1f);
        }

        /// <summary>지정된 시계열 인덱스의 샘플 데이터를 정규화된 가로 범위(startNormalizedX..endNormalizedX)에 선분 Image들로 갱신합니다.</summary>
        public void updateSeries(int seriesIndex, IReadOnlyList<float> samples, Color lineColor, float thickness, float startNormalizedX, float endNormalizedX)
        {
            try
            {
                if (mPlotArea == null || samples == null || samples.Count < 2) return;

                while (mSeriesSegments.Count <= seriesIndex)
                {
                    mSeriesSegments.Add(new List<Image>());
                }

                var segs = mSeriesSegments[seriesIndex];
                float clampedStart = Mathf.Clamp01(startNormalizedX);
                float clampedEnd = Mathf.Clamp01(endNormalizedX);
                if (clampedEnd <= clampedStart + 0.001f)
                {
                    for (int i = 0; i < segs.Count; i++)
                    {
                        if (segs[i] != null) segs[i].gameObject.SetActive(false);
                    }
                    return;
                }

                int needed = samples.Count - 1;
                while (segs.Count < needed)
                {
                    var img = ControlRoomUIFactory.createPipeLine(mPlotArea, $"S{seriesIndex}_Seg{segs.Count}", Vector2.zero, Vector2.right, thickness, lineColor);
                    segs.Add(img);
                }

                for (int i = 0; i < segs.Count; i++)
                {
                    if (i >= needed)
                    {
                        segs[i].gameObject.SetActive(false);
                        continue;
                    }

                    segs[i].gameObject.SetActive(true);
                    float t0 = Mathf.InverseLerp(mMinVal, mMaxVal, samples[i]);
                    float t1 = Mathf.InverseLerp(mMinVal, mMaxVal, samples[i + 1]);
                    float x0 = Mathf.Lerp(clampedStart, clampedEnd, i / (float)needed) * mWidth;
                    float x1 = Mathf.Lerp(clampedStart, clampedEnd, (i + 1) / (float)needed) * mWidth;
                    float y0 = (1f - Mathf.Clamp01(t0)) * mHeight;
                    float y1 = (1f - Mathf.Clamp01(t1)) * mHeight;

                    Vector2 a = new Vector2(x0, -y0);
                    Vector2 b = new Vector2(x1, -y1);
                    Vector2 diff = b - a;
                    var rt = segs[i].rectTransform;
                    rt.anchoredPosition = (a + b) * 0.5f;
                    rt.sizeDelta = new Vector2(diff.magnitude + thickness * 0.45f, thickness);
                    rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);
                    segs[i].color = lineColor;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TrendChartWidget] updateSeries 오류: {ex}");
            }
        }

        /// <summary>차트에 그려진 모든 시계열 선분을 숨깁니다.</summary>
        public void clearAllSeries()
        {
            try
            {
                for (int s = 0; s < mSeriesSegments.Count; s++)
                {
                    var segs = mSeriesSegments[s];
                    for (int i = 0; i < segs.Count; i++)
                    {
                        if (segs[i] != null) segs[i].gameObject.SetActive(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TrendChartWidget] clearAllSeries 오류: {ex}");
            }
        }
    }
}
