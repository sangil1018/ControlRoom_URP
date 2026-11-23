using System;

public enum SoundType
{
    BGM = 0,
    SFX,
    Narration,
}

[Serializable]
public class SoundTableData
{
    public int index;
    public bool isLoop;
    public bool isOneshot;
    public float pitch;
    public SoundType soundType;
    public string audioPath;
}