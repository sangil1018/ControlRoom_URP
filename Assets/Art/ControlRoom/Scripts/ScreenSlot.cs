using UnityEngine;

// Info carried over from Blender: which shared image sequence this screen uses.
public class ScreenSlot : MonoBehaviour
{
    [Tooltip("Screens with the same group share one image sequence")] public int group;
    [Tooltip("Front / Mid / Edge")] public string tier;
    [Tooltip("Planned frame count for this tier")] public int frames;
    public float widthMeters;
    public float heightMeters;
}
