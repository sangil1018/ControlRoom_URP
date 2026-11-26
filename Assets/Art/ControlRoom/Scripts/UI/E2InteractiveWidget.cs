using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ControlRoom.UI
{
    /// <summary>
    /// NuScale E2 시뮬레이터 고유 호버(살짝 진해짐) + 의도된 단계에서만 작동하는 클릭(#00A5FF 파란 점등 + 액션 실행) 컴포넌트입니다.
    /// 비의도 컨트롤은 호버 반응만 수행하고 클릭 시 아무 동작도 하지 않습니다.
    /// Unity 프리팹 직렬화(MonoScript GUID 매칭)를 위해 클래스명과 동일한 개별 파일로 관리합니다.
    /// </summary>
    public class E2InteractiveWidget : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        private Image mTargetImage;
        private Color mNormalColor;
        private Color mSelectedColor = ControlRoomTheme.AccentBlue;
        private Func<bool> mIsSelectedProvider;
        private Func<bool> mIsIntendedActionProvider;
        private UnityAction mOnIntendedClick;
        private GameObject mTooltipObject;
        private bool mPlayClickSound = true;

        /// <summary>현재 포인터가 위젯 위에 올라와 있는지 여부</summary>
        public bool IsHovered { get; private set; }

        /// <summary>의도된 컨트롤이 현재 눌려 #00A5FF로 점등 중인지 여부</summary>
        public bool IsPressedBlue { get; private set; }

        /// <summary>위젯 색상, 선택 조건, 의도된 단계 조건, 클릭 콜백을 초기화합니다.</summary>
        public void configure(
            Image targetImage,
            Color normalColor,
            Color selectedColor,
            Func<bool> isSelectedProvider,
            Func<bool> isIntendedActionProvider,
            UnityAction onIntendedClick,
            GameObject tooltipObject = null,
            bool playClickSound = true)
        {
            try
            {
                mTargetImage = targetImage;
                mNormalColor = normalColor;
                mSelectedColor = selectedColor;
                mIsSelectedProvider = isSelectedProvider;
                mIsIntendedActionProvider = isIntendedActionProvider;
                mOnIntendedClick = onIntendedClick;
                mTooltipObject = tooltipObject;
                mPlayClickSound = playClickSound;
                if (mTooltipObject != null) mTooltipObject.SetActive(false);
                refreshVisualColor();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[E2InteractiveWidget] '{name}' configure 오류: {ex}");
            }
        }

        /// <summary>기본 배경 색상을 동적으로 갱신하고 시각 상태를 다시 적용합니다.</summary>
        public void setBaseColor(Color normalColor)
        {
            try
            {
                mNormalColor = normalColor;
                refreshVisualColor();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[E2InteractiveWidget] '{name}' setBaseColor 오류: {ex}");
            }
        }

        /// <summary>선택·클릭·호버 상태 우선순위에 따라 대상 Image 색상을 갱신합니다.</summary>
        public void refreshVisualColor()
        {
            try
            {
                if (mTargetImage == null) return;
                bool selected = mIsSelectedProvider != null && mIsSelectedProvider();
                Color baseCol = selected ? mSelectedColor : mNormalColor;
                if (IsPressedBlue)
                {
                    mTargetImage.color = ControlRoomTheme.ClickPressBlue;
                }
                else if (IsHovered)
                {
                    mTargetImage.color = ControlRoomTheme.darkenForHover(baseCol);
                }
                else
                {
                    mTargetImage.color = baseCol;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[E2InteractiveWidget] '{name}' refreshVisualColor 오류: {ex}");
            }
        }

        /// <summary>포인터 진입 시 툴팁을 표시하고 호버 진하게 효과를 적용합니다.</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            try
            {
                IsHovered = true;
                if (mTooltipObject != null) mTooltipObject.SetActive(true);
                refreshVisualColor();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[E2InteractiveWidget] '{name}' OnPointerEnter 오류: {ex}");
            }
        }

        /// <summary>포인터 이탈 시 툴팁과 호버/누름 시각 상태를 복원합니다.</summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            try
            {
                IsHovered = false;
                IsPressedBlue = false;
                if (mTooltipObject != null) mTooltipObject.SetActive(false);
                refreshVisualColor();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[E2InteractiveWidget] '{name}' OnPointerExit 오류: {ex}");
            }
        }

        /// <summary>의도된 현재 진행 단계의 컨트롤일 때만 클릭 블루(#00A5FF)를 표시합니다.</summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            try
            {
                if (isIntendedNow())
                {
                    IsPressedBlue = true;
                    refreshVisualColor();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[E2InteractiveWidget] '{name}' OnPointerDown 오류: {ex}");
            }
        }

        /// <summary>포인터를 떼면 #00A5FF 누름 강조를 해제합니다.</summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            try
            {
                IsPressedBlue = false;
                refreshVisualColor();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[E2InteractiveWidget] '{name}' OnPointerUp 오류: {ex}");
            }
        }

        /// <summary>의도된 단계일 때만 클릭 사운드를 재생하고 콜백을 실행합니다.</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            try
            {
                if (!isIntendedNow()) return;

                IsPressedBlue = false;
                if (mTooltipObject != null) mTooltipObject.SetActive(false);
                if (mPlayClickSound) ControlRoomUIFactory.OnAnyClickSound?.Invoke();
                mOnIntendedClick?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[E2InteractiveWidget] '{name}' 클릭 오류: {ex}");
            }
        }

        private bool isIntendedNow() => mOnIntendedClick != null && (mIsIntendedActionProvider == null || mIsIntendedActionProvider());

        /// <summary>단위 테스트용 포인터 호버 진입 시뮬레이션 헬퍼</summary>
        public void simulateHoverEnter() => OnPointerEnter(null);

        /// <summary>단위 테스트용 포인터 호버 이탈 시뮬레이션 헬퍼</summary>
        public void simulateHoverExit() => OnPointerExit(null);

        /// <summary>단위 테스트용 포인터 다운 시뮬레이션 헬퍼</summary>
        public void simulatePointerDown() => OnPointerDown(null);

        /// <summary>단위 테스트용 결정적 클릭 시퀀스(다운-업-클릭) 시뮬레이션 헬퍼</summary>
        public void simulateClick()
        {
            OnPointerDown(null);
            OnPointerUp(null);
            OnPointerClick(null);
        }
    }
}
