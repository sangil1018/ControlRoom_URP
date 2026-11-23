using Common;
using Common.Extension;
using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class PhaseProcessorManager : MonoBehaviour
{
    [SerializeField] List<PhaseProcessor> mPhaseProcessors = new();

    private List<PhaseMission> mPhaseMissions = new();

    static public PhaseProcessorManager Instance { get; private set; } = null;


    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    public void Initialized()
    {
        Time.timeScale = 1.0f;

        if (mPhaseProcessors != null)
            mPhaseProcessors.ForEach(x => x.Initialized());

        StartPhaseProcess().Forget();
    }

    public void Deinitialized()
    {
        if (mPhaseProcessors != null)
            mPhaseProcessors.ForEach(x => x.Deinitialized());
    }

    //private void Update()
    //{ 
    //    if (Keyboard.current != null && Keyboard.current.spaceKey.wasReleasedThisFrame)
    //    { 
    //        StartPhaseProcess().Forget();
    //    }
    //}

    public async UniTaskVoid StartPhaseProcess()
    {
        foreach (PhaseProcessor processor in mPhaseProcessors)
        {
            if (processor != null)
            {
                await processor.StartProcessAsync();
            }
        }
    }

    public bool AllClearMission()
    {
        foreach (var mission in mPhaseMissions)
        {
            if (mission != null && !mission.IsCompleted)
            {
                return false;
            }
        }

        return true;
    }

    public void AddMission(PhaseMission result) => mPhaseMissions.Add(result);
}
