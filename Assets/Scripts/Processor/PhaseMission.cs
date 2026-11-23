using Common;
using Common.Extension;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;


public enum PhaseMissionType
{
   
}

public class PhaseMission
{
    public PhaseMissionType missionType;
    public bool IsCompleted = false;
    public float missionTime = 0f;

    public PhaseMission(PhaseMissionType _missionType)
    {
        missionType = _missionType;
    }

    public void StartMission()
    {       
        missionTime = Time.time;
    }

    public void EndMission(bool isCompleted)
    {
        IsCompleted = isCompleted;
        missionTime -= Time.time;

        PhaseProcessorManager.Instance.AddMission(this);
    }
}