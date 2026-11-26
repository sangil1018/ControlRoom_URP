using Common;
using Common.Extension;
using Cysharp.Threading.Tasks;

using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Events;
using System.Threading.Tasks;


public abstract class PhaseProcessor : MonoBehaviour
{
    [Header("[Phase Position]")]
    [SerializeField] protected Transform mStartAnchor = null;
    [Header("[Phase Events]")]
    [SerializeField] protected List<PhaseEventTrigger> mPhaseEvents = null;

    public void Initialized()
    {
        if (mStartAnchor == null)
            mStartAnchor = transform;

        if (mPhaseEvents != null)
            mPhaseEvents.ForEach(e => e.Initiailze());
    }

    public void Deinitialized()
    {
        if (mPhaseEvents != null)
            mPhaseEvents.ForEach(e => e.Deinitiailze());
    }

    public abstract UniTask StartProcessAsync();

    protected T GetEventTrigger<T>(int index) where T : PhaseEventTrigger
    {
        if (mPhaseEvents == null || mPhaseEvents.Count <= index)
        {
            return null;
        }
        return mPhaseEvents[index] as T;
    }

    /// <summary>
    /// 페이즈 이벤트 호출
    /// </summary>
    protected async UniTask<PhaseEventTrigger> StartPhaseEventAsync(int eventIndex, int narration, int triggerSound = 0, CancellationToken cancellationToken = default, UnityAction callback = null)
    {
        DLogger.TLog($"[{eventIndex}]번 페이즈 이벤트 트리거 호출...");

        // 이벤트 핸들링
        var eventTrigger = GetEventTrigger<PhaseEventTrigger>(eventIndex);
        if (eventTrigger == null)
        {
            return null;
        }

        // 이벤트 브리핑 출력
        if (narration > 0)
            ShowNarrationAsync(narration).Forget();

        // 이벤트 트리거 호출
        bool hasTriggered = false;
        eventTrigger.OnActivePhase(() =>
        {
            hasTriggered = true;

            if (callback != null)
                callback.Invoke();
        });

        await UniTask.WaitUntil(() => hasTriggered,
            cancellationToken: cancellationToken);

        // 이벤트 클리어음 출력
        if (SoundManager.Instance != null && triggerSound > 0)
            SoundManager.Instance.PlaySound(triggerSound);

        DLogger.TLog($"[{eventIndex}]번 페이즈 이벤트 트리거 종료...");

        return eventTrigger;
    }

    /// <summary>
    /// 노티 브리핑 출력
    /// </summary>
    /// <param name="sndIndex"></param>
    /// <param name="descIndex"></param>
    /// <returns></returns>
    protected async UniTask ShowNoticAsync(int index)
    {
        var strData = TableManager.Instance.GetData<StringTableData>(index);
        if (strData == null)
        {
            DLogger.Error($"[{index}] 스트링 테이블 데이터를 불러올수 없습니다!");
            return;
        }

        // 노티 팝업
        UIManager.Instance.ShowNarrationNoticPopup(true, strData.text);
        await UniTask.Delay(3000, ignoreTimeScale: true, 
            cancellationToken: this.GetCancellationTokenOnDestroy());
        UIManager.Instance.ShowNarrationNoticPopup(false);
    }

    /// <summary>
    /// 나레이션 브리핑 출력
    /// </summary>
    /// <param name="sndIndex"></param>
    /// <param name="descIndex"></param>
    /// <returns></returns>
    protected async UniTask ShowNarrationAsync(int index)
    {
        var narrationData = TableManager.Instance.GetData<NarrationTableData>(index);
        if (narrationData == null)
        {
            DLogger.Error($"[{index}] 나레이션 테이블 데이터를 불러올수 없습니다!");
            return;
        }

        int narrationDuration = 3000;

        // 나레이션 사운드
        var sndChannel = SoundManager.Instance.PlayNarrationSound(narrationData.sound);
        if (sndChannel != null)
            narrationDuration = sndChannel.GetTimeFromMilliseconds();

        // 나레이션 팝업
        UIManager.Instance.ShowNarrationNoticPopup(true, narrationData.text);
        await UniTask.Delay(narrationDuration + 1000, ignoreTimeScale: true, 
            cancellationToken: this.GetCancellationTokenOnDestroy());
        UIManager.Instance.ShowNarrationNoticPopup(false);
    }

    /// <summary>
    /// 미션 브리핑 출력
    /// </summary>
    /// <param name="sndIndex"></param>
    /// <param name="desc"></param>
    /// <returns></returns>
    protected async UniTask ShowMissionAsync(int index, int number)
    {
        var narrationData = TableManager.Instance.GetData<NarrationTableData>(index);
        if (narrationData == null)
        {
            DLogger.Error($"[{index}] 나레이션 테이블 데이터를 불러올수 없습니다!");
            return;
        }

        int narrationDuration = 3000;

        // 미션 나레이션 사운드 출력
        var sndChannel = SoundManager.Instance.PlayNarrationSound(narrationData.sound);
        if (sndChannel != null)
            narrationDuration = sndChannel.GetTimeFromMilliseconds();

        // 미션 브리핑 팝업 출력
        UIManager.Instance.ShowMissionNoticPopup(true, number);
        await UniTask.Delay(narrationDuration + 1000, ignoreTimeScale: true,
            cancellationToken: this.GetCancellationTokenOnDestroy());
        UIManager.Instance.ShowMissionNoticPopup(false);
    }

    // 시작 좌표 이동
    protected void SetStartPosition() => 
        PlayerController.Instance?.SetPosition(mStartAnchor);

    // 화면 페이드 처리
    protected async UniTask ShowFadeScreenAsync(float duration = 2f, UnityAction callback = null) =>
      await UIFadeScreen.FadeAsync(UIFadeScreen.FadeType.InOut, duration, this.GetCancellationTokenOnDestroy(), callback);

    protected async UniTask ScreenFadeInAsync(UnityAction callback = null) => 
        await UIFadeScreen.FadeAsync(UIFadeScreen.FadeType.In, 1f, this.GetCancellationTokenOnDestroy(), callback);   

    protected async UniTask ScreenFadeOutAsync(UnityAction callback = null) => 
        await UIFadeScreen.FadeAsync(UIFadeScreen.FadeType.Out, 1f, this.GetCancellationTokenOnDestroy(), callback);
}
