using Common;
using Common.Extension;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class UIManager : MonoSingleton<UIManager>
{
    [SerializeField] UINarrationNoticPopup mNarrationNoticPopup = null;


    public void Initialized()
    {

    }

    public void Deinitialized()
    {
        
    }

    public void ShowNarrationNoticPopup(bool isShow, string desc = null)
    {
        if (mNarrationNoticPopup)
        {
            mNarrationNoticPopup.ShowPopup(isShow);
            if (desc.IsNullOrEmpty() == false) mNarrationNoticPopup.SetNarrationDesc(desc);
        }
    }

    public void ShowMissionNoticPopup(bool isShow, int missionNumber = 1)
    {
     
    }
}
