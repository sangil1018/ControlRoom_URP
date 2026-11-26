using Common;
using Common.Extension;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using static SoundManager;
using static UIFadeScreen;



public class IntroProcessor : PhaseProcessor
{
    /// <summary>
    /// 원전 통제실 위기대응 교육 브리핑(인트로) 페이즈
    /// </summary>
    /// <returns></returns>
    public override async UniTask StartProcessAsync()
    {
        DLogger.TLog("[원전 통제실 위기대응 교육 브리핑 브리핑] 프로세스 시작...");
       
        if (PlayerController.Instance != null)
            PlayerController.Instance.SetArmSwingerPaused(true);

        // 시작 좌표 이동
        SetStartPosition();

        // BGM 사운드
        SoundManager.Instance.PlayBgm();

        await ScreenFadeOutAsync();

        await UniTask.Delay(500, ignoreTimeScale: true);

        // 나레이션 출력 / 안녕하십니까. 원전 통제실 위기대응...
        await ShowNarrationAsync(1);

        await UniTask.Delay(500, ignoreTimeScale: true);

        //await ScreenFadeInAsync();

        DLogger.TLog(" 원전 통제실 위기대응 교육 브리핑] 프로세스 종료...");
    }
}
