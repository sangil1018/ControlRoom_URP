using Common;
using Common.Extension;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// 자리로 이동 페이즈
/// </summary>
public class Phase01Processor : PhaseProcessor
{
    public override async UniTask StartProcessAsync()
    {
        DLogger.TLog("[자리로 이동] 미션 프로세스 시작...");
        
        await UniTask.Delay(500, ignoreTimeScale: true);

        using (var cancelToken = new CancellationTokenSource())
        {
            // 나레이션 출력 / 다음 표시된 곳까지 양손을 움직여 이동해주세요.
            await ShowNarrationAsync(2);

            if (PlayerController.Instance != null)
                PlayerController.Instance.SetArmSwingerPaused(false);

            // 자리로 이동 이벤트 호출
            await StartPhaseEventAsync(0, 0, 11, cancelToken.Token);

            if (PlayerController.Instance != null)
                PlayerController.Instance.SetArmSwingerPaused(true);

            await UniTask.Delay(500, ignoreTimeScale: true);

            // 자리 도착후 의자에 앉는 애니메이션 출력
            await StartPhaseEventAsync(1, 0, cancellationToken: cancelToken.Token);
            await UniTask.Delay(500, ignoreTimeScale: true);
        }

        // 나레이션 출력 / 정상 운전 출력조정을 시작합니다 모니터를 바라봅니다.
        await ShowNarrationAsync(3);
        await UniTask.Delay(500, ignoreTimeScale: true);

        DLogger.TLog("[자리로 이동] 미션 프로세스 종료...");
    }
}
