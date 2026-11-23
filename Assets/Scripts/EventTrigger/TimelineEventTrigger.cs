using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Common.Extension;
using UnityEngine.Playables;



public class TimelineEventTrigger : PhaseEventTrigger
{
    [Header("[Timeline]")]
    [SerializeField] private PlayableDirector mDirector = null;

    private void Awake()
    {
        if (mDirector == null)
            mDirector = GetComponent<PlayableDirector>();
    }

    public override void Initiailze()
    {
        base.Initiailze();

        if (mDirector != null)
        {
            // 재생 종료 이벤트 구독
            mDirector.stopped += OnPlayableDirectorStopped;
            mDirector.Play();
        }
        else
        {
            DLogger.Error("TimelineEventTrigger 트리거 설정 오류로 초기화 실패!");
        }
    }    

    public override void OnTriggerEvent()
    {
        if (mHasTriggered == false)
        {
            // 애니메이션 재생
            if (mDirector != null)
            {
                // 이벤트 구독 해제
                mDirector.stopped -= OnPlayableDirectorStopped;
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

    void OnPlayableDirectorStopped(PlayableDirector director)
    {
        if (director == mDirector)
        {
            Debug.Log("TimelineEventTrigger 타임라인 재생이 완료되었습니다!");

            OnTriggerEvent();
        }
    }
}
