using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


public class LocalizedTextMeshUGUI : TextMeshProUGUI
{
    [Header("String Table Settings")]
    [SerializeField] private int stringTableIndex = 0;

    public int StringTableIndex
    {
        get => stringTableIndex;
        set
        {
            stringTableIndex = value;
            UpdateLocalizedText();
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateLocalizedText();
    }

    public void UpdateLocalizedText()
    {
        if (stringTableIndex > 0 && TableManager.IsInstance())
        {
            this.text = TableManager.GetString(stringTableIndex);
        }
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        UpdateLocalizedText();
    }
#endif
}
