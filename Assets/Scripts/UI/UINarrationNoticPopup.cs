using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

using Common.Extension;


public class UINarrationNoticPopup : UIPopup
{
    [SerializeField] private TextMeshProUGUI mDescText = null;

    public override void Open()
    {
        mIsDestroy = false;

        mDescText.SetString("");

        base.Open();
    }

    public override void Close(bool isImmediate)
    {
        base.Close(isImmediate);
    }

    public void SetNarrationDesc(int strIndex) => mDescText.SetString(strIndex);
    public void SetNarrationDesc(string strText) => mDescText.SetString(strText);
}
