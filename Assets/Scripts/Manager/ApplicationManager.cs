using Common;
using Common.Extension;
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class ApplicationManager : MonoSingleton<ApplicationManager>
{
    private void Start()
    {
        TableManager.Instance.LoadTables();
        UIManager.Instance.Initialized();
        SoundManager.Instance.Initialized();
        PhaseProcessorManager.Instance.Initialized();        
    }

    public void ReturnMainMenu()
    {
        DLogger.TLog($"메인 메뉴 씬으로 전환...");
        
    }

    /// <summary>
    /// 비동기 씬 로딩
    /// </summary>
    /// <param name="sceneName"></param>
    /// <returns></returns>
    public async UniTask LoadSceneAsync(string sceneName)
    {
        DLogger.Log($"{sceneName} 로딩 시작...");

        // 씬을 비동기로 로딩하고 완료될 때까지 대기
        await SceneManager.LoadSceneAsync(sceneName).ToUniTask();

        DLogger.Log($"{sceneName} 로딩 완료!");
    }

    public void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
