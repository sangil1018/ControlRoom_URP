using Common.Extension;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;



public class CutSceneEventTrigger : PhaseEventTrigger
{
    [Header("[Timeline]")]
    [SerializeField] private PlayableDirector mDirector = null;

    public override void Initiailze()
    {
        base.Initiailze();

        if (mDirector != null)
        {
            mDirector.stopped += OnCutSceneStopped;
        }
        else
        {
            DLogger.Error("CutSceneEventTrigger 트리거 설정 오류로 초기화 실패!");
        }
    }

    public override void Deinitiailze()
    {
        if (mDirector != null)
        {
            mDirector.stopped -= OnCutSceneStopped;
        }

        base.Deinitiailze();
    }

    public override void OnActivePhase(UnityAction callback)
    {
        base.OnActivePhase(callback);

        if (mDirector != null)
        {
            mDirector.Play();
        }            
    }

    private void OnCutSceneStopped(PlayableDirector director)
    {
        Debug.Log("[CutSceneEventTrigger] 트리거 이벤트 호출됨.");

        OnTriggerEvent();
    }
}
