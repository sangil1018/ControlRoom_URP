using Common.Extension;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem.XR;



public class PlayerSitPoseEventTrigger : PhaseEventTrigger
{
    [Header("[Animator]")]
    [SerializeField] private Animator mSeatAnimator = null;
    [SerializeField] private string mSeatStateName = string.Empty;


    public override void Initiailze()
    {
        if (mSeatAnimator != null)
            mSeatAnimator.enabled = false;

        base.Initiailze();
    }

    public override void OnActivePhase(UnityAction callback)
    {
        base.OnActivePhase(callback);

        if (mSeatAnimator != null &&
            mSeatStateName.IsNullOrEmpty() == false)
        {
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetSitPoseOnChair(mSeatAnimator.transform);

                StartCoroutine(PlayAndWait());
            }
        }
    }

    public override void OnTriggerEvent()
    {
        if (mHasTriggered == false)
        {            
            base.OnTriggerEvent();
        }

        Debug.Log("[CameraPoseEventTrigger] 트리거 이벤트 호출됨.");
    }

    private IEnumerator PlayAndWait()
    {
        mSeatAnimator.enabled = true;
        mSeatAnimator.Play(mSeatStateName);

        // 상태 전환이 반영될 때까지 한 프레임 대기
        yield return null;

        // 현재 재생 중인 애니메이션의 길이(초)만큼 대기
        float animLength = mSeatAnimator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(animLength);

        Debug.Log("애니메이션 종료!");
        yield return null;

        mSeatAnimator.enabled = false;

        OnTriggerEvent();       
    }
}
