using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Common.Extension;



public class AnimationEventTrigger : ColliderEventTrigger
{
    [Header("[Animation]")]
    [SerializeField] private Animator mAnimator = null;
    [SerializeField] private string mTriggerParamter = string.Empty;

    private void Awake()
    {
        if (mAnimator == null)
            mAnimator = GetComponent<Animator>();
    }

    public override void Initiailze()
    {
        base.Initiailze();

        if (mAnimator != null &&
            mTriggerParamter.IsNullOrEmpty() == false)
        {
            // 애니메이션 초기화
            mAnimator.SetBool(mTriggerParamter, false);
        }
        else
        {
            DLogger.Error("AnimationEventTrigger 트리거 설정 오류로 초기화 실패!");
        }
    }

    public override void OnTriggerEvent()
    {
        if (mHasTriggered == false)
        {
            // 애니메이션 재생
            if (mAnimator != null)
            {
                mAnimator.SetBool(mTriggerParamter, true);
            }

            // 효과음이 붙어 있으면 출력
            var audio = GetComponent<AudioSource>();
            if (audio != null)
            {
                audio.Play();
            }

            base.OnTriggerEvent();         
        }

        Debug.Log("[PlayAnimationTrigger] 트리거 이벤트 호출됨.");
    }
}
